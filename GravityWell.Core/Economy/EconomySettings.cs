using System.ComponentModel.DataAnnotations;

namespace GravityWell.Core.Economy;

public sealed record EconomySettings
{
    /// <summary>Seconds for an active colony to deliver one raw unit to a refinery.</summary>
    [Range(0d, float.MaxValue, MinimumIsExclusive = true)]
    public float SupplyIntervalSeconds { get; init; } = 2f;

    /// <summary>Seconds for an active refinery to turn one raw unit into a finished unit.</summary>
    [Range(0d, float.MaxValue, MinimumIsExclusive = true)]
    public float RefiningIntervalSeconds { get; init; } = 2f;

    public void Validate() =>
        Validator.ValidateObject(this, new ValidationContext(this), validateAllProperties: true);
}
