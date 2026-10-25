using System.Numerics;
using GravityWell.Core.Entities;
using GravityWell.Core.World;

namespace GravityWell.Core.AI;

// A small visibility graph around expanded circular obstacles. Replanned per
// order, not per frame; following a fixed route also prevents left/right jitter.
//
// This runs inside the browser's interpreter, where a single call used to cost a quarter of a second and freeze the
// game. So it works on flat arrays rather than LINQ and interfaces, returns the straight path without building a
// graph whenever one is clear, and searches with A* so it only looks at nodes that lie toward the destination.
public static class RoutePlanner
{
    // How far a ship keeps from an obstacle's edge, and how far beyond it the detour nodes sit.
    private const float SafetyMargin = 20f;
    private const float DetourMargin = 60f;
    private const int NodesPerObstacle = 16;

    /// <summary>Every planet and each operational space dock. <see cref="IsClear"/> walks the same set without allocating.</summary>
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
        if (IsInfinite(ship.Position) || IsInfinite(destination))
        {
            return []; // Nowhere to route to: the ship brakes and tries again, as it does when there is no route.
        }

        if (IsClear(ship.Position, destination, ship, world))
        {
            return [destination];
        }

        var field = ObstacleField.Of(world, ship);
        if (field.Covers(destination))
        {
            // Only the ship itself can be inside a margin among the nodes, and it has just been tried: no leg can
            // end at a destination that is inside one, so there is no route to search for.
            return [];
        }

        var nodes = BuildNodes(field, ship, destination);
        var previous = Search(nodes, field, destination);
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

    /// <summary>
    /// Whether the segment misses every obstacle. It walks the same obstacles as <see cref="Obstacles"/>, every planet
    /// and each operational space dock, directly rather than through that iterator so it allocates nothing: keep the two
    /// in step, or the autopilot's "is my next waypoint still clear" check will disagree with <see cref="Plan"/>.
    /// </summary>
    public static bool IsClear(Vector2 start, Vector2 end, Ship ship, GameWorld world)
    {
        var segment = end - start;
        var lengthSquared = segment.LengthSquared();
        if (lengthSquared == 0)
        {
            return true;
        }

        var planets = world.Planets;
        for (var i = 0; i < planets.Count; i++)
        {
            var planet = planets[i];
            if (Blocks(start, segment, lengthSquared, planet.Position, planet.CollisionRadius + ship.CollisionRadius + SafetyMargin))
            {
                return false;
            }

            if (planet.HasOperationalStructure(StructureType.SpaceDock))
            {
                var dock = planet.SpaceDock;
                if (Blocks(start, segment, lengthSquared, dock.Position, dock.CollisionRadius + ship.CollisionRadius + SafetyMargin))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool IsInfinite(Vector2 point) => float.IsInfinity(point.X) || float.IsInfinity(point.Y);

    /// <summary>Whether the segment from <c>start</c> along <c>segment</c> runs into the circle at <c>center</c>.</summary>
    private static bool Blocks(Vector2 start, Vector2 segment, float lengthSquared, Vector2 center, float radius)
    {
        // Allow a ship already inside the safety margin to leave it outward.
        var fromCenter = start - center;
        if (fromCenter.LengthSquared() < radius * radius && Vector2.Dot(fromCenter, segment) >= 0f)
        {
            return false;
        }

        var t = Math.Clamp(Vector2.Dot(center - start, segment) / lengthSquared, 0f, 1f);
        return Vector2.DistanceSquared(start + t * segment, center) < radius * radius;
    }

    /// <summary>Every obstacle's centre and the radius a ship must keep out of, read once into flat arrays.</summary>
    private sealed class ObstacleField
    {
        public required Vector2[] Centers { get; init; }
        public required float[] CollisionRadii { get; init; }
        public required float[] ClearRadii { get; init; }

        public static ObstacleField Of(GameWorld world, Ship ship)
        {
            var centers = new List<Vector2>();
            var radii = new List<float>();
            foreach (var obstacle in Obstacles(world))
            {
                centers.Add(obstacle.Position);
                radii.Add(obstacle.CollisionRadius);
            }

            var collisionRadii = radii.ToArray();
            var clearRadii = new float[collisionRadii.Length];
            for (var i = 0; i < clearRadii.Length; i++)
            {
                clearRadii[i] = collisionRadii[i] + ship.CollisionRadius + SafetyMargin;
            }

            return new ObstacleField { Centers = centers.ToArray(), CollisionRadii = collisionRadii, ClearRadii = clearRadii };
        }

        /// <summary>Whether the point is inside some obstacle's keep-out radius.</summary>
        public bool Covers(Vector2 point)
        {
            for (var i = 0; i < Centers.Length; i++)
            {
                if (Vector2.DistanceSquared(point, Centers[i]) < ClearRadii[i] * ClearRadii[i])
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsClear(Vector2 start, Vector2 end)
        {
            var segment = end - start;
            var lengthSquared = segment.LengthSquared();
            if (lengthSquared == 0)
            {
                return true;
            }

            for (var i = 0; i < Centers.Length; i++)
            {
                if (Blocks(start, segment, lengthSquared, Centers[i], ClearRadii[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>The ship, the destination, then detour points around each obstacle that no other obstacle covers.</summary>
    private static Vector2[] BuildNodes(ObstacleField field, Ship ship, Vector2 destination)
    {
        var nodes = new List<Vector2>(2 + field.Centers.Length * NodesPerObstacle) { ship.Position, destination };
        for (var obstacle = 0; obstacle < field.Centers.Length; obstacle++)
        {
            for (var i = 0; i < NodesPerObstacle; i++)
            {
                var angle = i * MathF.Tau / NodesPerObstacle;
                var point = field.Centers[obstacle] + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) *
                    (field.CollisionRadii[obstacle] + ship.CollisionRadius + DetourMargin);
                if (IsOutsideEveryObstacle(field, point))
                {
                    nodes.Add(point);
                }
            }
        }

        return nodes.ToArray();
    }

    private static bool IsOutsideEveryObstacle(ObstacleField field, Vector2 point)
    {
        for (var other = 0; other < field.Centers.Length; other++)
        {
            if (!(Vector2.Distance(point, field.Centers[other]) > field.ClearRadii[other]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>A* from node 0 to node 1. Returns each node's predecessor, or -1 where the search never reached it.</summary>
    private static int[] Search(Vector2[] nodes, ObstacleField field, Vector2 destination)
    {
        var costs = new float[nodes.Length];
        var previous = new int[nodes.Length];
        var closed = new bool[nodes.Length];
        Array.Fill(costs, float.PositiveInfinity);
        Array.Fill(previous, -1);
        costs[0] = 0;
        var open = new PriorityQueue<int, float>();
        open.Enqueue(0, Vector2.Distance(nodes[0], destination));
        while (open.TryDequeue(out var current, out _))
        {
            if (closed[current])
            {
                continue; // A stale entry: a cheaper way to this node was found and expanded already.
            }

            if (current == 1)
            {
                break;
            }

            closed[current] = true;
            for (var next = 1; next < nodes.Length; next++)
            {
                // The cheap tests come first: the clearance test is the expensive one, and most edges either lead
                // somewhere already reached more cheaply or cannot beat the route to the destination found so far.
                if (closed[next])
                {
                    continue;
                }

                var cost = costs[current] + Vector2.Distance(nodes[current], nodes[next]);
                if (cost >= costs[next])
                {
                    continue;
                }

                // The straight-line distance to the destination never overestimates, so the first time the
                // destination is taken off the queue its route is the shortest.
                var estimate = cost + Vector2.Distance(nodes[next], destination);
                if (estimate >= costs[1] || !field.IsClear(nodes[current], nodes[next]))
                {
                    continue;
                }

                costs[next] = cost;
                previous[next] = current;
                open.Enqueue(next, estimate);
            }
        }

        return previous;
    }
}
