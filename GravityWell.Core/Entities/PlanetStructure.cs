namespace GravityWell.Core.Entities;

public sealed class PlanetStructure
{
    public PlanetStructure(StructureType type, StructureState state = StructureState.Planned)
    {
        Type = type;
        State = state;
        ConstructionProgress = state == StructureState.Operational ? 1f : 0f;
        Health = state == StructureState.Destroyed ? 0 : MaxHealth;
    }

    public const int MaxHealth = 4;

    public StructureType Type { get; }
    public StructureState State { get; private set; }
    public float ConstructionProgress { get; private set; }
    public int Health { get; private set; }

    /// <summary>A structure that is being built or standing can be shot. A planned or destroyed one is not there.</summary>
    public bool IsTargetable => State is StructureState.Building or StructureState.Operational;

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || !IsTargetable)
        {
            return;
        }

        Health = Math.Max(0, Health - damage);
        if (Health == 0)
        {
            State = StructureState.Destroyed;
        }
    }

    public void BeginConstruction()
    {
        if (State == StructureState.Planned)
        {
            State = StructureState.Building;
        }
    }

    public void AdvanceConstruction(float progress)
    {
        if (State != StructureState.Building || progress <= 0f)
        {
            return;
        }

        ConstructionProgress = Math.Clamp(ConstructionProgress + progress, 0f, 1f);
        if (ConstructionProgress >= 1f)
        {
            State = StructureState.Operational;
        }
    }
}
