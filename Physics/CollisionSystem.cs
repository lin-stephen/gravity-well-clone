using System.Numerics;
using GravityWell.Entities;
using GravityWell.World;

namespace GravityWell.Physics;

public sealed class CollisionSystem
{
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
            if (contacts.Contains(pair)) continue;

            var relativeSpeed = (first.Velocity - second.Velocity).Length();
            var damage = CalculateDamage(relativeSpeed);
            first.Health = Math.Max(0, first.Health - damage);
            second.Health = Math.Max(0, second.Health - damage);
        }

        contacts = touching;
    }

    public static int CalculateDamage(float relativeSpeed) =>
        relativeSpeed <= DamageSpeedThreshold
            ? 0
            : (int)MathF.Ceiling((relativeSpeed - DamageSpeedThreshold) / SpeedPerDamagePoint);
}
