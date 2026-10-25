using System.Numerics;
using AwesomeAssertions;
using GravityWell.Core.Entities;
using GravityWell.Core.Weapons;

namespace GravityWell.Tests.Weapons;

public class WeaponSystemFiringTests
{
    [Test]
    public void Fire_SpawnsBoltAtNoseAlongFacing()
    {
        var world = WeaponTestWorld.CreateWorld();
        world.Player.Rotation = 90f;
        world.Player.Velocity = new Vector2(30f, 0f);
        var system = WeaponTestWorld.CreateSystem();

        system.Update(world, WeaponTestWorld.Fire, 0.02f);

        world.Bolts.Should().ContainSingle();
        var bolt = world.Bolts[0];
        bolt.Position.ShouldBeNear(new Vector2(13f, 0f));
        bolt.Velocity.ShouldBeNear(new Vector2(530f, 0f));
        bolt.Shooter.Should().BeSameAs(world.Player);
        bolt.TimeRemaining.Should().Be(2f);
    }

    [Test]
    public void Fire_FacingUp_InheritsSidewaysVelocity()
    {
        var world = WeaponTestWorld.CreateWorld();
        world.Player.Rotation = 0f;
        world.Player.Velocity = new Vector2(40f, 0f);
        var system = WeaponTestWorld.CreateSystem();

        system.Update(world, WeaponTestWorld.Fire, 0.02f);

        world.Bolts[0].Velocity.ShouldBeNear(new Vector2(40f, -500f));
        world.Bolts[0].Position.ShouldBeNear(new Vector2(0f, -13f));
    }

    [Test]
    public void NoFire_NoBolt()
    {
        var world = WeaponTestWorld.CreateWorld();
        var system = WeaponTestWorld.CreateSystem();

        system.Update(world, WeaponTestWorld.Idle, 0.02f);

        world.Bolts.Should().BeEmpty();
    }

    [Test]
    public void Fire_TwiceWithinInterval_FiresOnce()
    {
        var world = WeaponTestWorld.CreateWorld();
        var system = WeaponTestWorld.CreateSystem();

        for (var i = 0; i < 3; i++)
        {
            system.Update(world, WeaponTestWorld.Fire, 0.1f);
        }

        world.Bolts.Should().HaveCount(1);

        system.Update(world, WeaponTestWorld.Fire, 0.1f);

        world.Bolts.Should().HaveCount(2);
    }

    [Test]
    public void Fire_AtMaxBolts_DoesNotExceedCap()
    {
        var world = WeaponTestWorld.CreateWorld();
        var system = WeaponTestWorld.CreateSystem(WeaponTestWorld.Settings(s => s with { MaxBolts = 2, FireInterval = 0f }));

        for (var i = 0; i < 4; i++)
        {
            system.Update(world, WeaponTestWorld.Fire, 0.01f);
        }

        world.Bolts.Should().HaveCount(2);
    }

    [Test]
    public void Fire_WhileDocked_StillFires()
    {
        var planet = new Planet(new Vector2(5000f, 5000f));
        var world = WeaponTestWorld.CreateWorld([planet]);
        world.Player.BeginDocking(planet, Vector2.UnitY);
        world.Player.CompleteDocking();
        var system = WeaponTestWorld.CreateSystem();

        system.Update(world, WeaponTestWorld.Fire, 0.02f);

        world.Bolts.Should().ContainSingle();
    }

    [Test]
    public void Constructor_ValidatesSettings()
    {
        Assert.Throws<System.ComponentModel.DataAnnotations.ValidationException>(() =>
            WeaponTestWorld.CreateSystem(new WeaponSettings { BoltSpeed = 0f }));
    }

    [Test]
    public void Fire_FromOffOriginPlayer_StartsAtTheNose()
    {
        var world = WeaponTestWorld.CreateWorld();
        world.Player.Position = new Vector2(100f, 50f);
        world.Player.Rotation = 0f;
        var system = WeaponTestWorld.CreateSystem();

        system.Update(world, WeaponTestWorld.Fire, 0.02f);

        world.Bolts[0].Position.ShouldBeNear(new Vector2(100f, 37f));
    }

    [Test]
    public void Update_NegativeDeltaTime_DoesNotLengthenTheCooldown()
    {
        var world = WeaponTestWorld.CreateWorld();
        var system = WeaponTestWorld.CreateSystem();
        system.Update(world, WeaponTestWorld.Fire, 0.02f);

        system.Update(world, WeaponTestWorld.Fire, -1f);
        for (var i = 0; i < 3; i++)
        {
            system.Update(world, WeaponTestWorld.Fire, 0.1f);
        }

        world.Bolts.Should().HaveCount(2);
    }

    [Test]
    public void Update_NanDeltaTime_DoesNotSwitchOffTheCooldown()
    {
        var world = WeaponTestWorld.CreateWorld();
        var system = WeaponTestWorld.CreateSystem();
        system.Update(world, WeaponTestWorld.Fire, 0.02f);

        system.Update(world, WeaponTestWorld.Fire, float.NaN);
        system.Update(world, WeaponTestWorld.Fire, 0.02f);

        world.Bolts.Should().ContainSingle();
    }

    [Test]
    public void Fire_CooldownCountsDownBeforeTheFireCheck()
    {
        var world = WeaponTestWorld.CreateWorld();
        var system = WeaponTestWorld.CreateSystem();
        system.Update(world, WeaponTestWorld.Fire, 0.01f);

        system.Update(world, WeaponTestWorld.Fire, 0.25f);

        world.Bolts.Should().HaveCount(2);
    }
}
