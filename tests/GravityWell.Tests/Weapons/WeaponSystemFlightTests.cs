using System.Numerics;
using AwesomeAssertions;
using GravityWell.Core.Entities;
using GravityWell.Core.Physics;
using GravityWell.Core.Weapons;

namespace GravityWell.Tests.Weapons;

public class WeaponSystemFlightTests
{
    [Test]
    public void Bolt_WithoutGravity_TravelsStraight()
    {
        var world = WeaponTestWorld.CreateWorld();
        var system = WeaponTestWorld.CreateSystem();
        system.Update(world, WeaponTestWorld.Fire, 0.1f);

        system.Update(world, WeaponTestWorld.Idle, 0.1f);

        world.Bolts[0].Position.ShouldBeNear(new Vector2(63f, 0f));
        world.Bolts[0].Velocity.ShouldBeNear(new Vector2(500f, 0f));
    }

    [Test]
    public void Bolt_WithGravity_BendsTowardPlanet()
    {
        var world = WeaponTestWorld.CreateWorld([new Planet(new Vector2(0f, -100f))]);
        var system = WeaponTestWorld.CreateSystem(gravity: true);
        system.Update(world, WeaponTestWorld.Fire, 0.02f);

        for (var i = 0; i < 30; i++)
        {
            system.Update(world, WeaponTestWorld.Idle, 0.02f);
        }

        var bolt = world.Bolts.Should().ContainSingle().Subject;
        bolt.Velocity.Y.Should().BeLessThan(0f);
        bolt.Position.Y.Should().BeLessThan(0f);
    }

    [Test]
    public void Bolt_PastLifetime_IsRemoved()
    {
        var world = WeaponTestWorld.CreateWorld();
        var system = WeaponTestWorld.CreateSystem(WeaponTestWorld.Settings(s => s with { BoltLifetime = 0.5f }));
        system.Update(world, WeaponTestWorld.Fire, 0.02f);

        system.Update(world, WeaponTestWorld.Idle, 0.3f);
        world.Bolts.Should().HaveCount(1);
        system.Update(world, WeaponTestWorld.Idle, 0.3f);

        world.Bolts.Should().BeEmpty();
    }

    [Test]
    public void Update_ZeroDeltaTime_DoesNotMoveBolts()
    {
        var world = WeaponTestWorld.CreateWorld();
        var system = WeaponTestWorld.CreateSystem(gravity: true);
        system.Update(world, WeaponTestWorld.Fire, 0.02f);
        var before = world.Bolts[0].Position;

        system.Update(world, WeaponTestWorld.Idle, 0f);

        world.Bolts.Should().ContainSingle();
        world.Bolts[0].Position.Should().Be(before);
        float.IsFinite(world.Bolts[0].Position.X).Should().BeTrue();
        world.Bolts[0].TimeRemaining.Should().Be(2f);
    }

    [Test]
    public void Update_VeryLargeDeltaTime_RemovesExpiredBolt()
    {
        var world = WeaponTestWorld.CreateWorld();
        var system = WeaponTestWorld.CreateSystem(gravity: true);
        system.Update(world, WeaponTestWorld.Fire, 0.02f);

        Assert.DoesNotThrow(() => system.Update(world, WeaponTestWorld.Idle, 5f));

        world.Bolts.Should().BeEmpty();
    }

    [Test]
    public void Impact_AgesAndExpires()
    {
        var world = WeaponTestWorld.CreateWorld();
        var system = WeaponTestWorld.CreateSystem(WeaponTestWorld.Settings(s => s with { ImpactDuration = 0.3f }));
        world.Impacts.Add(new Impact(Vector2.Zero));

        system.Update(world, WeaponTestWorld.Idle, 0.2f);
        world.Impacts.Should().ContainSingle().Which.Age.Should().BeApproximately(0.2f, 0.0001f);
        system.Update(world, WeaponTestWorld.Idle, 0.2f);

        world.Impacts.Should().BeEmpty();
    }

    [Test]
    public void Bolt_GravityChangesVelocityBeforeItMovesThePosition()
    {
        var world = WeaponTestWorld.CreateWorld([new Planet(new Vector2(13f, 200f))]);
        var system = new WeaponSystem(
            new PhysicsSystem(new PhysicsSettings { PlanetGravityStrength = 100_000f }), WeaponTestWorld.Settings());
        system.Update(world, WeaponTestWorld.Fire, 0.02f);

        system.Update(world, WeaponTestWorld.Idle, 0.1f);

        // 100000 / 200^2 = 2.5 units/s^2 straight down. The new velocity moves the bolt, so y = 0.25 * 0.1.
        world.Bolts[0].Velocity.Y.Should().BeApproximately(0.25f, 0.001f);
        world.Bolts[0].Position.Y.Should().BeApproximately(0.025f, 0.001f);
    }

    [Test]
    public void Bolt_WhoseLifetimeEndsExactlyOnAStep_IsRemoved()
    {
        var world = WeaponTestWorld.CreateWorld();
        var system = WeaponTestWorld.CreateSystem(WeaponTestWorld.Settings(s => s with { BoltLifetime = 0.5f }));
        system.Update(world, WeaponTestWorld.Fire, 0.02f);

        system.Update(world, WeaponTestWorld.Idle, 0.25f);
        world.Bolts.Should().HaveCount(1);
        system.Update(world, WeaponTestWorld.Idle, 0.25f);

        world.Bolts.Should().BeEmpty();
    }

    [Test]
    public void Impact_ThatReachesItsDurationExactly_IsRemoved()
    {
        var world = WeaponTestWorld.CreateWorld();
        var system = WeaponTestWorld.CreateSystem(WeaponTestWorld.Settings(s => s with { ImpactDuration = 0.4f }));
        world.Impacts.Add(new Impact(Vector2.Zero));

        system.Update(world, WeaponTestWorld.Idle, 0.2f);
        world.Impacts.Should().HaveCount(1);
        system.Update(world, WeaponTestWorld.Idle, 0.2f);

        world.Impacts.Should().BeEmpty();
    }

    [Test]
    public void Update_BadDeltaTime_LeavesBoltsAlone()
    {
        var world = WeaponTestWorld.CreateWorld();
        var system = WeaponTestWorld.CreateSystem(gravity: true);
        system.Update(world, WeaponTestWorld.Fire, 0.02f);
        var before = world.Bolts[0].Position;

        system.Update(world, WeaponTestWorld.Idle, -0.1f);
        system.Update(world, WeaponTestWorld.Idle, float.NaN);

        world.Bolts[0].Position.Should().Be(before);
        world.Bolts[0].TimeRemaining.Should().Be(2f);
    }

    [Test]
    public void Update_LargeDeltaTimeShorterThanLifetime_KeepsTheBoltFinite()
    {
        var world = WeaponTestWorld.CreateWorld([new Planet(new Vector2(0f, -100f))]);
        var system = WeaponTestWorld.CreateSystem(gravity: true);
        system.Update(world, WeaponTestWorld.Fire, 0.02f);

        system.Update(world, WeaponTestWorld.Idle, 1.9f);

        var bolt = world.Bolts.Should().ContainSingle().Subject;
        float.IsFinite(bolt.Position.X).Should().BeTrue();
        float.IsFinite(bolt.Position.Y).Should().BeTrue();
        float.IsFinite(bolt.Velocity.Length()).Should().BeTrue();
    }
}
