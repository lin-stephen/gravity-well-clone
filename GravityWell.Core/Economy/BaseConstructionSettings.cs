using System.ComponentModel.DataAnnotations;
using GravityWell.Core.Entities;

namespace GravityWell.Core.Economy;

public sealed record BaseConstructionSettings
{
    [Range(1, ResourceInventory.FinishedCapacity)]
    public int TechCenterCost { get; init; } = 5;

    [Range(1, ResourceInventory.FinishedCapacity)]
    public int CommCenterCost { get; init; } = 5;

    [Range(0d, float.MaxValue, MinimumIsExclusive = true)]
    public float ConstructionDurationSeconds { get; init; } = 5f;

    public void Validate() =>
        Validator.ValidateObject(this, new ValidationContext(this), validateAllProperties: true);
}
