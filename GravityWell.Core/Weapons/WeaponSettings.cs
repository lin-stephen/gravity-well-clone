using System.ComponentModel.DataAnnotations;

namespace GravityWell.Core.Weapons;

public sealed record WeaponSettings
{
    /// <summary>Speed added along the nose, on top of the fighter's own velocity.</summary>
    [Range(0d, float.MaxValue, MinimumIsExclusive = true)]
    public float BoltSpeed { get; init; } = 520f;

    /// <summary>Seconds a bolt flies before it fades out.</summary>
    [Range(0d, float.MaxValue, MinimumIsExclusive = true)]
    public float BoltLifetime { get; init; } = 1.6f;

    /// <summary>Minimum seconds between two shots.</summary>
    [Range(0d, float.MaxValue)]
    public float FireInterval { get; init; } = 0.25f;

    [Range(1, int.MaxValue)]
    public int Damage { get; init; } = 1;

    /// <summary>When false, a bolt never hurts the shooter's own faction.</summary>
    public bool FriendlyFire { get; init; }

    [Range(1, int.MaxValue)]
    public int MaxBolts { get; init; } = 32;

    /// <summary>Seconds a hit flash stays on screen.</summary>
    [Range(0d, float.MaxValue, MinimumIsExclusive = true)]
    public float ImpactDuration { get; init; } = 0.3f;

    public void Validate() =>
        Validator.ValidateObject(this, new ValidationContext(this), validateAllProperties: true);
}
