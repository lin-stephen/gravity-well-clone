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
            new Planet(new Vector2(800, 170)),
            new Planet(new Vector2(1040, 480), PlanetType.Barren),
            new Planet(new Vector2(330, 640)),
        ];
    }
}
