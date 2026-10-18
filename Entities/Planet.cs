using System.Numerics;

namespace GravityWell.Entities;

public sealed class Planet
{
    // In the original game's battle and landing views, a planet is roughly
    // three fighter lengths across.
    public const float Radius = 34f;
    private readonly List<PlanetStructure> structures = [];

    public Planet(
        Vector2 position,
        PlanetType type = PlanetType.Habitable,
        Faction? owner = null)
    {
        Position = position;
        Type = type;
        Owner = owner;
    }

    public Vector2 Position { get; }
    public PlanetType Type { get; }
    public Faction? Owner { get; private set; }
    public bool IsColonized => Owner.HasValue;
    public IReadOnlyList<PlanetStructure> Structures => structures;

    public void Claim(Faction faction) => Owner ??= faction;

    public bool HasStructure(StructureType type) =>
        structures.Any(structure => structure.Type == type && structure.State != StructureState.Destroyed);

    public bool HasOperationalStructure(StructureType type) =>
        structures.Any(structure => structure.Type == type && structure.State == StructureState.Operational);

    public bool CanAddStructure(StructureType type)
    {
        if (!Owner.HasValue || HasStructure(type))
            return false;

        return type switch
        {
            StructureType.Colony => Type == PlanetType.Habitable && HasOperationalStructure(StructureType.Base),
            StructureType.TechCenter or StructureType.CommCenter => HasOperationalStructure(StructureType.Base),
            StructureType.Starbase => HasOperationalStructure(StructureType.Base),
            StructureType.SpaceDock or StructureType.SensorArray => HasOperationalStructure(StructureType.Starbase),
            _ => true
        };
    }

    public PlanetStructure AddStructure(
        StructureType type,
        StructureState state = StructureState.Planned)
    {
        if (!CanAddStructure(type))
            throw new InvalidOperationException($"Cannot add {type} to this planet.");

        var structure = new PlanetStructure(type, state);
        structures.Add(structure);
        return structure;
    }

    public void BuildHomeworld()
    {
        foreach (var type in Enum.GetValues<StructureType>())
            AddStructure(type, StructureState.Operational);
    }
}
