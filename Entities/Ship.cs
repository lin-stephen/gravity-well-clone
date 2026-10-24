using System.Numerics;
using GravityWell.Input;

namespace GravityWell.Entities;

public abstract class Ship : ICollisionBody
{
    public abstract float CollisionRadius { get; }
    public abstract float SpeedLimit { get; }
    public abstract float ThrustAcceleration { get; }
    public abstract float TurnRate { get; }
    public ShipControls Controls { get; set; }
    public ICollisionBody? ReleasedDockingTarget { get; set; }
    public float InverseMass => 1f;

    public void ApplyCollision(Vector2 displacement, Vector2 velocityChange)
    {
        Position += displacement;
        Velocity += velocityChange;
    }

    public ICollisionBody? DockingTarget { get; private set; }
    public bool IsDocked { get; private set; }
    public Vector2 DockingNormal { get; private set; }

    public void BeginDocking(ICollisionBody target, Vector2 normal)
    {
        DockingTarget = target;
        DockingNormal = normal;
        IsDocked = false;
    }

    public void CompleteDocking() => IsDocked = true;

    public void Undock()
    {
        ReleasedDockingTarget = DockingTarget;
        DockingTarget = null;
        IsDocked = false;
    }

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
}
