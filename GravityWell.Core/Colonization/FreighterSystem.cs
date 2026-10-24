using GravityWell.Entities;
using GravityWell.World;

namespace GravityWell.Colonization;

public sealed class FreighterSystem
{
    private const float BuildDurationSeconds = 3f;
    private const float ProductionDurationSeconds = 5f;
    private const int FreighterCapacityPerFaction = 3;
    private readonly Dictionary<Faction, float> productionProgress = [];

    public void Update(GameWorld world, float deltaTime)
    {
        UpdateActiveFreighters(world, deltaTime);
        DispatchIdleFreighters(world);
        ProduceReplacementFreighters(world, deltaTime);
    }

    private static void UpdateActiveFreighters(GameWorld world, float deltaTime)
    {
        foreach (var freighter in world.Freighters)
        {
            if (freighter.BeginBuilding())
                BeginConstruction(freighter);

            if (freighter.State != FreighterState.Building || freighter.TargetPlanet is null ||
                freighter.TargetStructure is null)
                continue;

            var structure = freighter.TargetPlanet.Structures.Single(candidate =>
                candidate.Type == freighter.TargetStructure.Value);
            structure.AdvanceConstruction(deltaTime / BuildDurationSeconds);

            if (structure.State == StructureState.Operational)
                freighter.Consume();
        }
    }

    private static void BeginConstruction(Freighter freighter)
    {
        var structure = freighter.TargetPlanet!.AddStructure(freighter.TargetStructure!.Value);
        structure.BeginConstruction();
    }

    private static void DispatchIdleFreighters(GameWorld world)
    {
        foreach (var planet in world.Planets.Where(NeedsExpansion))
        {
            if (world.Freighters.Any(freighter =>
                    freighter.TargetPlanet == planet && freighter.State != FreighterState.Consumed))
                continue;

            var freighter = world.Freighters.FirstOrDefault(candidate =>
                candidate.Faction == planet.Owner && candidate.State == FreighterState.Idle);
            var nextStructure = GetNextStructure(planet);
            if (freighter is null || nextStructure is null)
                continue;

            freighter.Dispatch(planet, nextStructure.Value);
        }
    }

    private static bool NeedsExpansion(Planet planet) =>
        planet.IsColonized && GetNextStructure(planet).HasValue;

    private static StructureType? GetNextStructure(Planet planet)
    {
        if (!planet.HasStructure(StructureType.Base))
            return StructureType.Base;

        if (!planet.HasOperationalStructure(StructureType.Base))
            return null;

        if (planet.Type == PlanetType.Habitable && !planet.HasStructure(StructureType.Colony))
            return StructureType.Colony;

        if (planet.Type == PlanetType.Habitable && !planet.HasOperationalStructure(StructureType.Colony))
            return null;

        if (!planet.HasStructure(StructureType.Starbase))
            return StructureType.Starbase;

        return null;
    }

    private void ProduceReplacementFreighters(GameWorld world, float deltaTime)
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
                continue;

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
