using System.Numerics;
using GravityWell.Core.Entities;
using GravityWell.Core.World;

namespace GravityWell.Core.AI;

// A small visibility graph around expanded circular obstacles. Replanned per
// order, not per frame; following a fixed route also prevents left/right jitter.
public static class RoutePlanner
{
    public static IEnumerable<ICollisionBody> Obstacles(GameWorld world)
    {
        foreach (var planet in world.Planets)
        {
            yield return planet;
            if (planet.HasOperationalStructure(StructureType.SpaceDock))
            {
                yield return planet.SpaceDock;
            }
        }
    }

    public static List<Vector2> Plan(GameWorld world, Ship ship, Vector2 destination)
    {
        var obstacles = Obstacles(world).ToArray();
        var nodes = new List<Vector2> { ship.Position, destination };
        foreach (var obstacle in obstacles)
        {
            for (var i = 0; i < 16; i++)
            {
                var angle = i * MathF.Tau / 16;
                var point = obstacle.Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) *
                    (obstacle.CollisionRadius + ship.CollisionRadius + 60f);
                if (obstacles.All(other => Vector2.Distance(point, other.Position) >
                                           other.CollisionRadius + ship.CollisionRadius + 20f))
                {
                    nodes.Add(point);
                }
            }
        }

        var costs = Enumerable.Repeat(float.PositiveInfinity, nodes.Count).ToArray();
        var previous = Enumerable.Repeat(-1, nodes.Count).ToArray();
        var visited = new bool[nodes.Count];
        costs[0] = 0;
        for (var step = 0; step < nodes.Count; step++)
        {
            var current = -1;
            for (var i = 0; i < nodes.Count; i++)
            {
                if (!visited[i] && (current < 0 || costs[i] < costs[current]))
                {
                    current = i;
                }
            }

            if (current < 0 || !float.IsFinite(costs[current]))
            {
                break;
            }

            if (current == 1)
            {
                break;
            }

            visited[current] = true;
            for (var next = 1; next < nodes.Count; next++)
            {
                if (visited[next] || !IsClear(nodes[current], nodes[next], ship, obstacles))
                {
                    continue;
                }

                var cost = costs[current] + Vector2.Distance(nodes[current], nodes[next]);
                if (cost >= costs[next])
                {
                    continue;
                }

                costs[next] = cost;
                previous[next] = current;
            }
        }
        if (previous[1] < 0)
        {
            return [];
        }

        var route = new List<Vector2>();
        for (var node = 1; node != 0; node = previous[node])
        {
            route.Add(nodes[node]);
        }

        route.Reverse();
        return route;
    }

    public static bool IsClear(Vector2 start, Vector2 end, Ship ship, IEnumerable<ICollisionBody> obstacles)
    {
        var segment = end - start;
        var lengthSquared = segment.LengthSquared();
        if (lengthSquared == 0)
        {
            return true;
        }

        foreach (var obstacle in obstacles)
        {
            var radius = obstacle.CollisionRadius + ship.CollisionRadius + 20f;
            // Allow a ship already inside the safety margin to leave it outward.
            var fromCenter = start - obstacle.Position;
            if (fromCenter.LengthSquared() < radius * radius && Vector2.Dot(fromCenter, segment) >= 0f)
            {
                continue;
            }

            var t = Math.Clamp(Vector2.Dot(obstacle.Position - start, segment) / lengthSquared, 0f, 1f);
            if (Vector2.DistanceSquared(start + t * segment, obstacle.Position) < radius * radius)
            {
                return false;
            }
        }
        return true;
    }
}
