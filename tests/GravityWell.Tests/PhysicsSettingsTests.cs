using System.ComponentModel.DataAnnotations;
using System.Numerics;
using GravityWell.Entities;
using GravityWell.Input;
using GravityWell.Physics;
using GravityWell.World;

namespace GravityWell.Tests;

public class PhysicsSettingsTests
{
    [Test]
    public void Validate_AcceptsDefaultsAndZeroGravity()
    {
        Assert.DoesNotThrow(new PhysicsSettings().Validate);
        Assert.DoesNotThrow(new PhysicsSettings { PlanetGravityStrength = 0f }.Validate);
    }

    [Test]
    public void Constructor_ValidatesSettings()
    {
        Assert.Throws<ValidationException>(() => new PhysicsSystem(
            new PhysicsSettings { MinimumGravityDistanceSquared = 0f }));
    }

    [TestCase(0f, 0f)]
    [TestCase(10f, -10f)]
    [TestCase(20f, -2.5f)]
    public void GetGravity_UsesConfiguredStrengthAndMinimumDistance(float distance, float expected)
    {
        var world = new GameWorld([new Planet(Vector2.Zero)], []);
        var physics = new PhysicsSystem(new PhysicsSettings
        {
            PlanetGravityStrength = 1_000f,
            MinimumGravityDistanceSquared = 100f
        });

        Assert.That(physics.GetGravity(world, new Vector2(distance, 0f)),
            Is.EqualTo(new Vector2(expected, 0f)));
    }

    [Test]
    public void Update_UsesConfiguredGravity()
    {
        var world = new GameWorld([new Planet(Vector2.Zero)], [])
        {
            Player =
            {
                Position = new Vector2(10f, 0f),
                Velocity = Vector2.Zero
            }
        };
        var physics = new PhysicsSystem(new PhysicsSettings
        {
            PlanetGravityStrength = 1_000f,
            MinimumGravityDistanceSquared = 100f
        });

        physics.Update(world, default, 0.5f);

        Assert.Multiple(() =>
        {
            Assert.That(world.Player.Velocity, Is.EqualTo(new Vector2(-5f, 0f)));
            Assert.That(world.Player.Position, Is.EqualTo(new Vector2(7.5f, 0f)));
        });
    }
}
