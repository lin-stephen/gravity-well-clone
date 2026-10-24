using System.Numerics;
using GravityWell.Entities;

namespace GravityWell.World;

public sealed class GameWorld
{
    private readonly List<Freighter> freighters;

    public Fighter Player { get; } = new();

    public IEnumerable<Ship> Ships
    {
        get
        {
            yield return Player;
            foreach (var freighter in freighters)
                if (freighter.State != FreighterState.Consumed)
                    yield return freighter;
        }
    }

    public IReadOnlyList<Planet> Planets { get; }
    public IReadOnlyList<Freighter> Freighters => freighters;

    public GameWorld(IEnumerable<Planet> planets, IEnumerable<Freighter> freighters)
    {
        Planets = planets.ToArray();
        this.freighters = freighters.ToList();
    }

    public GameWorld()
    {
        var homeworld = new Planet(new Vector2(430, 360), owner: Faction.Blue);
        homeworld.BuildHomeworld();

        var rivalHomeworld = new Planet(new Vector2(1900, 900), owner: Faction.Red);
        rivalHomeworld.BuildHomeworld();

        Planets =
        [
            homeworld,
            rivalHomeworld,
            new Planet(new Vector2(950, 95)),
            new Planet(new Vector2(1260, 530), PlanetType.Barren),
            new Planet(new Vector2(290, 760)),
        ];

        freighters =
        [
            new Freighter(homeworld.Position + new Vector2(-44f, 50f), Faction.Blue),
            new Freighter(homeworld.Position + new Vector2(0f, 58f), Faction.Blue),
            new Freighter(homeworld.Position + new Vector2(44f, 50f), Faction.Blue),
            new Freighter(rivalHomeworld.Position + new Vector2(-44f, 50f), Faction.Red),
            new Freighter(rivalHomeworld.Position + new Vector2(0f, 58f), Faction.Red),
            new Freighter(rivalHomeworld.Position + new Vector2(44f, 50f), Faction.Red),
        ];
        foreach (var ship in freighters)
        {
            var producer = Planets.MinBy(planet => Vector2.DistanceSquared(planet.Position, ship.Position))!;
            FaceOutward(ship, producer);
        }
    }

    public void AddFreighter(Planet producer, Faction faction)
    {
        // Search outward from the producer instead of spawning overlapping hulls.
        for (var ring = 0; ; ring++)
        for (var slot = 0; slot < 16; slot++)
        {
            var angle = MathF.PI / 2f + slot * MathF.Tau / 16;
            var position = producer.Position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) *
                (Planet.Radius + Freighter.Radius + 30f + ring * 30f);
            var blocked = Ships.Cast<ICollisionBody>().Concat(Planets)
                .Concat(Planets.Where(planet => planet.HasOperationalStructure(StructureType.SpaceDock))
                    .Select(planet => planet.SpaceDock))
                .Any(body => Vector2.Distance(position, body.Position) <
                    Freighter.Radius + body.CollisionRadius + 8f);
            if (blocked) continue;
            var ship = new Freighter(position, faction);
            FaceOutward(ship, producer);
            freighters.Add(ship);
            return;
        }
    }

    private static void FaceOutward(Freighter ship, Planet producer)
    {
        var outward = ship.Position - producer.Position;
        ship.Rotation = MathF.Atan2(outward.X, -outward.Y) * 180f / MathF.PI;
    }
}
