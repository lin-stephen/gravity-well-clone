using System.Numerics;
using GravityWell.Core.Entities;
using GravityWell.Core.World;

namespace GravityWell.Core.Colonization;

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
            if (freighter.State == FreighterState.Traveling && freighter.TargetPlanet is { } target)
            {
                KeepOrderCurrent(freighter, target);
            }

            if (freighter.HasArrived)
            {
                BeginConstruction(freighter);
            }

            if (freighter.BuildSite is not { } site)
            {
                continue;
            }

            if (site.State == StructureState.Destroyed)
            {
                // The build site was shot out from under the freighter. It is sent again if the planet still needs it.
                freighter.Abandon();
                continue;
            }

            site.AdvanceConstruction(deltaTime / BuildDurationSeconds);

            if (site.State == StructureState.Operational)
            {
                freighter.Consume();
            }
        }
    }

    /// <summary>
    /// A planet can change hands before the freighter arrives, and it must not then build for the new owner: the
    /// freighter is recalled. A structure can also be destroyed on the way, so the planet needs something other than
    /// what the freighter was sent to build. The order is changed in place, since a freighter already touching the
    /// planet must not be sent away and back, which its own dock would refuse. With nothing to build it is recalled.
    /// </summary>
    private static void KeepOrderCurrent(Freighter freighter, Planet planet)
    {
        var next = GetNextStructure(planet);
        if ((planet.Owner is { } owner && owner != freighter.Faction) || next is null)
        {
            freighter.Recall();
        }
        else if (next != freighter.TargetStructure)
        {
            freighter.Redirect(next.Value);
        }
    }

    private static void BeginConstruction(Freighter freighter)
    {
        var site = freighter.TargetPlanet!.AddStructure(freighter.TargetStructure!.Value);
        site.BeginConstruction();
        freighter.BeginBuilding(site);
    }

    private static void DispatchIdleFreighters(GameWorld world)
    {
        foreach (var planet in world.Planets.Where(NeedsExpansion))
        {
            if (world.Freighters.Any(freighter =>
                    freighter.TargetPlanet == planet && freighter.State != FreighterState.Consumed))
            {
                continue;
            }

            // The closest idle freighter wins, so one recalled mid-flight beats a freighter sitting at home.
            var freighter = world.Freighters
                .Where(candidate => candidate.Faction == planet.Owner && candidate.State == FreighterState.Idle)
                .MinBy(candidate => Vector2.DistanceSquared(candidate.Position, planet.Position));
            var nextStructure = GetNextStructure(planet);
            if (freighter is null || nextStructure is null)
            {
                continue;
            }

            freighter.Dispatch(planet, nextStructure.Value);
        }
    }

    private static bool NeedsExpansion(Planet planet) =>
        planet.IsColonized && GetNextStructure(planet).HasValue;

    private static StructureType? GetNextStructure(Planet planet)
    {
        if (!planet.HasStructure(StructureType.Base))
        {
            return StructureType.Base;
        }

        if (!planet.HasOperationalStructure(StructureType.Base))
        {
            return null;
        }

        if (planet.Type == PlanetType.Habitable && !planet.HasStructure(StructureType.Colony))
        {
            return StructureType.Colony;
        }

        if (planet.Type == PlanetType.Habitable && !planet.HasOperationalStructure(StructureType.Colony))
        {
            return null;
        }

        if (!planet.HasStructure(StructureType.Starbase))
        {
            return StructureType.Starbase;
        }

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
