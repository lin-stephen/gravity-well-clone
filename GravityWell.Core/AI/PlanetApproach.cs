using System.Numerics;
using GravityWell.Core.Entities;
using GravityWell.Core.Input;
using GravityWell.Core.Physics;
using GravityWell.Core.World;

namespace GravityWell.Core.AI;

public sealed class FlightPlan
{
    public Planet? Target;
    public Vector2 HoldPosition;
    public Vector2 LandingNormal;
    public Vector2 ApproachPoint;
    public List<Vector2> Route = [];
    public FlightPhase Phase;
    public float ReplanDelay;
}

/// <summary>Flies any ship to a planet's landing corridor, aligns it rear-first and brings it down to dock.</summary>
public sealed class PlanetApproach(DockingSystem docking, PhysicsSystem physics, ApproachTuning tuning)
{
    public ShipControls Steer(GameWorld world, Ship ship, FlightPlan plan, Planet planet, float deltaTime)
    {
        if (plan.Target != planet)
            BeginApproach(world, ship, plan, planet);
        plan.ReplanDelay -= deltaTime;
        var outward = ship.Position - planet.Position;
        var distance = outward.Length();
        var normal = distance > 0f ? outward / distance : plan.LandingNormal;
        var surfaceDistance = distance - Planet.Radius - ship.CollisionRadius;

        if (plan.Phase is FlightPhase.Align or FlightPhase.Approach &&
            SteerTerminal(world, ship, plan, planet, normal, surfaceDistance) is { } terminal)
            return terminal;

        FollowRoute(world, ship, plan);
        return SteerCruise(world, ship, plan, normal);
    }

    private void BeginApproach(GameWorld world, Ship ship, FlightPlan plan, Planet planet)
    {
        plan.Target = planet;
        var offset = ship.Position - planet.Position;
        plan.LandingNormal = offset.LengthSquared() > 0 ? Vector2.Normalize(offset) : Vector2.UnitY;
        // Avoid the orbital installation when choosing a landing corridor.
        if (planet.HasOperationalStructure(StructureType.SpaceDock) && plan.LandingNormal.Y < -0.4f)
            plan.LandingNormal = Vector2.UnitX;
        plan.ApproachPoint = planet.Position + plan.LandingNormal *
            (Planet.Radius + ship.CollisionRadius + tuning.ApproachClearance);
        plan.Route = RoutePlanner.Plan(world, ship, plan.ApproachPoint);
        plan.ReplanDelay = 1f;
        plan.Phase = FlightPhase.Cruise;
    }

    /// <summary>Handles Align and Approach. Returns null when the ship left the corridor and should resume cruising.</summary>
    private ShipControls? SteerTerminal(GameWorld world, Ship ship, FlightPlan plan, Planet planet,
        Vector2 normal, float surfaceDistance)
    {
        // A bounce or another ship can displace us: return to the corridor.
        if (surfaceDistance > tuning.CorridorBreakDistance || ship.Velocity.Length() > tuning.CorridorBreakSpeed)
        {
            plan.Phase = FlightPhase.Cruise;
            plan.Route = RoutePlanner.Plan(world, ship, plan.ApproachPoint);
            return null;
        }

        var sideways = ship.Velocity - Vector2.Dot(ship.Velocity, normal) * normal;
        if (plan.Phase == FlightPhase.Align)
        {
            if (docking.IsRearAligned(ship, planet) && sideways.Length() < tuning.AlignedSidewaysSpeed)
                plan.Phase = FlightPhase.Approach;
            else
                return Accelerate(ship, HoverAcceleration(world, ship, normal, sideways), normal);
        }
        if (surfaceDistance <= docking.CaptureDistance &&
            docking.IsRearAligned(ship, planet) && docking.IsSpeedSafe(ship, planet))
            return default(ShipControls);
        var desiredVelocity = -normal * tuning.TouchdownSpeed;
        var acceleration = (desiredVelocity - ship.Velocity) * 2f -
            physics.GetGravity(world, ship.Position);
        return Accelerate(ship, acceleration, normal);
    }

    private void FollowRoute(GameWorld world, Ship ship, FlightPlan plan)
    {
        while (plan.Route.Count > 1 &&
               Vector2.Distance(ship.Position, plan.Route[0]) < tuning.WaypointReachedDistance)
            plan.Route.RemoveAt(0);
        if (plan.ReplanDelay <= 0f && (plan.Route.Count == 0 ||
            !RoutePlanner.IsClear(ship.Position, plan.Route[0], ship, RoutePlanner.Obstacles(world))))
        {
            plan.Route = RoutePlanner.Plan(world, ship, plan.ApproachPoint);
            plan.ReplanDelay = 1f;
        }
    }

    private ShipControls SteerCruise(GameWorld world, Ship ship, FlightPlan plan, Vector2 normal)
    {
        if (plan.Route.Count == 0)
            return Seek(world, ship, ship.Position, 0f); // Brake and retry instead of flying through obstacles.
        var waypoint = plan.Route[0];
        var remaining = Vector2.Distance(ship.Position, waypoint);
        if (plan.Route.Count == 1 && remaining < tuning.AlignTriggerDistance &&
            ship.Velocity.Length() < tuning.AlignTriggerSpeed)
        {
            plan.Phase = FlightPhase.Align;
            return Accelerate(ship, HoverAcceleration(world, ship, normal, ship.Velocity), normal);
        }
        // Reserve distance for both the turn to face backwards and braking.
        var brakingDistance = ship.Velocity.LengthSquared() / (2f * tuning.BrakeDeceleration) +
            ship.Velocity.Length() * tuning.BrakeTurnSeconds;
        plan.Phase = remaining < brakingDistance ? FlightPhase.Brake : FlightPhase.Cruise;
        return Seek(world, ship, waypoint,
            plan.Route.Count > 1 ? tuning.RouteWaypointSpeed : tuning.CruiseSpeed);
    }

    /// <summary>Hold off the surface at the tuned hover speed while damping the given drift and countering gravity.</summary>
    private Vector2 HoverAcceleration(GameWorld world, Ship ship, Vector2 normal, Vector2 drift) =>
        normal * tuning.HoverSpeed - drift * 2f - physics.GetGravity(world, ship.Position);

    /// <summary>Fly to and stay at the plan's hold position.</summary>
    public ShipControls HoldAt(GameWorld world, Ship ship, FlightPlan plan) =>
        Seek(world, ship, plan.HoldPosition, tuning.HoldSpeed);

    public ShipControls Seek(GameWorld world, Ship ship, Vector2 destination, float maxSpeed)
    {
        var offset = destination - ship.Position;
        var distance = offset.Length();
        var speed = MathF.Min(maxSpeed,
            MathF.Min(distance * 0.6f, MathF.Sqrt(tuning.BrakeDeceleration * distance)));
        var desiredVelocity = distance > 0f ? offset / distance * speed : Vector2.Zero;
        // Predict closest approach to other ships. A fixed right-hand preference
        // gives crossing traffic a consistent passing side.
        foreach (var other in world.Ships)
        {
            if (ReferenceEquals(other, ship)) continue;
            var separation = other.Position - ship.Position;
            var relativeVelocity = ship.Velocity - other.Velocity;
            var speedSquared = relativeVelocity.LengthSquared();
            var time = speedSquared > 0.01f
                ? Math.Clamp(Vector2.Dot(separation, relativeVelocity) / speedSquared, 0f, 5f) : 0f;
            var closest = separation - relativeVelocity * time;
            var clearance = ship.CollisionRadius + other.CollisionRadius + 12f + MathF.Min(43f, relativeVelocity.Length() * 0.4f);
            if (closest.Length() >= clearance || separation.Length() > 400f) continue;
            var away = separation.LengthSquared() > 0f ? -Vector2.Normalize(separation) : Vector2.UnitX;
            var right = new Vector2(away.Y, -away.X);
            desiredVelocity *= 0.5f;
            desiredVelocity += (away + right) * (clearance - closest.Length()) * 0.8f;
        }
        var acceleration = (desiredVelocity - ship.Velocity) * 2f - physics.GetGravity(world, ship.Position);
        return Accelerate(ship, acceleration, offset);
    }

    private static ShipControls Accelerate(Ship ship, Vector2 acceleration, Vector2 fallbackDirection)
    {
        var direction = acceleration.LengthSquared() > 0.25f ? acceleration : fallbackDirection;
        if (direction.LengthSquared() == 0f) return default;
        var angle = MathF.Atan2(direction.X, -direction.Y) * 180f / MathF.PI;
        var error = MathF.IEEERemainder(angle - ship.Rotation, 360f);
        var turn = Math.Clamp(error / 30f, -1f, 1f);
        var throttle = MathF.Abs(error) < 15f
            ? Math.Clamp(Vector2.Dot(acceleration, ship.Forward) / ship.ThrustAcceleration, 0f, 1f) : 0f;
        return new ShipControls(turn, throttle);
    }
}
