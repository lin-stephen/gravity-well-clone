using System.Numerics;
using AwesomeAssertions;
using GravityWell.Core.Entities;

namespace GravityWell.Tests.Weapons;

public class WeaponSystemShipHitTests
{
    [Test]
    public void Bolt_HitsEnemyFighter()
    {
        var enemy = WeaponTestWorld.Enemy(new Vector2(200f, 0f));
        var world = WeaponTestWorld.CreateWorld(aiFighters: [enemy]);
        var system = WeaponTestWorld.CreateSystem();

        WeaponTestWorld.Run(system, world, 0.6f, fire: true);

        enemy.Health.Should().Be(Fighter.MaxHealth - 1);
        world.Bolts.Should().BeEmpty();
        var impact = world.Impacts.Should().ContainSingle().Subject;
        impact.Position.X.Should().BeApproximately(190f, 1f);
    }

    [Test]
    public void Bolt_HitsEnemyFreighter()
    {
        var freighter = new Freighter(new Vector2(200f, 0f), Faction.Red);
        var world = WeaponTestWorld.CreateWorld(freighters: [freighter]);
        var system = WeaponTestWorld.CreateSystem();

        WeaponTestWorld.Run(system, world, 0.6f, fire: true);

        freighter.Health.Should().Be(Freighter.MaxHealth - 1);
        world.Bolts.Should().BeEmpty();
    }

    [Test]
    public void Bolt_FliesThroughFriendlyShip()
    {
        var friendly = WeaponTestWorld.Enemy(new Vector2(200f, 0f), Faction.Blue);
        var enemy = WeaponTestWorld.Enemy(new Vector2(400f, 0f));
        var world = WeaponTestWorld.CreateWorld(aiFighters: [friendly, enemy]);
        var system = WeaponTestWorld.CreateSystem();

        WeaponTestWorld.Run(system, world, 1f, fire: true);

        friendly.Health.Should().Be(Fighter.MaxHealth);
        enemy.Health.Should().Be(Fighter.MaxHealth - 1);
    }

    [Test]
    public void FriendlyFireOn_HurtsFriendlyShip()
    {
        var friendly = WeaponTestWorld.Enemy(new Vector2(200f, 0f), Faction.Blue);
        var world = WeaponTestWorld.CreateWorld(aiFighters: [friendly]);
        var system = WeaponTestWorld.CreateSystem(WeaponTestWorld.Settings(s => s with { FriendlyFire = true }));

        WeaponTestWorld.Run(system, world, 0.6f, fire: true);

        friendly.Health.Should().Be(Fighter.MaxHealth - 1);
    }

    [Test]
    public void Bolt_NeverHurtsItsShooter_EvenWithFriendlyFire()
    {
        var world = WeaponTestWorld.CreateWorld();
        var system = WeaponTestWorld.CreateSystem(WeaponTestWorld.Settings(s => s with { FriendlyFire = true }));
        world.Bolts.Add(new Bolt(world.Player, world.Player.Position, new Vector2(500f, 0f), 2f));

        system.Update(world, WeaponTestWorld.Idle, 0.02f);

        world.Player.Health.Should().Be(Fighter.MaxHealth);
        world.Bolts.Should().ContainSingle();
    }

    [Test]
    public void FastBolt_DoesNotTunnelThroughShip()
    {
        var enemy = WeaponTestWorld.Enemy(new Vector2(1000f, 0f));
        var world = WeaponTestWorld.CreateWorld(aiFighters: [enemy]);
        var system = WeaponTestWorld.CreateSystem(
            WeaponTestWorld.Settings(s => s with { BoltSpeed = 20000f, BoltLifetime = 1f }));
        system.Update(world, WeaponTestWorld.Fire, 0.1f);

        system.Update(world, WeaponTestWorld.Idle, 0.1f);

        enemy.Health.Should().Be(Fighter.MaxHealth - 1);
    }

    [Test]
    public void Bolt_ThatExpiresOnTheFrameItReachesATarget_StillHits()
    {
        var enemy = WeaponTestWorld.Enemy(new Vector2(60f, 0f));
        var world = WeaponTestWorld.CreateWorld(aiFighters: [enemy]);
        var system = WeaponTestWorld.CreateSystem(WeaponTestWorld.Settings(s => s with { BoltLifetime = 0.1f }));
        system.Update(world, WeaponTestWorld.Fire, 0.02f);

        system.Update(world, WeaponTestWorld.Idle, 0.1f);

        enemy.Health.Should().Be(Fighter.MaxHealth - 1);
    }

    [Test]
    public void Bolt_HitsNearerOfTwoEnemiesOnly_EvenWhenBothAreInOneFramesPath()
    {
        var near = WeaponTestWorld.Enemy(new Vector2(1000f, 0f));
        var far = WeaponTestWorld.Enemy(new Vector2(1100f, 0f));
        var world = WeaponTestWorld.CreateWorld(aiFighters: [far, near]);
        var system = WeaponTestWorld.CreateSystem(
            WeaponTestWorld.Settings(s => s with { BoltSpeed = 20000f, BoltLifetime = 1f }));
        system.Update(world, WeaponTestWorld.Fire, 0.1f);

        system.Update(world, WeaponTestWorld.Idle, 0.1f);

        near.Health.Should().Be(Fighter.MaxHealth - 1);
        far.Health.Should().Be(Fighter.MaxHealth);
        world.Impacts.Should().ContainSingle().Which.Position.X.Should().BeApproximately(990f, 1f);
    }

    [Test]
    public void Bolt_SpawningInsideAnEnemy_HitsItAtTheSpawnPoint()
    {
        var enemy = WeaponTestWorld.Enemy(new Vector2(20f, 0f));
        var world = WeaponTestWorld.CreateWorld(aiFighters: [enemy]);
        var system = WeaponTestWorld.CreateSystem();
        system.Update(world, WeaponTestWorld.Fire, 0.02f);

        system.Update(world, WeaponTestWorld.Idle, 0.02f);

        enemy.Health.Should().Be(Fighter.MaxHealth - 1);
        world.Impacts.Should().ContainSingle().Which.Position.X.Should().BeApproximately(13f, 0.01f);
    }

    [Test]
    public void Hit_ClampsHealthAtZero_AndKeepsShipInWorld()
    {
        var enemy = WeaponTestWorld.Enemy(new Vector2(200f, 0f));
        enemy.Health = 0;
        var world = WeaponTestWorld.CreateWorld(aiFighters: [enemy]);
        var system = WeaponTestWorld.CreateSystem();

        WeaponTestWorld.Run(system, world, 0.6f, fire: true);

        enemy.Health.Should().Be(0);
        world.AiFighters.Should().Contain(enemy);
        world.Bolts.Should().BeEmpty();
    }

    [Test]
    public void Bolt_HitsDockedEnemyShip()
    {
        var planet = new Planet(new Vector2(5000f, 5000f));
        var enemy = WeaponTestWorld.Enemy(new Vector2(200f, 0f));
        enemy.BeginDocking(planet, Vector2.UnitY);
        enemy.CompleteDocking();
        var world = WeaponTestWorld.CreateWorld([planet], aiFighters: [enemy]);
        var system = WeaponTestWorld.CreateSystem();

        WeaponTestWorld.Run(system, world, 0.6f, fire: true);

        enemy.Health.Should().Be(Fighter.MaxHealth - 1);
    }
}
