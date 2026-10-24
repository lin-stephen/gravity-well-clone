using System.Numerics;
using GravityWell.Entities;
using GravityWell.Input;
using GravityWell.World;

namespace GravityWell.Physics;

public sealed class DockingSystem
{
    public const float SpeedThreshold = 60f;
    public const float AlignmentDegrees = 30f;
    public const float CaptureDistance = 4f;
    public const float DampingRate = 8f;
    private ICollisionBody? releasedTarget;

    public void Update(GameWorld world, PlayerControls controls, float deltaTime)
    {
        var fighter = world.Player;
        if (controls.Reset) releasedTarget = null;
        if (controls.Thrust && fighter.DockingTarget is { } departing)
        {
            releasedTarget = departing;
            fighter.Undock();
        }

        if (releasedTarget is { } released &&
            Vector2.Distance(fighter.Position, released.Position) >
            Fighter.Radius + released.CollisionRadius + CaptureDistance)
            releasedTarget = null;

        if (fighter.DockingTarget is SpaceDockBody dock &&
            !dock.Planet.HasOperationalStructure(StructureType.SpaceDock))
            fighter.Undock();

        if (fighter.DockingTarget is null && !controls.Thrust)
        {
            // Prefer the dock when its capture zone overlaps the planet's.
            foreach (var planet in world.Planets)
            {
                if (planet.HasOperationalStructure(StructureType.SpaceDock) && TryCapture(fighter, planet.SpaceDock))
                    break;
                if (TryCapture(fighter, planet)) break;
            }
        }

        if (fighter.DockingTarget is not { } target) return;
        var destination = target.Position + fighter.DockingNormal *
            (Fighter.Radius + target.CollisionRadius);
        var blend = 1f - MathF.Exp(-DampingRate * MathF.Max(0f, deltaTime));
        // Forward is (sin(rotation), -cos(rotation)), so align it with the
        // outward surface normal. Wrap the error to take the shortest turn.
        var dockingRotation = MathF.Atan2(fighter.DockingNormal.X, -fighter.DockingNormal.Y)
            * 180f / MathF.PI;
        var rotationError = MathF.IEEERemainder(dockingRotation - fighter.Rotation, 360f);
        fighter.Rotation += rotationError * blend;
        fighter.Position = Vector2.Lerp(fighter.Position, destination, blend);
        fighter.Velocity = Vector2.Lerp(fighter.Velocity, target.Velocity, blend);
        if (Vector2.DistanceSquared(fighter.Position, destination) < 0.01f &&
            (fighter.Velocity - target.Velocity).LengthSquared() < 0.25f &&
            MathF.Abs(rotationError) < 0.1f)
        {
            fighter.Position = destination;
            fighter.Velocity = target.Velocity;
            fighter.Rotation = dockingRotation;
            fighter.CompleteDocking();
        }
    }

    public static bool IsSpeedSafe(Fighter fighter, ICollisionBody target) =>
        (fighter.Velocity - target.Velocity).LengthSquared() < SpeedThreshold * SpeedThreshold;

    public static bool IsApproachSpeedSafe(Fighter fighter, ICollisionBody target)
    {
        var towardTarget = target.Position - fighter.Position;
        if (towardTarget.LengthSquared() == 0f) return false;
        var approachSpeed = Vector2.Dot(fighter.Velocity - target.Velocity,
            Vector2.Normalize(towardTarget));
        // Receding motion has no closing speed; sideways motion contributes zero.
        return MathF.Max(0f, approachSpeed) < SpeedThreshold;
    }

    private bool TryCapture(Fighter fighter, ICollisionBody target)
    {
        if (target == releasedTarget || fighter.Health <= 0 || target.Health <= 0) return false;
        var offset = fighter.Position - target.Position;
        var distance = offset.Length();
        var radius = Fighter.Radius + target.CollisionRadius;
        if (distance < radius - CaptureDistance || distance > radius + CaptureDistance || distance == 0f)
            return false;
        var normal = offset / distance;
        if (Vector2.Dot(fighter.Forward, normal) < MathF.Cos(AlignmentDegrees * MathF.PI / 180f) ||
            !IsSpeedSafe(fighter, target))
            return false;
        fighter.BeginDocking(target, normal);
        return true;
    }
}
