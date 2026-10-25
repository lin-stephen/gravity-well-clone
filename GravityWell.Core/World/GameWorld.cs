using System.Numerics;
using GravityWell.Core.Entities;

namespace GravityWell.Core.World;

public sealed class GameWorld
{
    private readonly List<Freighter> freighters;
    private readonly List<Fighter> aiFighters;

    public Fighter Player { get; } = new();

    public IEnumerable<Ship> Ships
    {
        get
        {
            foreach (var fighter in Fighters)
            {
                yield return fighter;
            }

            foreach (var freighter in freighters)
            {
                if (freighter.State != FreighterState.Consumed)
                {
                    yield return freighter;
                }
            }
        }
    }

    public IReadOnlyList<Planet> Planets { get; }
    public IReadOnlyList<Freighter> Freighters => freighters;
    public IReadOnlyList<Fighter> AiFighters => aiFighters;
    public List<Bolt> Bolts { get; } = [];
    public List<Impact> Impacts { get; } = [];

    public IEnumerable<Fighter> Fighters
    {
        get
        {
            yield return Player;
            foreach (var fighter in aiFighters)
            {
                yield return fighter;
            }
        }
    }

    public GameWorld(IEnumerable<Planet> planets, IEnumerable<Freighter> freighters,
        IEnumerable<Fighter>? aiFighters = null)
    {
        Planets = planets.ToArray();
        this.freighters = freighters.ToList();
        this.aiFighters = aiFighters?.ToList() ?? [];
    }

    public GameWorld()
    {
        var homeworld = new Planet(new Vector2(430, 360), owner: Faction.Blue);
        homeworld.BuildHomeworld();

        var rivalHomeworld = new Planet(new Vector2(1900, 900), owner: Faction.Red);
        rivalHomeworld.BuildHomeworld();

        var yellowHomeworld = new Planet(new Vector2(1750, 230), owner: Faction.Yellow);
        yellowHomeworld.BuildHomeworld();

        Planets =
        [
            homeworld,
            rivalHomeworld,
            yellowHomeworld,
            new Planet(new Vector2(950, 95)),
            new Planet(new Vector2(1260, 530), PlanetType.Barren),
            new Planet(new Vector2(290, 760)),
            new Planet(new Vector2(640, 540)),
            new Planet(new Vector2(1000, 760)),
            new Planet(new Vector2(1560, 640)),
            new Planet(new Vector2(1480, 200)),
        ];

        freighters =
        [
            new Freighter(homeworld.Position + new Vector2(-44f, 50f), Faction.Blue),
            new Freighter(homeworld.Position + new Vector2(0f, 58f), Faction.Blue),
            new Freighter(homeworld.Position + new Vector2(44f, 50f), Faction.Blue),
            new Freighter(rivalHomeworld.Position + new Vector2(-44f, 50f), Faction.Red),
            new Freighter(rivalHomeworld.Position + new Vector2(0f, 58f), Faction.Red),
            new Freighter(rivalHomeworld.Position + new Vector2(44f, 50f), Faction.Red),
            new Freighter(yellowHomeworld.Position + new Vector2(-44f, 50f), Faction.Yellow),
            new Freighter(yellowHomeworld.Position + new Vector2(0f, 58f), Faction.Yellow),
            new Freighter(yellowHomeworld.Position + new Vector2(44f, 50f), Faction.Yellow),
        ];
        foreach (var ship in freighters)
        {
            var producer = Planets.MinBy(planet => Vector2.DistanceSquared(planet.Position, ship.Position))!;
            FaceOutward(ship, producer);
        }
        aiFighters = CreateAiFighters(Planets, Player.Faction,
            faction => faction == Faction.Yellow ? FighterBehavior.Null : FighterBehavior.Peaceful);
    }

    /// <summary>
    /// One fighter per faction that owns a homeworld, except the player's faction. <paramref name="behaviorFor"/>
    /// chooses how each faction's fighter is flown; the default is <see cref="FighterBehavior.Peaceful"/>.
    /// </summary>
    public static List<Fighter> CreateAiFighters(IEnumerable<Planet> planets, Faction playerFaction,
        Func<Faction, FighterBehavior>? behaviorFor = null)
    {
        behaviorFor ??= _ => FighterBehavior.Peaceful;
        var fighters = new List<Fighter>();
        foreach (var homeworld in planets)
        {
            if (homeworld.Owner is not { } faction || faction == playerFaction ||
                !homeworld.HasOperationalStructure(StructureType.TechCenter) ||
                fighters.Any(fighter => fighter.Faction == faction))
            {
                continue;
            }

            fighters.Add(new Fighter
            {
                Faction = faction,
                Behavior = behaviorFor(faction),
                Position = homeworld.Position + new Vector2(0f, 100f),
                Velocity = Vector2.Zero,
                Rotation = 180f, // Faces outward, away from the homeworld.
            });
        }
        return fighters;
    }

    public void AddFreighter(Planet producer, Faction faction)
    {
        // Search outward from the producer instead of spawning overlapping hulls.
        for (var ring = 0; ; ring++)
        {
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
            if (blocked)
                {
                    continue;
                }

                var ship = new Freighter(position, faction);
            FaceOutward(ship, producer);
            freighters.Add(ship);
            return;
        }
        }
    }

    private static void FaceOutward(Freighter ship, Planet producer)
    {
        var outward = ship.Position - producer.Position;
        ship.Rotation = MathF.Atan2(outward.X, -outward.Y) * 180f / MathF.PI;
    }
}
