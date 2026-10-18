using System.Numerics;

namespace GravityWell.Entities;

public sealed class Fighter
{
    public const int MaxHealth = 6;
    public const float Radius = 10f;
    public const float MaxSpeed = 260f;

    private static readonly Vector2 InitialPosition = new(430, 270);
    private static readonly Vector2 InitialVelocity = new(105, 0);

    public Faction Faction { get; init; } = Faction.Blue;
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public float Rotation { get; set; }
    public int Health { get; set; }

    public Vector2 Forward
    {
        get
        {
            var radians = Rotation * MathF.PI / 180f;
            return new Vector2(MathF.Sin(radians), -MathF.Cos(radians));
        }
    }

    public Fighter() => Reset();

    public void Reset()
    {
        Position = InitialPosition;
        Velocity = InitialVelocity;
        Rotation = 90f;
        Health = MaxHealth;
    }
}
