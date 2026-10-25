using System.Numerics;

namespace GravityWell.Core.Entities;

public sealed class Freighter : Ship
{
    public const float Radius = 12f;
    public const float Speed = 90f;
    public const int CargoCapacity = 1;
    public const int MaxHealth = 6;

    public Freighter(Vector2 position, Faction faction)
    {
        Position = position;
        Faction = faction;
        Health = MaxHealth;
    }

    public override float CollisionRadius => Radius;
    public override float SpeedLimit => Speed;
    public override float ThrustAcceleration => 180f;
    public override float TurnRate => 168f;
    public Vector2 Heading => Forward;
    public override int HealthCapacity => MaxHealth;
    public FreighterState State { get; private set; } = FreighterState.Idle;
    public int RawMaterials { get; private set; } = CargoCapacity;
    public Planet? TargetPlanet { get; private set; }
    public StructureType? TargetStructure { get; private set; }

    public void Dispatch(Planet planet, StructureType structureType)
    {
        if (State != FreighterState.Idle || RawMaterials < CargoCapacity)
        {
            throw new InvalidOperationException("Only a fully loaded idle freighter can be dispatched.");
        }

        TargetPlanet = planet;
        TargetStructure = structureType;
        State = FreighterState.Traveling;
    }

    /// <summary>Cancels the order of a freighter that is still on its way, keeping its cargo so it can be sent elsewhere.</summary>
    public void Recall()
    {
        if (State != FreighterState.Traveling)
        {
            return;
        }

        if (DockingTarget is not null)
        {
            Undock();
        }

        TargetPlanet = null;
        TargetStructure = null;
        State = FreighterState.Idle;
    }

    public bool BeginBuilding()
    {
        if (State != FreighterState.Traveling || !IsDocked || DockingTarget != TargetPlanet)
        {
            return false;
        }

        State = FreighterState.Building;
        return true;
    }

    public void Consume()
    {
        if (State != FreighterState.Building)
        {
            return;
        }

        RawMaterials = 0;
        State = FreighterState.Consumed;
    }
}
