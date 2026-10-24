namespace GravityWell.Physics;

public sealed record CollisionSettings
{
    public float Restitution { get; init; } = 0.8f;
    public float DamageSpeedThreshold { get; init; } = 100f;
    public float SpeedPerDamagePoint { get; init; } = 25f;

    public void Validate()
    {
        if (!float.IsFinite(Restitution) || Restitution is < 0f or > 1f)
            throw new ArgumentOutOfRangeException(nameof(Restitution), "Restitution must be between zero and one.");
        if (!float.IsFinite(DamageSpeedThreshold) || DamageSpeedThreshold < 0f)
            throw new ArgumentOutOfRangeException(nameof(DamageSpeedThreshold), "Damage speed threshold must be finite and nonnegative.");
        if (!float.IsFinite(SpeedPerDamagePoint) || SpeedPerDamagePoint <= 0f)
            throw new ArgumentOutOfRangeException(nameof(SpeedPerDamagePoint), "Speed per damage point must be finite and positive.");
    }
}
