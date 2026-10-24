using System.ComponentModel.DataAnnotations;
using System.Numerics;
using GravityWell.Core.Entities;
using GravityWell.Core.Physics;
using GravityWell.Core.World;

namespace GravityWell.Tests.Physics;

public class CollisionSystemTests
{
    [TestCase(0f, 0)]
    [TestCase(9f, 0)]
    [TestCase(10f, 0)]
    [TestCase(11f, 1)]
    [TestCase(15f, 1)]
    [TestCase(16f, 2)]
    [TestCase(20f, 2)]
    public void CalculateDamage_AppliesThresholdAndRoundsUp(float closingSpeed, int expected)
    {
        var collisions = new CollisionSystem(new CollisionSettings
        {
            DamageSpeedThreshold = 10f,
            SpeedPerDamagePoint = 5f
        });

        Assert.That(collisions.CalculateDamage(closingSpeed), Is.EqualTo(expected));
    }

    [TestCase(0f, 5f, 5f)]
    [TestCase(1f, 0f, 10f)]
    public void ResolveContact_UsesConfiguredRestitution(float restitution, float firstSpeed, float secondSpeed)
    {
        var collisions = new CollisionSystem(new CollisionSettings { Restitution = restitution });
        var first = new Fighter { Position = Vector2.Zero, Velocity = new Vector2(10f, 0f) };
        var second = new Fighter { Position = new Vector2(20f, 0f), Velocity = Vector2.Zero };

        collisions.ResolveContact(first, second);

        Assert.Multiple(() =>
        {
            Assert.That(first.Velocity.X, Is.EqualTo(firstSpeed));
            Assert.That(second.Velocity.X, Is.EqualTo(secondSpeed));
        });
    }

    [Test]
    public void Constructor_ValidatesSettings()
    {
        Assert.Throws<ValidationException>(() => new CollisionSystem(
            new CollisionSettings { SpeedPerDamagePoint = 0f }));
    }

    [Test]
    public void Update_UsesConfiguredDamageBeforeBounce()
    {
        var collisions = new CollisionSystem(new CollisionSettings
        {
            DamageSpeedThreshold = 10f,
            SpeedPerDamagePoint = 5f,
            Restitution = 1f
        });
        var planet = new Planet(Vector2.Zero);
        var world = new GameWorld([planet], []);
        world.Player.Position = new Vector2(planet.CollisionRadius + world.Player.CollisionRadius, 0f);
        world.Player.Velocity = new Vector2(-16f, 0f);
        var initialShipHealth = world.Player.Health;
        var initialPlanetHealth = planet.Health;

        collisions.Update(world);

        Assert.Multiple(() =>
        {
            // Six speed units above the threshold rounds up to two damage points.
            Assert.That(world.Player.Health, Is.EqualTo(initialShipHealth - 2));
            Assert.That(planet.Health, Is.EqualTo(initialPlanetHealth - 2));
            Assert.That(world.Player.Velocity.X, Is.EqualTo(16f));
        });
    }

    [Test]
    public void Update_AiFighterHitsFreighter_BothTakeDamage()
    {
        var collisions = new CollisionSystem(new CollisionSettings
        {
            DamageSpeedThreshold = 10f,
            SpeedPerDamagePoint = 5f
        });
        var fighter = new Fighter { Faction = Faction.Red, Position = Vector2.Zero, Velocity = new Vector2(8f, 0f) };
        var freighter = new Freighter(new Vector2(20f, 0f), Faction.Red) { Velocity = new Vector2(-8f, 0f) };
        var world = new GameWorld([new Planet(new Vector2(5000f, 5000f))], [freighter], [fighter]);
        world.Player.Position = new Vector2(10000f, 10000f);

        collisions.Update(world);

        Assert.Multiple(() =>
        {
            // Closing at 16 is six above the threshold, which rounds up to two damage points.
            Assert.That(fighter.Health, Is.EqualTo(Fighter.MaxHealth - 2));
            Assert.That(freighter.Health, Is.EqualTo(Freighter.MaxHealth - 2));
        });
    }
}
