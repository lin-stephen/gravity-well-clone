using System.Numerics;

namespace GravityWell.Core.Entities;

public sealed class Planet : ICollisionBody
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
        SpaceDock = new SpaceDockBody(this);
        Type = type;
        Owner = owner;
    }

    public float CollisionRadius => Radius;
    public float InverseMass => 0f;
    public void ApplyCollision(Vector2 displacement, Vector2 velocityChange) { }

    public const int MaxHealth = 100;
    public int Health { get; set; } = MaxHealth;
    public Vector2 Velocity => Vector2.Zero;

    public ResourceInventory BaseResources { get; } = new();
    public ResourceInventory StarbaseResources { get; } = new();
    public SpaceDockBody SpaceDock { get; }
    public Vector2 Position { get; }
    public PlanetType Type { get; }
    public Faction? Owner { get; private set; }
    public bool IsColonized => Owner.HasValue;
    public IReadOnlyList<PlanetStructure> Structures => structures;

    /// <summary>
    /// Takes an unowned planet, or one owned by another faction that has no base. A base, even one still
    /// being built, guards a planet from being taken.
    /// </summary>
    public void Claim(Faction faction)
    {
        if (Owner is null || (Owner != faction && !HasStructure(StructureType.Base)))
        {
            Owner = faction;
        }
    }

    public bool HasStructure(StructureType type) =>
        structures.Any(structure => structure.Type == type && structure.State != StructureState.Destroyed);

    public bool HasOperationalStructure(StructureType type) =>
        structures.Any(structure => structure.Type == type && structure.State == StructureState.Operational);

    public bool CanAddStructure(StructureType type)
    {
        if (!Owner.HasValue || HasStructure(type))
        {
            return false;
        }

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
        {
            throw new InvalidOperationException($"Cannot add {type} to this planet.");
        }

        // A destroyed structure of this type gives way to its replacement, so a type never appears twice.
        structures.RemoveAll(existing => existing.Type == type && existing.State == StructureState.Destroyed);
        var structure = new PlanetStructure(type, state);
        structures.Add(structure);
        return structure;
    }

    public void BuildHomeworld()
    {
        foreach (var type in Enum.GetValues<StructureType>())
        {
            AddStructure(type, StructureState.Operational);
        }

        BaseResources.AddRawMaterials(10);
        BaseResources.AddFinishedMaterials(10);
        StarbaseResources.AddRawMaterials(10);
        StarbaseResources.AddFinishedMaterials(10);
    }
}
