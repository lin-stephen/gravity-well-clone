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
        bodies.AddRange(world.Freighters.Where(body => body.State != FreighterState.Consumed));
        HashSet<(ICollisionBody, ICollisionBody)> touching = [];

        for (var i = 0; i < bodies.Count; i++)
        for (var j = i + 1; j < bodies.Count; j++)
        {
            var first = bodies[i];
            var second = bodies[j];
            var radius = first.CollisionRadius + second.CollisionRadius;
            if (Vector2.DistanceSquared(first.Position, second.Position) > radius * radius)
                continue;

            var pair = (first, second);
            touching.Add(pair);
            if (!contacts.Contains(pair))
            {
                // Damage uses the velocities before the bounce impulse.
                var relativeSpeed = (first.Velocity - second.Velocity).Length();
                var damage = CalculateDamage(relativeSpeed);
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
        // Coincident centers have no geometric normal. Oppose relative motion
        // when possible, otherwise use a deterministic axis to separate them.
        var normal = distance > 0f ? offset / distance
            : relativeVelocity.LengthSquared() > 0f ? -Vector2.Normalize(relativeVelocity)
            : Vector2.UnitX;
        var normalSpeed = Vector2.Dot(relativeVelocity, normal);
        var impulse = normalSpeed < 0f
            ? -(1f + Restitution) * normalSpeed / inverseMass * normal
            : Vector2.Zero;
        var correction = normal * (penetration / inverseMass);
        first.ApplyCollision(-correction * first.InverseMass, -impulse * first.InverseMass);
        second.ApplyCollision(correction * second.InverseMass, impulse * second.InverseMass);
    }

    public static int CalculateDamage(float relativeSpeed) =>
        relativeSpeed <= DamageSpeedThreshold
            ? 0
            : (int)MathF.Ceiling((relativeSpeed - DamageSpeedThreshold) / SpeedPerDamagePoint);
}
