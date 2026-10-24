using System.Numerics;
using GravityWell.Entities;
using GravityWell.Input;
using GravityWell.Physics;
using GravityWell.World;

namespace GravityWell.AI;

public enum FlightPhase { Holding, Cruise, Brake, Align, Approach, Dock }

public sealed class FreighterAutopilot
{
    private readonly DockingSystem docking;
    private readonly PhysicsSystem physics;

    public FreighterAutopilot(DockingSystem? docking = null, PhysicsSystem? physics = null)
    {
        this.docking = docking ?? new DockingSystem();
        this.physics = physics ?? new PhysicsSystem();
    }

    private sealed class FlightPlan
    {
        public Planet? Target;
        public Vector2 HoldPosition;
        public Vector2 LandingNormal;
        public Vector2 ApproachPoint;
        public List<Vector2> Route = [];
        public FlightPhase Phase;
        public float ReplanDelay;
    }

    private readonly Dictionary<Freighter, FlightPlan> plans = [];

    public FlightPhase GetPhase(Freighter ship) => plans.TryGetValue(ship, out var plan)
        ? plan.Phase : FlightPhase.Holding;

    public void Update(GameWorld world, float deltaTime)
    {
        foreach (var removed in plans.Keys.Where(ship => ship.State == FreighterState.Consumed).ToArray())
            plans.Remove(removed);
        foreach (var ship in world.Freighters)
        {
            if (ship.State == FreighterState.Consumed) continue;
            if (!plans.TryGetValue(ship, out var plan))
            {
                plan = new FlightPlan { HoldPosition = ship.Position };
                plans.Add(ship, plan);
            }
            ship.Controls = Steer(world, ship, plan, deltaTime);
        }
    }

    private ShipControls Steer(GameWorld world, Freighter ship, FlightPlan plan, float deltaTime)
    {
        if (ship.DockingTarget is not null)
        {
            plan.Phase = FlightPhase.Dock;
            return default;
        }
        if (ship.State != FreighterState.Traveling || ship.TargetPlanet is null)
        {
            plan.Phase = FlightPhase.Holding;
            return Seek(world, ship, plan.HoldPosition, 20f);
        }

        var planet = ship.TargetPlanet;
        if (plan.Target != planet)
        {
            plan.Target = planet;
            var offset = ship.Position - planet.Position;
            plan.LandingNormal = offset.LengthSquared() > 0 ? Vector2.Normalize(offset) : Vector2.UnitY;
            // Avoid the orbital installation when choosing a landing corridor.
            if (planet.HasOperationalStructure(StructureType.SpaceDock) && plan.LandingNormal.Y < -0.4f)
                plan.LandingNormal = Vector2.UnitX;
            plan.ApproachPoint = planet.Position + plan.LandingNormal *
                (Planet.Radius + ship.CollisionRadius + 90f);
            plan.Route = RoutePlanner.Plan(world, ship, plan.ApproachPoint);
            plan.ReplanDelay = 1f;
            plan.Phase = FlightPhase.Cruise;
        }
        plan.ReplanDelay -= deltaTime;
        var outward = ship.Position - planet.Position;
        var distance = outward.Length();
        var normal = distance > 0f ? outward / distance : plan.LandingNormal;
        var surfaceDistance = distance - Planet.Radius - ship.CollisionRadius;

        if (plan.Phase is FlightPhase.Align or FlightPhase.Approach)
        {
            // A bounce or another ship can displace us: return to the corridor.
            if (surfaceDistance > 150f || ship.Velocity.Length() > 65f)
            {
                plan.Phase = FlightPhase.Cruise;
                plan.Route = RoutePlanner.Plan(world, ship, plan.ApproachPoint);
            }
            else
            {
                var sideways = ship.Velocity - Vector2.Dot(ship.Velocity, normal) * normal;
                if (plan.Phase == FlightPhase.Align)
                {
                    if (docking.IsRearAligned(ship, planet) && sideways.Length() < 5f)
                        plan.Phase = FlightPhase.Approach;
                    else
                        return Accelerate(ship, normal * 20f - sideways * 2f -
                            physics.GetGravity(world, ship.Position), normal);
                }
                if (surfaceDistance <= docking.CaptureDistance &&
                    docking.IsRearAligned(ship, planet) && docking.IsSpeedSafe(ship, planet))
                    return default;
                var desiredVelocity = -normal * 12f;
                var acceleration = (desiredVelocity - ship.Velocity) * 2f -
                    physics.GetGravity(world, ship.Position);
                return Accelerate(ship, acceleration, normal);
            }
        }

        while (plan.Route.Count > 1 && Vector2.Distance(ship.Position, plan.Route[0]) < 20f)
            plan.Route.RemoveAt(0);
        if (plan.ReplanDelay <= 0f && (plan.Route.Count == 0 ||
            !RoutePlanner.IsClear(ship.Position, plan.Route[0], ship, RoutePlanner.Obstacles(world))))
        {
            plan.Route = RoutePlanner.Plan(world, ship, plan.ApproachPoint);
            plan.ReplanDelay = 1f;
        }
        if (plan.Route.Count == 0)
            return Seek(world, ship, ship.Position, 0f); // Brake and retry instead of flying through obstacles.
        var waypoint = plan.Route[0];
        var remaining = Vector2.Distance(ship.Position, waypoint);
        if (plan.Route.Count == 1 && remaining < 18f && ship.Velocity.Length() < 15f)
        {
            plan.Phase = FlightPhase.Align;
            return Accelerate(ship, normal * 20f - ship.Velocity * 2f -
                physics.GetGravity(world, ship.Position), normal);
        }
        // Reserve distance for both the turn to face backwards and braking.
        var brakingDistance = ship.Velocity.LengthSquared() / 80f + ship.Velocity.Length() * 1.2f;
        plan.Phase = remaining < brakingDistance ? FlightPhase.Brake : FlightPhase.Cruise;
        return Seek(world, ship, waypoint, plan.Route.Count > 1 ? 55f : Freighter.Speed);
    }

    private ShipControls Seek(GameWorld world, Freighter ship, Vector2 destination, float maxSpeed)
    {
        var offset = destination - ship.Position;
        var distance = offset.Length();
        var speed = MathF.Min(maxSpeed, MathF.Min(distance * 0.6f, MathF.Sqrt(40f * distance)));
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
