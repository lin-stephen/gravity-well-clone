namespace GravityWell.Core.Entities;

public sealed class ResourceInventory
{
    public const int RawCapacity = 20;
    public const int FinishedCapacity = 20;

    public int RawMaterials { get; private set; }
    public int FinishedMaterials { get; private set; }
    internal double SupplyProgress { get; set; }
    internal double RefiningProgress { get; set; }

    public void AddRawMaterials(int amount) =>
        RawMaterials = (int)Math.Clamp((long)RawMaterials + Math.Max(0, amount), 0, RawCapacity);

    public void AddFinishedMaterials(int amount) =>
        FinishedMaterials = (int)Math.Clamp((long)FinishedMaterials + Math.Max(0, amount), 0, FinishedCapacity);

    public bool TryRefine()
    {
        if (RawMaterials == 0 || FinishedMaterials == FinishedCapacity) return false;
        RawMaterials--;
        FinishedMaterials++;
        return true;
    }

    public bool TrySpendFinishedMaterial()
    {
        if (FinishedMaterials == 0) return false;
        FinishedMaterials--;
        return true;
    }
}
