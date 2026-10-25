using System.Numerics;
using AwesomeAssertions;
using GravityWell.Core.AI;
using GravityWell.Core.Entities;
using GravityWell.Core.World;

namespace GravityWell.Tests.AI;

public class RoutePlannerTests
{
    private static float Length(Vector2 start, IReadOnlyList<Vector2> route)
    {
        var total = 0f;
        var from = start;
        foreach (var waypoint in route)
        {
            total += Vector2.Distance(from, waypoint);
            from = waypoint;
        }

        return total;
    }

    /// <summary>Starts and destinations scattered over the default map, many of them right next to a planet.</summary>
    private static IEnumerable<(Vector2 Start, Vector2 Destination)> RandomTrips(GameWorld world, int count)
    {
        var random = new Random(20251025);
        Vector2 Anywhere() => new(-300f + (float)random.NextDouble() * 2600f, -300f + (float)random.NextDouble() * 1600f);
        Vector2 NearAPlanet()
        {
            var planet = world.Planets[random.Next(world.Planets.Count)];
            var angle = (float)random.NextDouble() * MathF.Tau;
            var distance = Planet.Radius + Freighter.Radius + (float)random.NextDouble() * 120f;
            return planet.Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
        }

        for (var i = 0; i < count; i++)
        {
            yield return (random.Next(3) == 0 ? NearAPlanet() : Anywhere(), random.Next(3) == 0 ? NearAPlanet() : Anywhere());
        }
    }

    [Test]
    public void Plan_FindsRoutesAsShortAsTheReferencePlanner()
    {
        var world = new GameWorld();
        var ship = world.Freighters[0];
        foreach (var (start, destination) in RandomTrips(world, 400))
        {
            ship.Position = start;

            var route = RoutePlanner.Plan(world, ship, destination);
            var expected = ReferenceRoutePlanner.Plan(world, ship, destination);

            var because = $"from {start} to {destination}";
            (route.Count == 0).Should().Be(expected.Count == 0, because);
            if (route.Count == 0)
            {
                continue;
            }

            route[^1].Should().Be(destination, because);
            var expectedLength = Length(start, expected);
            Length(start, route).Should().BeApproximately(expectedLength, 0.01f + expectedLength * 1e-4f, because);
        }
    }

    [Test]
    public void Plan_EveryLegOfARouteIsClear()
    {
        var world = new GameWorld();
        var ship = world.Freighters[0];
        foreach (var (start, destination) in RandomTrips(world, 200))
        {
            ship.Position = start;

            var route = RoutePlanner.Plan(world, ship, destination);

            var from = start;
            foreach (var waypoint in route)
            {
                ReferenceRoutePlanner.IsClear(from, waypoint, ship, RoutePlanner.Obstacles(world))
                    .Should().BeTrue($"leg {from} to {waypoint} of a trip to {destination}");
                from = waypoint;
            }
        }
    }

    [Test]
    public void Plan_ClearStraightPath_IsJustTheDestination()
    {
        var world = new GameWorld();
        var ship = world.Freighters[0];
        ship.Position = new Vector2(-250f, -250f);
        var destination = new Vector2(-250f, 1250f);

        var route = RoutePlanner.Plan(world, ship, destination);

        route.Should().Equal(destination);
    }

    [Test]
    public void Plan_DestinationInsideAPlanet_HasNoRoute()
    {
        var world = new GameWorld();
        var ship = world.Freighters[0];
        ship.Position = new Vector2(-250f, -250f);

        RoutePlanner.Plan(world, ship, world.Planets[3].Position).Should().BeEmpty();
    }

    [Test]
    public void Plan_ShipWalledInByPlanets_HasNoRoute()
    {
        var ring = Enumerable.Range(0, 8).Select(i =>
            new Planet(new Vector2(MathF.Cos(i * MathF.Tau / 8), MathF.Sin(i * MathF.Tau / 8)) * 95f)).ToList();
        var ship = new Freighter(Vector2.Zero, Faction.Blue);
        var world = new GameWorld(ring, [ship]);

        var route = RoutePlanner.Plan(world, ship, new Vector2(1000f, 0f));

        route.Should().BeEmpty();
        route.Should().Equal(ReferenceRoutePlanner.Plan(world, ship, new Vector2(1000f, 0f)));
    }

    [Test]
    public void Plan_DoesNotAllocateMuchGarbage()
    {
        // The reference planner allocated about 226 KB per call, which is what filled the nursery and, more to the
        // point, made each call slow in the browser's interpreter.
        var world = new GameWorld();
        var ship = world.Freighters[0];
        var trips = RandomTrips(world, 40).ToList();
        foreach (var (start, destination) in trips.Take(5))
        {
            ship.Position = start;
            RoutePlanner.Plan(world, ship, destination);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        foreach (var (start, destination) in trips)
        {
            ship.Position = start;
            RoutePlanner.Plan(world, ship, destination);
        }

        var perPlan = (GC.GetAllocatedBytesForCurrentThread() - before) / trips.Count;
        perPlan.Should().BeLessThan(32 * 1024);
    }

    [Test]
    public void IsClear_AgreesWithTheReferenceForAnySegment()
    {
        var world = new GameWorld();
        var ship = world.Freighters[0];
        var obstacles = RoutePlanner.Obstacles(world).ToArray();
        foreach (var (start, end) in RandomTrips(world, 1000))
        {
            RoutePlanner.IsClear(start, end, ship, world)
                .Should().Be(ReferenceRoutePlanner.IsClear(start, end, ship, obstacles), $"{start} to {end}");
        }
    }

    private static void AssertSameAsReference(GameWorld world, Ship ship, Vector2 start, Vector2 destination)
    {
        ship.Position = start;

        var route = RoutePlanner.Plan(world, ship, destination);
        var expected = ReferenceRoutePlanner.Plan(world, ship, destination);

        var because = $"from {start} to {destination}";
        (route.Count == 0).Should().Be(expected.Count == 0, because);
        if (route.Count > 0)
        {
            var expectedLength = Length(start, expected);
            Length(start, route).Should().BeApproximately(expectedLength, 0.01f + expectedLength * 1e-4f, because);
        }
    }

    [Test]
    public void Plan_AroundAPlanetsKeepOutRadius_MatchesTheReference()
    {
        // The keep-out radius is 34 + 12 + 20 = 66. Cases right at its edge, and with the ship already inside it
        // (which may leave outward), are where a wrong comparison or a wrong early-out would show.
        var world = new GameWorld();
        var ship = world.Freighters[0];
        var p = world.Planets[6].Position;
        var m = Planet.Radius + Freighter.Radius + 20f;
        (Vector2 Start, Vector2 Destination)[] trips =
        [
            (p + new Vector2(50f, 0f), p + new Vector2(60f, 0f)),
            (p + new Vector2(50f, 0f), p + new Vector2(-50f, 0f)),
            (p + new Vector2(50f, 0f), p + new Vector2(-400f, 0f)),
            (p + new Vector2(300f, 0f), p + new Vector2(-m, 0f)),
            (p + new Vector2(300f, 0f), p + new Vector2(-m - 0.001f, 0f)),
            (p + new Vector2(300f, 0f), p + new Vector2(-m + 0.001f, 0f)),
            (p + new Vector2(300f, 0f), p + new Vector2(300f, 0f)),
            (p, p),
            (new Vector2(940f, 540f), new Vector2(574f, 540f)),
        ];

        foreach (var (start, destination) in trips)
        {
            AssertSameAsReference(world, ship, start, destination);
        }
    }

    [Test]
    public void Plan_PastAHomeworldsSpaceDock_MatchesTheReference()
    {
        var world = new GameWorld();
        var ship = world.Freighters[0];
        var homeworld = world.Planets[0];
        homeworld.HasOperationalStructure(StructureType.SpaceDock).Should().BeTrue();
        var dock = homeworld.SpaceDock.Position;

        AssertSameAsReference(world, ship, dock + new Vector2(0f, -100f), dock + new Vector2(0f, 400f));
        AssertSameAsReference(world, ship, dock + new Vector2(-200f, 0f), dock + new Vector2(200f, 0f));
    }

    [Test]
    public void Plan_InAWorldWithNoPlanets_IsTheStraightLine()
    {
        var ship = new Freighter(Vector2.Zero, Faction.Blue);
        var world = new GameWorld([], [ship]);

        RoutePlanner.Plan(world, ship, new Vector2(500f, 300f)).Should().Equal(new Vector2(500f, 300f));
    }

    [TestCase(float.PositiveInfinity, 540f)]
    [TestCase(float.NegativeInfinity, 540f)]
    [TestCase(940f, float.PositiveInfinity)]
    public void Plan_ToAnInfiniteDestination_HasNoRoute(float x, float y)
    {
        var world = new GameWorld();
        var ship = world.Freighters[0];
        ship.Position = new Vector2(940f, 540f);

        RoutePlanner.Plan(world, ship, new Vector2(x, y)).Should().BeEmpty();
    }

    [Test]
    public void Plan_FromAnInfinitePosition_HasNoRoute()
    {
        var world = new GameWorld();
        var ship = world.Freighters[0];
        ship.Position = new Vector2(float.PositiveInfinity, 540f);

        RoutePlanner.Plan(world, ship, new Vector2(940f, 540f)).Should().BeEmpty();
    }
}
