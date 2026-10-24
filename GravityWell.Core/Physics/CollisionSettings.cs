using System.ComponentModel.DataAnnotations;

namespace GravityWell.Core.Physics;

public sealed record CollisionSettings
{
    [Range(0d, 1d)]
    public float Restitution { get; init; } = 0.8f;

    [Range(0d, float.MaxValue)]
    public float DamageSpeedThreshold { get; init; } = 100f;

    [Range(0d, float.MaxValue, MinimumIsExclusive = true)]
    public float SpeedPerDamagePoint { get; init; } = 25f;

    public void Validate() =>
        Validator.ValidateObject(this, new ValidationContext(this), validateAllProperties: true);
}
