using System.Numerics;

namespace GravityWell.Core.Entities;

/// <summary>A cannon shot in flight. <see cref="Shooter"/> is never hurt by its own bolt.</summary>
public sealed class Bolt(Ship shooter, Vector2 position, Vector2 velocity, float lifetime)
{
    public Ship Shooter { get; } = shooter;
    public Faction Faction => Shooter.Faction;
    public Vector2 Position { get; set; } = position;
    public Vector2 Velocity { get; set; } = velocity;
    public float TimeRemaining { get; set; } = lifetime;
}
