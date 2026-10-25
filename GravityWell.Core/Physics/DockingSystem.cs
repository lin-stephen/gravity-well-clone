using System.Numerics;
using GravityWell.Core.Entities;
using GravityWell.Core.World;

namespace GravityWell.Core.Physics;

public sealed class DockingSystem
{
    private readonly DockingSettings settings;

    public DockingSystem(DockingSettings? settings = null)
    {
        this.settings = settings ?? new DockingSettings();
        this.settings.Validate();
    }

    public float CaptureDistance => settings.CaptureDistance;

    public void Update(GameWorld world, float deltaTime)
    {
        foreach (var ship in world.Ships)
        {
            UpdateShip(world, ship, deltaTime);
        }
    }

    private void UpdateShip(GameWorld world, Ship ship, float deltaTime)
    {
        if (ship.ReleasedDockingTarget is { } released &&
            Vector2.Distance(ship.Position, released.Position) >
            ship.CollisionRadius + released.CollisionRadius + CaptureDistance)
        {
            ship.ReleasedDockingTarget = null;
        }

        if (ship.DockingTarget is SpaceDockBody dock &&
            !dock.Planet.HasOperationalStructure(StructureType.SpaceDock))
        {
            ship.Undock();
        }

        if (ship.DockingTarget is null && ship.Controls.Throttle <= 0f)
        {
            // Prefer the dock when its capture zone overlaps the planet's.
            foreach (var planet in world.Planets)
            {
                if (ship is Freighter freighter &&
                    (freighter.State != FreighterState.Traveling || freighter.TargetPlanet != planet))
                {
                    continue;
                }

                if (ship is not Freighter && planet.HasOperationalStructure(StructureType.SpaceDock) &&
                    TryCapture(ship, planet.SpaceDock))
                {
                    break;
                }

                if (TryCapture(ship, planet))
                {
                    break;
                }
            }
        }

        if (ship.DockingTarget is not { } target)
        {
            return;
        }

        var destination = target.Position + ship.DockingNormal *
            (ship.CollisionRadius + target.CollisionRadius);
        var blend = 1f - MathF.Exp(-settings.DampingRate * MathF.Max(0f, deltaTime));
        // Forward is (sin(rotation), -cos(rotation)), so align it with the
        // outward surface normal. Wrap the error to take the shortest turn.
        var dockingRotation = MathF.Atan2(ship.DockingNormal.X, -ship.DockingNormal.Y)
            * 180f / MathF.PI;
        var rotationError = MathF.IEEERemainder(dockingRotation - ship.Rotation, 360f);
        ship.Rotation += rotationError * blend;
        ship.Position = Vector2.Lerp(ship.Position, destination, blend);
        ship.Velocity = Vector2.Lerp(ship.Velocity, target.Velocity, blend);
        if (Vector2.DistanceSquared(ship.Position, destination) < 0.01f &&
            (ship.Velocity - target.Velocity).LengthSquared() < 0.25f &&
            MathF.Abs(rotationError) < 0.1f)
        {
            ship.Position = destination;
            ship.Velocity = target.Velocity;
            ship.Rotation = dockingRotation;
            ship.CompleteDocking();
        }
    }

    public bool IsSpeedSafe(Ship ship, ICollisionBody target) =>
        (ship.Velocity - target.Velocity).LengthSquared() < settings.SpeedThreshold * settings.SpeedThreshold;

    public bool IsApproachSpeedSafe(Ship ship, ICollisionBody target)
    {
        var towardTarget = target.Position - ship.Position;
        if (towardTarget.LengthSquared() == 0f)
        {
            return false;
        }

        var approachSpeed = Vector2.Dot(ship.Velocity - target.Velocity,
            Vector2.Normalize(towardTarget));
        // Receding motion has no closing speed; sideways motion contributes zero.
        return MathF.Max(0f, approachSpeed) < settings.SpeedThreshold;
    }

    public bool IsRearAligned(Ship ship, ICollisionBody target)
    {
        var outward = ship.Position - target.Position;
        return outward.LengthSquared() > 0f &&
            Vector2.Dot(ship.Forward, Vector2.Normalize(outward)) >=
            MathF.Cos(settings.AlignmentDegrees * MathF.PI / 180f);
    }

    public bool ShouldHighlight(Ship ship, ICollisionBody target) =>
        IsApproachSpeedSafe(ship, target) && IsRearAligned(ship, target);

    private bool TryCapture(Ship ship, ICollisionBody target)
    {
        if (target == ship.ReleasedDockingTarget)
        {
            return false;
        }

        var offset = ship.Position - target.Position;
        var distance = offset.Length();
        var radius = ship.CollisionRadius + target.CollisionRadius;
        if (distance < radius - CaptureDistance || distance > radius + CaptureDistance || distance == 0f)
        {
            return false;
        }

        var normal = offset / distance;
        if (!IsRearAligned(ship, target) || !IsSpeedSafe(ship, target))
        {
            return false;
        }

        ship.BeginDocking(target, normal);
        return true;
    }
}
