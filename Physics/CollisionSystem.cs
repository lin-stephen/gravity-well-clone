using System.Numerics;
using GravityWell.Entities;
using GravityWell.World;

namespace GravityWell.Physics;

public sealed class CollisionSystem
{
    public const float Restitution = 0.8f;
    public const float DamageSpeedThreshold = 100f;
    public const float SpeedPerDamagePoint = 25f;

    private HashSet<(ICollisionBody, ICollisionBody)> contacts = [];

    public void Reset() => contacts.Clear();

    public void Update(GameWorld world)
    {
        List<ICollisionBody> bodies = [world.Player];
        bodies.AddRange(world.Planets);
        bodies.AddRange(world.Planets.Where(planet => planet.HasOperationalStructure(StructureType.SpaceDock))
            .Select(planet => planet.SpaceDock));
        bodies.AddRange(world.Freighters.Where(body => body.State != FreighterState.Consumed));
        HashSet<(ICollisionBody, ICollisionBody)> touching = [];

        for (var i = 0; i < bodies.Count; i++)
        for (var j = i + 1; j < bodies.Count; j++)
        {
            var first = bodies[i];
            var second = bodies[j];
            if (first is Fighter { DockingTarget: { } target } && ReferenceEquals(target, second))
                continue;
            var radius = first.CollisionRadius + second.CollisionRadius;
            if (Vector2.DistanceSquared(first.Position, second.Position) > radius * radius)
                continue;

            var pair = (first, second);
            touching.Add(pair);
            if (!contacts.Contains(pair))
            {
                // Damage uses the velocities before the bounce impulse.
                var damage = CalculateDamage(GetClosingSpeed(first, second));
                first.Health = Math.Max(0, first.Health - damage);
                second.Health = Math.Max(0, second.Health - damage);
            }

            ResolveContact(first, second);
        }

        contacts = touching;
    }

    public static void ResolveContact(ICollisionBody first, ICollisionBody second)
    {
        var inverseMass = first.InverseMass + second.InverseMass;
        if (inverseMass <= 0f) return;

        var offset = second.Position - first.Position;
        var distance = offset.Length();
        var penetration = first.CollisionRadius + second.CollisionRadius - distance;
        if (penetration < 0f) return;

        var relativeVelocity = second.Velocity - first.Velocity;
        var normal = GetContactNormal(first, second);
        var normalSpeed = Vector2.Dot(relativeVelocity, normal);
        var impulse = normalSpeed < 0f
            ? -(1f + Restitution) * normalSpeed / inverseMass * normal
            : Vector2.Zero;
        var correction = normal * (penetration / inverseMass);
        first.ApplyCollision(-correction * first.InverseMass, -impulse * first.InverseMass);
        second.ApplyCollision(correction * second.InverseMass, impulse * second.InverseMass);
    }

    private static Vector2 GetContactNormal(ICollisionBody first, ICollisionBody second)
    {
        var offset = second.Position - first.Position;
        if (offset.LengthSquared() > 0f) return Vector2.Normalize(offset);
        // Match bounce resolution when coincident centers have no surface normal.
        var relativeVelocity = second.Velocity - first.Velocity;
        return relativeVelocity.LengthSquared() > 0f
            ? -Vector2.Normalize(relativeVelocity)
            : Vector2.UnitX;
    }

    public static float GetClosingSpeed(ICollisionBody first, ICollisionBody second) =>
        MathF.Max(0f, Vector2.Dot(first.Velocity - second.Velocity, GetContactNormal(first, second)));

    public static int CalculateDamage(float closingSpeed) =>
        closingSpeed <= DamageSpeedThreshold
            ? 0
            : (int)MathF.Ceiling((closingSpeed - DamageSpeedThreshold) / SpeedPerDamagePoint);
}
