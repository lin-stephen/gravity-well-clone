using System.ComponentModel.DataAnnotations;
using GravityWell.Core.Economy;

namespace GravityWell.Tests.Economy;

public class EconomySettingsTests
{
    [Test]
    public void Defaults_AreValid()
    {
        Assert.DoesNotThrow(new EconomySettings().Validate);
    }

    [Test]
    public void Validate_RejectsZeroSupplyInterval()
    {
        AssertRejects(new EconomySettings { SupplyIntervalSeconds = 0f }, nameof(EconomySettings.SupplyIntervalSeconds));
    }

    [Test]
    public void Validate_RejectsNegativeRefiningInterval()
    {
        AssertRejects(new EconomySettings { RefiningIntervalSeconds = -1f }, nameof(EconomySettings.RefiningIntervalSeconds));
    }

    [Test]
    public void Constructor_ValidatesSettings()
    {
        Assert.Throws<ValidationException>(() => new EconomySystem(new EconomySettings { SupplyIntervalSeconds = 0f }));
    }

    private static void AssertRejects(EconomySettings settings, string member)
    {
        var exception = Assert.Throws<ValidationException>(settings.Validate);
        Assert.That(exception!.ValidationResult.MemberNames, Does.Contain(member));
    }
}
