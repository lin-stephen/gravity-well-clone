using GravityWell.Entities;
using GravityWell.World;

namespace GravityWell.Economy;

public sealed class RepairSystem
{
    public const float SurfaceRepairIntervalSeconds = 2f;
    public const float OrbitalRepairIntervalSeconds = 1f;

    public void Update(GameWorld world, float deltaTime)
    {
        foreach (var ship in world.Ships)
            UpdateShip(ship, deltaTime);
    }

    private static void UpdateShip(Ship ship, float deltaTime)
    {
        if (!ship.IsDocked)
        {
            ship.ResetRepair();
            return;
        }
        var planet = ship.DockingTarget switch
        {
            Planet surface => surface,
            SpaceDockBody dock => dock.Planet,
            _ => null
        };
        var orbital = ship.DockingTarget is SpaceDockBody;
        var requiredFacility = orbital ? StructureType.Starbase : StructureType.Base;
        if (planet is null || planet.Owner != ship.Faction || planet.Health <= 0 || ship.Health <= 0 ||
            !planet.HasOperationalStructure(requiredFacility) ||
            (orbital && !planet.HasOperationalStructure(StructureType.SpaceDock)))
        {
            ship.ResetRepair(RepairStatus.Unavailable);
            return;
        }
        if (ship.Health >= ship.HealthCapacity)
        {
            ship.ResetRepair(RepairStatus.FullyRepaired);
            return;
        }
        var inventory = orbital ? planet.StarbaseResources : planet.BaseResources;
        if (inventory.FinishedMaterials == 0)
        {
            ship.ResetRepair(RepairStatus.AwaitingSupplies);
            return;
        }
        var interval = orbital ? OrbitalRepairIntervalSeconds : SurfaceRepairIntervalSeconds;
        ship.RepairProgress += Math.Max(0f, deltaTime);
        while (ship.RepairProgress >= interval && ship.Health < ship.HealthCapacity &&
            inventory.TrySpendFinishedMaterial())
        {
            ship.Health++;
            ship.RepairProgress -= interval;
        }
        if (ship.Health >= ship.HealthCapacity)
            ship.ResetRepair(RepairStatus.FullyRepaired);
        else if (inventory.FinishedMaterials == 0)
            ship.ResetRepair(RepairStatus.AwaitingSupplies);
        else ship.RepairStatus = RepairStatus.Repairing;
    }
}
