namespace GravityWell.Physics;

public sealed record DockingSettings
{
    public float SpeedThreshold { get; init; } = 60f;
    public float AlignmentDegrees { get; init; } = 30f;
    public float CaptureDistance { get; init; } = 4f;
    public float DampingRate { get; init; } = 8f;

    public void Validate()
    {
        if (!float.IsFinite(SpeedThreshold) || SpeedThreshold <= 0f)
            throw new ArgumentOutOfRangeException(nameof(SpeedThreshold), "Speed threshold must be finite and positive.");
        if (!float.IsFinite(AlignmentDegrees) || AlignmentDegrees is < 0f or > 180f)
            throw new ArgumentOutOfRangeException(nameof(AlignmentDegrees), "Alignment degrees must be between zero and 180.");
        if (!float.IsFinite(CaptureDistance) || CaptureDistance < 0f)
            throw new ArgumentOutOfRangeException(nameof(CaptureDistance), "Capture distance must be finite and nonnegative.");
        if (!float.IsFinite(DampingRate) || DampingRate <= 0f)
            throw new ArgumentOutOfRangeException(nameof(DampingRate), "Damping rate must be finite and positive.");
    }
}
