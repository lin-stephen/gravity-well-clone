using System.Numerics;
using AwesomeAssertions;
using GravityWell.Core.AI;
using GravityWell.Core.AI.Fighters;
using GravityWell.Core.Colonization;
using GravityWell.Core.Entities;
using GravityWell.Core.Physics;
using GravityWell.Core.World;

namespace GravityWell.Tests.AI.Fighters;

public class FighterHoldTests
{
    private const float Dt = 1f / 60f;

    [Test]
    public void Update_AfterClaimingTheLastPlanet_HoldsClearOfTheSurface()
    {
        var planet = new Planet(Vector2.Zero);
        var fighter = new Fighter
        {
            Faction = Faction.Red, Position = new Vector2(900f, 0f), Velocity = Vector2.Zero, Rotation = 90f,
        };
        var world = new GameWorld([planet], [], [fighter]);
        world.Player.Position = new Vector2(10000f, 10000f);
        var autopilot = new FighterAutopilot();
        var physics = new PhysicsSystem();
        var docking = new DockingSystem();
        var collisions = new CollisionSystem();
        var colonization = new ColonizationSystem();

        for (var step = 0; step < (int)(120f / Dt); step++)
        {
            autopilot.Update(world, Dt);
            physics.Update(world, default, Dt);
            docking.Update(world, Dt);
            collisions.Update(world);
            colonization.Update(world);
        }

        planet.Owner.Should().Be(Faction.Red);
        fighter.DockingTarget.Should().BeNull();
        var surfaceDistance = Vector2.Distance(fighter.Position, planet.Position) - Planet.Radius - Fighter.Radius;
        surfaceDistance.Should().BeGreaterThanOrEqualTo(ApproachTuning.ForFighter.ApproachClearance - 10f);
        fighter.Velocity.Length().Should().BeLessThan(1f);
    }

    [Test]
    public void Update_HoldingFromTheFirstTickNextToAPlanet_StillStandsOff()
    {
        // A new flight plan already reads as Holding, so the very first hold must clear planets too.
        var owned = new Planet(Vector2.Zero, owner: Faction.Blue);
        var fighter = new Fighter
        {
            Faction = Faction.Red, Position = new Vector2(0f, 100f), Velocity = Vector2.Zero, Rotation = 0f,
        };
        var world = new GameWorld([owned], [], [fighter]);
        world.Player.Position = new Vector2(10000f, 10000f);
        var autopilot = new FighterAutopilot();
        var physics = new PhysicsSystem();
        var docking = new DockingSystem();
        var collisions = new CollisionSystem();

        for (var step = 0; step < (int)(60f / Dt); step++)
        {
            autopilot.Update(world, Dt);
            physics.Update(world, default, Dt);
            docking.Update(world, Dt);
            collisions.Update(world);
        }

        var surfaceDistance = Vector2.Distance(fighter.Position, owned.Position) - Planet.Radius - Fighter.Radius;
        surfaceDistance.Should().BeGreaterThanOrEqualTo(ApproachTuning.ForFighter.ApproachClearance - 10f);
        fighter.Velocity.Length().Should().BeLessThan(1f);
    }

    [Test]
    public void Update_HoldStandoffFollowsTheFightersOwnTuning()
    {
        var tuning = ApproachTuning.ForFighter with { ApproachClearance = 200f };
        var owned = new Planet(Vector2.Zero, owner: Faction.Blue);
        var fighter = new Fighter
        {
            Faction = Faction.Red, Position = new Vector2(0f, 100f), Velocity = Vector2.Zero, Rotation = 0f,
        };
        var world = new GameWorld([owned], [], [fighter]);
        world.Player.Position = new Vector2(10000f, 10000f);
        var autopilot = new FighterAutopilot(tuning: tuning);
        var physics = new PhysicsSystem();
        var docking = new DockingSystem();
        var collisions = new CollisionSystem();

        for (var step = 0; step < (int)(90f / Dt); step++)
        {
            autopilot.Update(world, Dt);
            physics.Update(world, default, Dt);
            docking.Update(world, Dt);
            collisions.Update(world);
        }

        var surfaceDistance = Vector2.Distance(fighter.Position, owned.Position) - Planet.Radius - Fighter.Radius;
        surfaceDistance.Should().BeGreaterThanOrEqualTo(tuning.ApproachClearance - 10f);
        fighter.Velocity.Length().Should().BeLessThan(1f);
    }
}
