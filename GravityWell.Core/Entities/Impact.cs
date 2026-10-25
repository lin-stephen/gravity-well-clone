using System.Numerics;

namespace GravityWell.Core.Entities;

/// <summary>A short-lived flash where a bolt hit something. The weapon system ages it and the renderer draws it.</summary>
public sealed class Impact(Vector2 position)
{
    public Vector2 Position { get; } = position;
    public float Age { get; set; }
}
