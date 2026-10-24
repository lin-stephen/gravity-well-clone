using System.ComponentModel.DataAnnotations;
using System.Numerics;
using GravityWell.Core.Entities;
using GravityWell.Core.Physics;
using GravityWell.Core.World;

namespace GravityWell.Tests.Physics;

public class DockingSystemTests
{
    [TestCase(9f, true)]
    [TestCase(10f, false)]
    [TestCase(11f, false)]
    public void IsSpeedSafe_RequiresSpeedBelowConfiguredThreshold(float speed, bool expected)
    {
        var docking = new DockingSystem(new DockingSettings { SpeedThreshold = 10f });
        var ship = new Fighter { Velocity = new Vector2(speed, 0f) };

        Assert.That(docking.IsSpeedSafe(ship, new Planet(Vector2.Zero)), Is.EqualTo(expected));
    }

    [TestCase(-9f, 0f, true)]
    [TestCase(-10f, 0f, false)]
    [TestCase(20f, 0f, true)]
    [TestCase(0f, 20f, true)]
    public void IsApproachSpeedSafe_UsesOnlyClosingSpeed(float xSpeed, float ySpeed, bool expected)
    {
        var docking = new DockingSystem(new DockingSettings { SpeedThreshold = 10f });
        var ship = new Fighter { Position = new Vector2(100f, 0f), Velocity = new Vector2(xSpeed, ySpeed) };

        Assert.That(docking.IsApproachSpeedSafe(ship, new Planet(Vector2.Zero)), Is.EqualTo(expected));
    }

    [TestCase(0f, true)]
    [TestCase(45f, true)]
    [TestCase(46f, false)]
    public void IsRearAligned_UsesConfiguredAngle(float rotation, bool expected)
    {
        var docking = new DockingSystem(new DockingSettings { AlignmentDegrees = 45f });
        var ship = new Fighter { Position = new Vector2(0f, -100f), Rotation = rotation };

        Assert.That(docking.IsRearAligned(ship, new Planet(Vector2.Zero)), Is.EqualTo(expected));
    }

    [TestCase(1f, true)]
    [TestCase(2f, true)]
    [TestCase(3f, false)]
    [TestCase(-2f, true)]
    [TestCase(-3f, false)]
    public void Update_CapturesWithinConfiguredSurfaceDistance(float surfaceDistance, bool expected)
    {
        var docking = new DockingSystem(new DockingSettings { CaptureDistance = 2f });
        var planet = new Planet(Vector2.Zero);
        var world = new GameWorld([planet], []);
        world.Player.Position = new Vector2(0f, -(planet.CollisionRadius + world.Player.CollisionRadius + surfaceDistance));
        world.Player.Rotation = 0f;
        world.Player.Velocity = Vector2.Zero;

        docking.Update(world, 0f);

        Assert.That(world.Player.DockingTarget, expected ? Is.SameAs(planet) : Is.Null);
    }

    [Test]
    public void Update_UsesConfiguredDampingRate()
    {
        // At rate ln(2), one second halves the remaining distance.
        var docking = new DockingSystem(new DockingSettings { CaptureDistance = 10f, DampingRate = MathF.Log(2f) });
        var planet = new Planet(Vector2.Zero);
        var world = new GameWorld([planet], []);
        var surfaceRadius = planet.CollisionRadius + world.Player.CollisionRadius;
        world.Player.Position = new Vector2(0f, -(surfaceRadius + 8f));
        world.Player.Rotation = 0f;
        world.Player.Velocity = Vector2.Zero;

        docking.Update(world, 1f);

        Assert.That(world.Player.Position.Y, Is.EqualTo(-(surfaceRadius + 4f)).Within(0.0001f));
    }

    [Test]
    public void Constructor_ValidatesSettings()
    {
        Assert.Throws<ValidationException>(() => new DockingSystem(new DockingSettings { SpeedThreshold = 0f }));
    }
}
