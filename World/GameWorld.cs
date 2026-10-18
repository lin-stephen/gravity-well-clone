using System.Numerics;
using GravityWell.Entities;

namespace GravityWell.World;

public sealed class GameWorld
{
    public Fighter Player { get; } = new();

    public IReadOnlyList<Planet> Planets { get; }

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
    }
}
