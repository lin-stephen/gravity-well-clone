using System.Numerics;
using GravityWell.Entities;

namespace GravityWell.World;

public sealed class GameWorld
{
    public static readonly Vector2 StarPosition = new(760, 360);
    public static readonly Vector2 PlanetPosition = new(430, 360);

    public Fighter Player { get; } = new();
}
