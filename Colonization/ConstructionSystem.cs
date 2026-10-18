using GravityWell.Entities;
using GravityWell.World;

namespace GravityWell.Colonization;

public sealed class ConstructionSystem
{
    private const float BuildDurationSeconds = 3f;

    public void Update(GameWorld world, float deltaTime)
    {
        foreach (var planet in world.Planets.Where(planet => planet.IsColonized))
        {
            var active = planet.Structures.FirstOrDefault(structure =>
                structure.State is StructureState.Planned or StructureState.Building);

            if (active is not null)
            {
                active.BeginConstruction();
                active.AdvanceConstruction(deltaTime / BuildDurationSeconds);
                continue;
            }

            QueueNextExpansionStructure(planet);
        }
    }

    private static void QueueNextExpansionStructure(Planet planet)
    {
        if (!planet.HasStructure(StructureType.Base))
        {
            planet.AddStructure(StructureType.Base);
            return;
        }

        if (planet.Type == PlanetType.Habitable && !planet.HasStructure(StructureType.Colony))
        {
            planet.AddStructure(StructureType.Colony);
            return;
        }

        if (!planet.HasStructure(StructureType.Starbase))
            planet.AddStructure(StructureType.Starbase);
    }
}
