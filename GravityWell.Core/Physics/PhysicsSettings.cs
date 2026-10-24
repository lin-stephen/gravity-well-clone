using System.ComponentModel.DataAnnotations;

namespace GravityWell.Core.Physics;

public sealed record PhysicsSettings
{
    [Range(0d, float.MaxValue)]
    public float PlanetGravityStrength { get; init; } = 1_350_000f * 0.09f;

    [Range(0d, float.MaxValue, MinimumIsExclusive = true)]
    public float MinimumGravityDistanceSquared { get; init; } = 2_500f;

    public void Validate() =>
        Validator.ValidateObject(this, new ValidationContext(this), validateAllProperties: true);
}
