using System.ComponentModel.DataAnnotations;
using GravityWell.Core.Physics;

namespace GravityWell.Tests;

public class DockingSettingsTests
{
    [Test]
    public void Validate_AcceptsDefaults()
    {
        Assert.DoesNotThrow(new DockingSettings().Validate);
    }

    [TestCase(0f, 0f)]
    [TestCase(180f, 2f)]
    public void Validate_AcceptsAngleAndDistanceBoundaries(float angle, float distance)
    {
        var settings = new DockingSettings { AlignmentDegrees = angle, CaptureDistance = distance };
        Assert.DoesNotThrow(settings.Validate);
    }

    [TestCase(nameof(DockingSettings.SpeedThreshold), 0f)]
    [TestCase(nameof(DockingSettings.AlignmentDegrees), -1f)]
    [TestCase(nameof(DockingSettings.AlignmentDegrees), 181f)]
    [TestCase(nameof(DockingSettings.CaptureDistance), -1f)]
    [TestCase(nameof(DockingSettings.DampingRate), 0f)]
    public void Validate_IdentifiesInvalidProperty(string property, float value)
    {
        var settings = property switch
        {
            nameof(DockingSettings.SpeedThreshold) => new DockingSettings { SpeedThreshold = value },
            nameof(DockingSettings.AlignmentDegrees) => new DockingSettings { AlignmentDegrees = value },
            nameof(DockingSettings.CaptureDistance) => new DockingSettings { CaptureDistance = value },
            nameof(DockingSettings.DampingRate) => new DockingSettings { DampingRate = value },
            _ => throw new ArgumentException("Unknown settings property", nameof(property))
        };

        var exception = Assert.Throws<ValidationException>(settings.Validate);
        Assert.That(exception!.ValidationResult.MemberNames, Does.Contain(property));
    }
}
