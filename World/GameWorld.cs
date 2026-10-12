using System.Numerics;
using GravityWell.Entities;

namespace GravityWell.World;

public sealed class GameWorld
{
    public Fighter Player { get; } = new();

    public IReadOnlyList<Planet> Planets { get; } =
    [
        new Planet(new Vector2(430, 360), isColonized: true),
        new Planet(new Vector2(800, 170)),
        new Planet(new Vector2(1040, 480)),
        new Planet(new Vector2(330, 640)),
    ];
}
