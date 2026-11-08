using System.Numerics;
using GravityWell.Core.Entities;
using GravityWell.Core.World;

namespace GravityWell.Core.Colonization.Policies;

/// <summary>Reusable cargo trips from friendly colonies to Base and Starbase stores without local supply.</summary>
internal sealed class FreighterSupplyPolicy : IFreighterPolicy
{
    private const float LoadingIntervalSeconds = 2f;

    public void UpdateActive(GameWorld world, float deltaTime)
    {
        var elapsed = float.IsFinite(deltaTime) ? Math.Max(0f, deltaTime) : 0f;
        foreach (var ship in world.Freighters)
        {
            if (ship.IsDestroyed || ship.Health <= 0 || ship.State != FreighterState.Traveling ||
                ship.Mission == FreighterMission.Construction || ship.TargetPlanet is not { } target)
            {
                continue;
            }

            var valid = ship.Mission == FreighterMission.LoadCargo
                ? CanLoad(target, ship.Faction)
                : NeedsSupply(target, ship.Faction, ship.Mission);
            if (!valid)
            {
                ship.Recall();
                continue;
            }
            if (!ship.HasArrived)
            {
                continue;
            }

            if (ship.Mission == FreighterMission.LoadCargo)
            {
                ship.LoadingProgress += elapsed;
                while (ship.LoadingProgress >= LoadingIntervalSeconds && ship.RawMaterials < Freighter.CargoCapacity)
                {
                    ship.LoadRawMaterial();
                    ship.LoadingProgress -= LoadingIntervalSeconds;
                }
                if (ship.RawMaterials == Freighter.CargoCapacity)
                {
                    ship.Recall();
                }
            }
            else
            {
                ship.DeliverRawMaterials(Inventory(target, ship.Mission));
                var other = ship.Mission == FreighterMission.SupplyBase
                    ? FreighterMission.SupplyStarbase : FreighterMission.SupplyBase;
                if (ship.RawMaterials > 0 && NeedsSupply(target, ship.Faction, other))
                {
                    ship.DeliverRawMaterials(Inventory(target, other));
                }
                ship.Recall();
            }
        }
    }

    public void DispatchIdle(GameWorld world)
    {
        foreach (var ship in world.Freighters)
        {
            if (ship.IsDestroyed || ship.Health <= 0 || ship.State != FreighterState.Idle)
            {
                continue;
            }

            if (ship.RawMaterials > 0 && TryDispatchDelivery(world, ship))
            {
                continue;
            }
            if (ship.RawMaterials >= Freighter.CargoCapacity)
            {
                continue;
            }

            var source = world.Planets.Where(planet => CanLoad(planet, ship.Faction))
                .MinBy(planet => Vector2.DistanceSquared(ship.Position, planet.Position));
            if (source is not null)
            {
                ship.DispatchTransport(source, FreighterMission.LoadCargo);
            }
        }
    }

    private static bool TryDispatchDelivery(GameWorld world, Freighter ship)
    {
        Planet? destination = null;
        var mission = FreighterMission.SupplyBase;
        var distance = float.PositiveInfinity;
        foreach (var planet in world.Planets)
        {
            if (world.Freighters.Any(other => other.TargetPlanet == planet &&
                    other.State is FreighterState.Traveling or FreighterState.Building))
            {
                continue;
            }
            var nextMission = NeedsSupply(planet, ship.Faction, FreighterMission.SupplyBase)
                ? FreighterMission.SupplyBase : FreighterMission.SupplyStarbase;
            if (!NeedsSupply(planet, ship.Faction, nextMission))
            {
                continue;
            }
            var candidateDistance = Vector2.DistanceSquared(ship.Position, planet.Position);
            if (candidateDistance < distance)
            {
                distance = candidateDistance;
                destination = planet;
                mission = nextMission;
            }
        }
        if (destination is null)
        {
            return false;
        }
        ship.DispatchTransport(destination, mission);
        return true;
    }

    private static bool CanLoad(Planet planet, Faction faction) =>
        planet.Health > 0 && planet.Owner == faction && planet.Type == PlanetType.Habitable &&
        planet.HasOperationalStructure(StructureType.Colony);

    private static bool NeedsSupply(Planet planet, Faction faction, FreighterMission mission)
    {
        if (planet.Health <= 0 || planet.Owner != faction || planet.HasOperationalStructure(StructureType.Colony) ||
            !planet.HasOperationalStructure(mission == FreighterMission.SupplyBase ? StructureType.Base : StructureType.Starbase))
        {
            return false;
        }
        var stock = Inventory(planet, mission);
        return stock.RawMaterials + stock.FinishedMaterials < ResourceInventory.RawCapacity;
    }

    private static ResourceInventory Inventory(Planet planet, FreighterMission mission) =>
        mission == FreighterMission.SupplyBase ? planet.BaseResources : planet.StarbaseResources;
}
