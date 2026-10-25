namespace GravityWell.Core.Entities;

public sealed class PlanetStructure
{
    public PlanetStructure(StructureType type, StructureState state = StructureState.Planned)
    {
        Type = type;
        State = state;
        ConstructionProgress = state == StructureState.Operational ? 1f : 0f;
    }

    public StructureType Type { get; }
    public StructureState State { get; private set; }
    public float ConstructionProgress { get; private set; }

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
