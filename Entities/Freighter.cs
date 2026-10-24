using System.Numerics;

namespace GravityWell.Entities;

public sealed class Freighter : ICollisionBody
{
    private Vector2 collisionVelocity;

    public const float Radius = 12f;
    public const float Speed = 90f;
    public const int CargoCapacity = 1;

    public Freighter(Vector2 position, Faction faction)
    {
        Position = position;
        Faction = faction;
    }

    public float CollisionRadius => Radius;
    public float InverseMass => 1f;

    public void ApplyCollision(Vector2 displacement, Vector2 velocityChange)
    {
        Position += displacement;
        Velocity += velocityChange;
        collisionVelocity += velocityChange;
    }

    public const int MaxHealth = 6;
    public int Health { get; set; } = MaxHealth;
    public Vector2 Velocity { get; private set; }

    public Vector2 Position { get; private set; }
    public Vector2 Heading { get; private set; } = -Vector2.UnitY;
    public Faction Faction { get; }
    public FreighterState State { get; private set; } = FreighterState.Idle;
    public int RawMaterials { get; private set; } = CargoCapacity;
    public Planet? TargetPlanet { get; private set; }
    public StructureType? TargetStructure { get; private set; }

    public void Dispatch(Planet planet, StructureType structureType)
    {
        if (State != FreighterState.Idle || RawMaterials < CargoCapacity)
            throw new InvalidOperationException("Only a fully loaded idle freighter can be dispatched.");

        TargetPlanet = planet;
        TargetStructure = structureType;
        State = FreighterState.Traveling;
    }

    public bool Travel(float deltaTime)
    {
        if (deltaTime <= 0f || State == FreighterState.Consumed)
            return false;

        Position += collisionVelocity * deltaTime;
        collisionVelocity *= MathF.Exp(-2f * deltaTime);
        Velocity = collisionVelocity;
        if (State != FreighterState.Traveling || TargetPlanet is null)
            return false;

        var offset = TargetPlanet.Position - Position;
        var distance = offset.Length();
        var arrivalDistance = Planet.Radius + Radius;
        var travelDistance = Speed * deltaTime;
        if (distance > arrivalDistance)
        {
            Heading = Vector2.Normalize(offset);
            Velocity += Heading * Speed;
        }
        if (distance <= arrivalDistance + travelDistance)
        {
            if (distance > arrivalDistance)
                Position += Vector2.Normalize(offset) * (distance - arrivalDistance);
            State = FreighterState.Building;
            return true;
        }

        Heading = Vector2.Normalize(offset);
        Position += Heading * travelDistance;
        return false;
    }

    public void Consume()
    {
        if (State != FreighterState.Building)
            return;

        RawMaterials = 0;
        State = FreighterState.Consumed;
    }
}
