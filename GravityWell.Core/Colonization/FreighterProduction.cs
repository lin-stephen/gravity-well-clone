using GravityWell.Core.Entities;
using GravityWell.Core.World;

namespace GravityWell.Core.Colonization;

/// <summary>Builds replacement freighters at a Tech Center or Space Dock, up to a fixed fleet size per faction.</summary>
internal sealed class FreighterProduction
{
    private const float ProductionDurationSeconds = 5f;
    private const int FreighterCapacityPerFaction = 3;
    private readonly Dictionary<Faction, float> productionProgress = [];

    public void Update(GameWorld world, float deltaTime)
    {
        foreach (var faction in Enum.GetValues<Faction>())
        {
            var activeCount = world.Freighters.Count(freighter =>
                freighter.Faction == faction && freighter.State != FreighterState.Consumed);
            if (activeCount >= FreighterCapacityPerFaction)
            {
                productionProgress[faction] = 0f;
                continue;
            }

            var producer = world.Planets.FirstOrDefault(planet =>
                planet.Owner == faction &&
                (planet.HasOperationalStructure(StructureType.TechCenter) ||
                 planet.HasOperationalStructure(StructureType.SpaceDock)));
            if (producer is null)
            {
                continue;
            }

            productionProgress.TryGetValue(faction, out var progress);
            progress += deltaTime / ProductionDurationSeconds;
            if (progress < 1f)
            {
                productionProgress[faction] = progress;
                continue;
            }

            world.AddFreighter(producer, faction);
            productionProgress[faction] = 0f;
        }
    }
}
