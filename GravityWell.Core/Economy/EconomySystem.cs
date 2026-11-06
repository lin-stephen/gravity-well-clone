using GravityWell.Core.Entities;
using GravityWell.Core.World;

namespace GravityWell.Core.Economy;

public sealed class EconomySystem
{
    private readonly EconomySettings settings;

    public EconomySystem(EconomySettings? settings = null)
    {
        this.settings = settings ?? new EconomySettings();
        this.settings.Validate();
    }

    public void Update(GameWorld world, float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        foreach (var planet in world.Planets)
        {
            var colonyActive = planet is { Health: > 0, IsColonized: true, Type: PlanetType.Habitable }
                               && planet.HasOperationalStructure(StructureType.Colony);
            UpdateInventory(planet.BaseResources, colonyActive,
                planet is { Health: > 0, IsColonized: true }
                && planet.HasOperationalStructure(StructureType.Base),
                deltaTime);
            UpdateInventory(planet.StarbaseResources, colonyActive,
                planet is { Health: > 0, IsColonized: true }
                && planet.HasOperationalStructure(StructureType.Starbase),
                deltaTime);
        }
    }

    private void UpdateInventory(ResourceInventory inventory, bool colonyActive, bool refineryActive,
        float deltaTime)
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
            var refining = inventory is { RawMaterials: > 0, FinishedMaterials: < ResourceInventory.FinishedCapacity };
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

            var untilSupply = supplying ? settings.SupplyIntervalSeconds - inventory.SupplyProgress : double.PositiveInfinity;
            var untilRefined =
                refining ? settings.RefiningIntervalSeconds - inventory.RefiningProgress : double.PositiveInfinity;
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

            if (inventory.RefiningProgress >= settings.RefiningIntervalSeconds)
            {
                inventory.RefiningProgress = 0;
                inventory.TryRefine();
            }

            if (inventory.SupplyProgress >= settings.SupplyIntervalSeconds)
            {
                inventory.SupplyProgress = 0;
                inventory.AddRawMaterials(1);
            }
        }
    }
}
