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
        // Factions without an operational Tech Center or Space Dock don't build and keep their stored progress.
        // Ordered by faction so freighters are added in a stable order regardless of planet order.
        var producers = world.Planets
            .Where(planet => planet.Owner is not null &&
                (planet.HasOperationalStructure(StructureType.TechCenter) ||
                 planet.HasOperationalStructure(StructureType.SpaceDock)))
            .GroupBy(planet => planet.Owner!.Value)
            .OrderBy(group => group.Key)
            .ToList();

        foreach (var group in producers)
        {
            var faction = group.Key;
            var producer = group.First();
            var activeCount = world.Freighters.Count(freighter =>
                freighter.Faction == faction && freighter.State != FreighterState.Consumed);
            if (activeCount >= FreighterCapacityPerFaction)
            {
                productionProgress[faction] = 0f;
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
