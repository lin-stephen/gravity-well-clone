using GravityWell.Core.Entities;
using GravityWell.Core.World;

namespace GravityWell.Core.Economy;

/// <summary>Each operational Base builds its Tech Center, then Comm Center, from its finished stores.</summary>
public sealed class BaseConstructionSystem
{
    private readonly BaseConstructionSettings settings;

    public BaseConstructionSystem(BaseConstructionSettings? settings = null)
    {
        this.settings = settings ?? new BaseConstructionSettings();
        this.settings.Validate();
    }

    public void Update(GameWorld world, float deltaTime)
    {
        if (!float.IsFinite(deltaTime) || deltaTime <= 0f)
        {
            return;
        }

        foreach (var planet in world.Planets)
        {
            if (!planet.IsColonized || planet.Health <= 0 || !planet.HasOperationalStructure(StructureType.Base))
            {
                foreach (var structure in planet.Structures)
                {
                    if (IsBaseFacility(structure.Type) && structure.State == StructureState.Building)
                    {
                        structure.Cancel();
                    }
                }
                continue;
            }

            double remaining = deltaTime;
            while (remaining > 0)
            {
                var site = FindBuildingFacility(planet);
                if (site is null)
                {
                    var type = !planet.HasOperationalStructure(StructureType.TechCenter)
                        ? StructureType.TechCenter : StructureType.CommCenter;
                    if (planet.HasOperationalStructure(type))
                    {
                        break;
                    }

                    var cost = type == StructureType.TechCenter ? settings.TechCenterCost : settings.CommCenterCost;
                    if (!planet.BaseResources.TrySpendFinishedMaterials(cost))
                    {
                        break;
                    }

                    site = FindPlannedFacility(planet, type) ?? planet.AddStructure(type);
                    site.BeginConstruction();
                }

                var untilComplete = (1d - site.ConstructionProgress) * settings.ConstructionDurationSeconds;
                var elapsed = Math.Min(remaining, untilComplete);
                site.AdvanceConstruction(elapsed >= untilComplete ? 1f : (float)(elapsed / settings.ConstructionDurationSeconds));
                remaining -= elapsed;
            }
        }
    }

    private static bool IsBaseFacility(StructureType type) =>
        type is StructureType.TechCenter or StructureType.CommCenter;

    private static PlanetStructure? FindBuildingFacility(Planet planet)
    {
        foreach (var structure in planet.Structures)
        {
            if (IsBaseFacility(structure.Type) && structure.State == StructureState.Building)
            {
                return structure;
            }
        }
        return null;
    }

    private static PlanetStructure? FindPlannedFacility(Planet planet, StructureType type)
    {
        foreach (var structure in planet.Structures)
        {
            if (structure.Type == type && structure.State == StructureState.Planned)
            {
                return structure;
            }
        }
        return null;
    }
}
