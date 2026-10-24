using System.ComponentModel.DataAnnotations;
using GravityWell.Core.Physics;

namespace GravityWell.Tests.Physics;

public class CollisionSettingsTests
{
    [Test]
    public void Validate_AcceptsDefaults()
    {
        Assert.DoesNotThrow(new CollisionSettings().Validate);
    }

    [TestCase(0f, 0f, 1f)]
    [TestCase(1f, 10f, 5f)]
    public void Validate_AcceptsRestitutionAndThresholdBoundaries(float restitution, float threshold, float speedPerDamagePoint)
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
    public void Validate_RejectsInvalidRestitution(float restitution)
    {
        var settings = new CollisionSettings { Restitution = restitution };

        var exception = Assert.Throws<ValidationException>(settings.Validate);
        Assert.That(exception!.ValidationResult.MemberNames, Does.Contain(nameof(CollisionSettings.Restitution)));
    }

    [Test]
    public void Validate_RejectsNegativeDamageSpeedThreshold()
    {
        var settings = new CollisionSettings { DamageSpeedThreshold = -1f };

        var exception = Assert.Throws<ValidationException>(settings.Validate);
        Assert.That(exception!.ValidationResult.MemberNames, Does.Contain(nameof(CollisionSettings.DamageSpeedThreshold)));
    }

    [Test]
    public void Validate_RejectsZeroSpeedPerDamagePoint()
    {
        var settings = new CollisionSettings { SpeedPerDamagePoint = 0f };

        var exception = Assert.Throws<ValidationException>(settings.Validate);
        Assert.That(exception!.ValidationResult.MemberNames, Does.Contain(nameof(CollisionSettings.SpeedPerDamagePoint)));
    }
}
