using System.Numerics;

namespace GravityWell.Core.Entities;

public sealed class Fighter : Ship
{
    public const int MaxHealth = 6;
    public const float Radius = 10f;
    public const float MaxSpeed = 260f;
    /// <summary>How far ahead of the centre the nose is drawn, and where a fired bolt starts.</summary>
    public const float NoseDistance = 13f;
    /// <summary>How the AI flies this fighter. Fixed at spawn; ignored for the player.</summary>
    public FighterBehavior Behavior { get; init; } = FighterBehavior.Peaceful;
    public override int HealthCapacity => MaxHealth;
    public override float CollisionRadius => Radius;
    public override float SpeedLimit => MaxSpeed;
    public override float ThrustAcceleration => 230f;
    public override float TurnRate => 168f;

    public Fighter() => Reset();

    public void Reset()
    {
        Undock();
        ReleasedDockingTarget = null;
        Controls = default;
        Position = new Vector2(430, 270);
        Velocity = new Vector2(105, 0);
        Rotation = 90f;
        Health = MaxHealth;
    }
}
