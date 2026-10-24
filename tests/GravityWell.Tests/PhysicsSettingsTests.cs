using System.ComponentModel.DataAnnotations;
using System.Numerics;
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using GravityWell.Core.Entities;
using GravityWell.Core.Physics;
using GravityWell.Core.World;

namespace GravityWell.Tests;

public class PhysicsSettingsTests
{
    [Test]
    public void Validate_AcceptsDefaultsAndZeroGravity()
    {
        var validateDefaults = new PhysicsSettings().Validate;
        var validateZeroGravity = new PhysicsSettings { PlanetGravityStrength = 0f }.Validate;

        validateDefaults.Should().NotThrow();
        validateZeroGravity.Should().NotThrow();
    }

    [Test]
    public void Constructor_ValidatesSettings()
    {
        Action construct = () => new PhysicsSystem(
            new PhysicsSettings { MinimumGravityDistanceSquared = 0f });

        construct.Should().Throw<ValidationException>();
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

        physics.GetGravity(world, new Vector2(distance, 0f)).Should().Be(new Vector2(expected, 0f));
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

        using var scope = new AssertionScope();
        world.Player.Velocity.Should().Be(new Vector2(-5f, 0f));
        world.Player.Position.Should().Be(new Vector2(7.5f, 0f));
    }
}
