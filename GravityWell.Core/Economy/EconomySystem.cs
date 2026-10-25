using GravityWell.Core.Entities;
using GravityWell.Core.World;

namespace GravityWell.Core.Economy;

public sealed class EconomySystem
{
    // Each active refinery receives one raw unit every two seconds from its colony.
    public const float SupplyIntervalSeconds = 2f;
    public const float RefiningIntervalSeconds = 2f;

    public void Update(GameWorld world, float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        foreach (var planet in world.Planets)
        {
            var colonyActive = planet.Health > 0 && planet.IsColonized &&
                planet.Type == PlanetType.Habitable && planet.HasOperationalStructure(StructureType.Colony);
            UpdateInventory(planet.BaseResources, colonyActive,
                planet.Health > 0 && planet.IsColonized && planet.HasOperationalStructure(StructureType.Base), deltaTime);
            UpdateInventory(planet.StarbaseResources, colonyActive,
                planet.Health > 0 && planet.IsColonized && planet.HasOperationalStructure(StructureType.Starbase), deltaTime);
        }
    }

    private static void UpdateInventory(ResourceInventory inventory, bool colonyActive, bool refineryActive, float deltaTime)
    {
        if (!refineryActive)
        {
            inventory.SupplyProgress = 0;
            inventory.RefiningProgress = 0;
            return;
        }
        // Advance to supply/refining events in time order. New raw materials
        // cannot receive refining time from before they actually arrived.
        double remaining = deltaTime;
        while (remaining > 0)
        {
            var supplying = colonyActive && inventory.RawMaterials < ResourceInventory.RawCapacity;
            var refining = inventory.RawMaterials > 0 &&
                inventory.FinishedMaterials < ResourceInventory.FinishedCapacity;
            if (!supplying)
            {
                inventory.SupplyProgress = 0;
            }

            if (!refining)
            {
                inventory.RefiningProgress = 0;
            }

            if (!supplying && !refining)
            {
                break;
            }

            var untilSupply = supplying ? SupplyIntervalSeconds - inventory.SupplyProgress : double.PositiveInfinity;
            var untilRefined = refining ? RefiningIntervalSeconds - inventory.RefiningProgress : double.PositiveInfinity;
            var elapsed = Math.Min(remaining, Math.Min(untilSupply, untilRefined));
            if (supplying)
            {
                inventory.SupplyProgress += elapsed;
            }

            if (refining)
            {
                inventory.RefiningProgress += elapsed;
            }

            remaining -= elapsed;

            if (inventory.RefiningProgress >= RefiningIntervalSeconds)
            {
                inventory.RefiningProgress = 0;
                inventory.TryRefine();
            }
            if (inventory.SupplyProgress >= SupplyIntervalSeconds)
            {
                inventory.SupplyProgress = 0;
                inventory.AddRawMaterials(1);
            }
        }
    }
}
