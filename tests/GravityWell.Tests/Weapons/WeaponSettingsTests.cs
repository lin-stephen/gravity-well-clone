using System.ComponentModel.DataAnnotations;
using AwesomeAssertions;
using GravityWell.Core.Entities;
using GravityWell.Core.Weapons;

namespace GravityWell.Tests.Weapons;

public class WeaponSettingsTests
{
    [Test]
    public void Defaults_AreValid()
    {
        Assert.DoesNotThrow(new WeaponSettings().Validate);
    }

    [Test]
    public void Defaults_FriendlyFireIsOff()
    {
        new WeaponSettings().FriendlyFire.Should().BeFalse();
    }

    [Test]
    public void DefaultBoltSpeed_ExceedsFighterMaxSpeed()
    {
        new WeaponSettings().BoltSpeed.Should().BeGreaterThan(Fighter.MaxSpeed);
    }

    [Test]
    public void Validate_RejectsZeroBoltSpeed()
    {
        AssertRejects(new WeaponSettings { BoltSpeed = 0f }, nameof(WeaponSettings.BoltSpeed));
    }

    [Test]
    public void Validate_RejectsZeroBoltLifetime()
    {
        AssertRejects(new WeaponSettings { BoltLifetime = 0f }, nameof(WeaponSettings.BoltLifetime));
    }

    [Test]
    public void Validate_RejectsNegativeFireInterval()
    {
        AssertRejects(new WeaponSettings { FireInterval = -1f }, nameof(WeaponSettings.FireInterval));
    }

    [Test]
    public void Validate_RejectsZeroDamage()
    {
        AssertRejects(new WeaponSettings { Damage = 0 }, nameof(WeaponSettings.Damage));
    }

    [Test]
    public void Validate_RejectsZeroMaxBolts()
    {
        AssertRejects(new WeaponSettings { MaxBolts = 0 }, nameof(WeaponSettings.MaxBolts));
    }

    [Test]
    public void Validate_RejectsZeroImpactDuration()
    {
        AssertRejects(new WeaponSettings { ImpactDuration = 0f }, nameof(WeaponSettings.ImpactDuration));
    }

    [Test]
    public void Validate_AcceptsZeroFireInterval()
    {
        Assert.DoesNotThrow(new WeaponSettings { FireInterval = 0f }.Validate);
    }

    private static void AssertRejects(WeaponSettings settings, string member)
    {
        var exception = Assert.Throws<ValidationException>(settings.Validate);
        Assert.That(exception!.ValidationResult.MemberNames, Does.Contain(member));
    }
}
