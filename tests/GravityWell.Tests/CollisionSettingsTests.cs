using GravityWell.Physics;

namespace GravityWell.Tests;

public class CollisionSettingsTests
{
    [TestCase(0f, 0f, 1f)]
    [TestCase(1f, 10f, 5f)]
    [TestCase(0.8f, 100f, 25f)]
    public void Validate_AcceptsValidSettings(float restitution, float threshold, float speedPerDamagePoint)
    {
        var settings = new CollisionSettings
        {
            Restitution = restitution,
            DamageSpeedThreshold = threshold,
            SpeedPerDamagePoint = speedPerDamagePoint
        };

        Assert.DoesNotThrow(settings.Validate);
    }

    [TestCase(-1f)]
    [TestCase(1.1f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.NegativeInfinity)]
    public void Validate_RejectsInvalidRestitution(float restitution)
    {
        var settings = new CollisionSettings { Restitution = restitution };

        var exception = Assert.Throws<ArgumentOutOfRangeException>(settings.Validate);
        Assert.That(exception!.ParamName, Is.EqualTo(nameof(CollisionSettings.Restitution)));
    }

    [TestCase(-1f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.NegativeInfinity)]
    public void Validate_RejectsInvalidDamageSpeedThreshold(float threshold)
    {
        var settings = new CollisionSettings { DamageSpeedThreshold = threshold };

        var exception = Assert.Throws<ArgumentOutOfRangeException>(settings.Validate);
        Assert.That(exception!.ParamName, Is.EqualTo(nameof(CollisionSettings.DamageSpeedThreshold)));
    }

    [TestCase(0f)]
    [TestCase(-1f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(float.NegativeInfinity)]
    public void Validate_RejectsInvalidSpeedPerDamagePoint(float speedPerDamagePoint)
    {
        var settings = new CollisionSettings { SpeedPerDamagePoint = speedPerDamagePoint };

        var exception = Assert.Throws<ArgumentOutOfRangeException>(settings.Validate);
        Assert.That(exception!.ParamName, Is.EqualTo(nameof(CollisionSettings.SpeedPerDamagePoint)));
    }
}
