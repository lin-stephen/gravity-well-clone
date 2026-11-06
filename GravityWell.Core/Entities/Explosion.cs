using System.Numerics;

namespace GravityWell.Core.Entities;

public sealed class Explosion(Vector2 position)
{
    public const float DurationSeconds = 0.8f;
    public Vector2 Position { get; } = position;
    public float Age { get; set; }
}
