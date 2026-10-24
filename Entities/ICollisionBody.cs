using System.Numerics;

namespace GravityWell.Entities;

public interface ICollisionBody
{
    Vector2 Position { get; }
    Vector2 Velocity { get; }
    float CollisionRadius { get; }
    int Health { get; set; }
}
