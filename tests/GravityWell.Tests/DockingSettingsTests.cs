using GravityWell.Physics;

namespace GravityWell.Tests;

public class DockingSettingsTests
{
    [Test]
    public void Defaults_PreserveGameBalance()
    {
        var settings = new DockingSettings();

        Assert.Multiple(() =>
        {
            Assert.That(settings.SpeedThreshold, Is.EqualTo(60f));
            Assert.That(settings.AlignmentDegrees, Is.EqualTo(30f));
            Assert.That(settings.CaptureDistance, Is.EqualTo(4f));
            Assert.That(settings.DampingRate, Is.EqualTo(8f));
            Assert.DoesNotThrow(settings.Validate);
        });
    }

    [TestCase(0f, 0f)]
    [TestCase(180f, 2f)]
    public void Validate_AcceptsAngleAndDistanceBoundaries(float angle, float distance)
    {
        var settings = new DockingSettings { AlignmentDegrees = angle, CaptureDistance = distance };
        Assert.DoesNotThrow(settings.Validate);
    }

    [TestCase(nameof(DockingSettings.SpeedThreshold), 0f)]
    [TestCase(nameof(DockingSettings.SpeedThreshold), -1f)]
    [TestCase(nameof(DockingSettings.AlignmentDegrees), -1f)]
    [TestCase(nameof(DockingSettings.AlignmentDegrees), 181f)]
    [TestCase(nameof(DockingSettings.CaptureDistance), -1f)]
    [TestCase(nameof(DockingSettings.DampingRate), 0f)]
    [TestCase(nameof(DockingSettings.DampingRate), -1f)]
    [TestCase(nameof(DockingSettings.SpeedThreshold), float.NaN)]
    [TestCase(nameof(DockingSettings.AlignmentDegrees), float.NaN)]
    [TestCase(nameof(DockingSettings.CaptureDistance), float.NaN)]
    [TestCase(nameof(DockingSettings.DampingRate), float.NaN)]
    [TestCase(nameof(DockingSettings.SpeedThreshold), float.PositiveInfinity)]
    [TestCase(nameof(DockingSettings.AlignmentDegrees), float.PositiveInfinity)]
    [TestCase(nameof(DockingSettings.CaptureDistance), float.PositiveInfinity)]
    [TestCase(nameof(DockingSettings.DampingRate), float.PositiveInfinity)]
    [TestCase(nameof(DockingSettings.SpeedThreshold), float.NegativeInfinity)]
    [TestCase(nameof(DockingSettings.AlignmentDegrees), float.NegativeInfinity)]
    [TestCase(nameof(DockingSettings.CaptureDistance), float.NegativeInfinity)]
    [TestCase(nameof(DockingSettings.DampingRate), float.NegativeInfinity)]
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

        var exception = Assert.Throws<ArgumentOutOfRangeException>(settings.Validate);
        Assert.That(exception!.ParamName, Is.EqualTo(property));
    }
}
