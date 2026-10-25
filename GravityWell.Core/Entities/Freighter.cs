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

    /// <summary>The structure under construction while the freighter is building, so nobody has to look it up again.</summary>
    public PlanetStructure? BuildSite { get; private set; }

    /// <summary>True once the freighter is docked at the planet it was sent to, ready to start building.</summary>
    public bool HasArrived => State == FreighterState.Traveling && IsDocked && DockingTarget == TargetPlanet;

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

    /// <summary>
    /// Changes what a traveling freighter will build when the planet's needs have changed on the way. It keeps its
    /// cargo, its flight and its dock, so one already touching the planet carries on instead of leaving.
    /// Does nothing unless the freighter is traveling.
    /// </summary>
    public void Redirect(StructureType structureType)
    {
        if (State == FreighterState.Traveling)
        {
            TargetStructure = structureType;
        }
    }

    /// <summary>
    /// Gives up a build whose structure was destroyed under it: the freighter lets go of its dock and returns to
    /// idle with its cargo, so it can be sent again. Does nothing unless the freighter is building.
    /// </summary>
    public void Abandon()
    {
        if (State != FreighterState.Building)
        {
            return;
        }

        if (DockingTarget is not null)
        {
            Undock();
        }

        TargetPlanet = null;
        TargetStructure = null;
        BuildSite = null;
        State = FreighterState.Idle;
    }

    /// <summary>Starts building the given structure. The freighter must already have arrived at its target planet.</summary>
    public void BeginBuilding(PlanetStructure site)
    {
        if (!HasArrived)
        {
            throw new InvalidOperationException("A freighter can only start building once docked at its target planet.");
        }

        BuildSite = site;
        State = FreighterState.Building;
    }

    public void Consume()
    {
        if (State != FreighterState.Building)
        {
            return;
        }

        RawMaterials = 0;
        BuildSite = null;
        State = FreighterState.Consumed;
    }
}
