using System.Numerics;

namespace GravityWell.Core.Entities;

// The dock glyph is offset from the shared starbase orbital anchor.
public sealed class SpaceDockBody(Planet planet) : ICollisionBody
{
    public Planet Planet { get; } = planet;
    public Vector2 Position => Planet.Position + new Vector2(-10f, -64f);
    public Vector2 Velocity => Vector2.Zero;
    public float CollisionRadius => 14f;
    public float InverseMass => 0f;
    public int Health { get => Planet.Health; set => Planet.Health = value; }
    public void ApplyCollision(Vector2 displacement, Vector2 velocityChange) { }
}
