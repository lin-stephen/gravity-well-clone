using System.Numerics;
using GravityWell.Entities;

namespace GravityWell.World;

public sealed class GameWorld
{
    private readonly List<Freighter> freighters;

    public Fighter Player { get; } = new();

    public IReadOnlyList<Planet> Planets { get; }
    public IReadOnlyList<Freighter> Freighters => freighters;

    public GameWorld()
    {
        var homeworld = new Planet(new Vector2(430, 360), owner: Faction.Blue);
        homeworld.BuildHomeworld();

        Planets =
        [
            homeworld,
            new Planet(new Vector2(950, 95)),
            new Planet(new Vector2(1260, 530), PlanetType.Barren),
            new Planet(new Vector2(290, 760)),
        ];

        freighters =
        [
            new Freighter(homeworld.Position + new Vector2(-44f, 50f), Faction.Blue),
            new Freighter(homeworld.Position + new Vector2(0f, 58f), Faction.Blue),
            new Freighter(homeworld.Position + new Vector2(44f, 50f), Faction.Blue),
        ];
    }

    public void AddFreighter(Planet producer, Faction faction)
    {
        var activeCount = freighters.Count(freighter =>
            freighter.Faction == faction && freighter.State != FreighterState.Consumed);
        var horizontalOffset = (activeCount % 3 - 1) * 20f;
        var spawnPosition = producer.Position + new Vector2(horizontalOffset, Planet.Radius + 24f);
        freighters.Add(new Freighter(spawnPosition, faction));
    }
}
