using System.Numerics;

namespace GravityWell.Entities;

public sealed class Planet
{
    public const float Radius = 22f;

    public Planet(Vector2 position, bool isColonized = false)
    {
        Position = position;
        IsColonized = isColonized;
    }

    public Vector2 Position { get; }
    public bool IsColonized { get; private set; }

    public void Colonize() => IsColonized = true;
}
