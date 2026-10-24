using System.ComponentModel.DataAnnotations;

namespace GravityWell.Physics;

public sealed record DockingSettings
{
    [Range(0d, float.MaxValue, MinimumIsExclusive = true)]
    public float SpeedThreshold { get; init; } = 60f;

    [Range(0d, 180d)]
    public float AlignmentDegrees { get; init; } = 30f;

    [Range(0d, float.MaxValue)]
    public float CaptureDistance { get; init; } = 4f;

    [Range(0d, float.MaxValue, MinimumIsExclusive = true)]
    public float DampingRate { get; init; } = 8f;

    public void Validate() =>
        Validator.ValidateObject(this, new ValidationContext(this), validateAllProperties: true);
}
