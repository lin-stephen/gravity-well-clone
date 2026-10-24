using System.Numerics;
using AwesomeAssertions;
using GravityWell.Core.AI;
using GravityWell.Core.Colonization;
using GravityWell.Core.Entities;
using GravityWell.Core.Physics;
using GravityWell.Core.World;

namespace GravityWell.Tests.AI;

public class FreighterRecallTests
{
    private const float Dt = 1f / 60f;

    [Test]
    public void RecalledFreighter_HoldsClearOfThePlanetItWasDockedAt()
    {
        // The freighter's first-seen position is behind the planet, so flying straight back to it
        // would pin the freighter against the planet it has just been recalled from.
        var planet = new Planet(new Vector2(300f, 0f), owner: Faction.Blue);
        var freighter = new Freighter(new Vector2(-520f, 0f), Faction.Blue) { Rotation = 90f };
        var world = new GameWorld([planet], [freighter]);
        world.Player.Position = new Vector2(10000f, 10000f);
        var autopilot = new FreighterAutopilot();
        var physics = new PhysicsSystem();
        var docking = new DockingSystem();
        var collisions = new CollisionSystem();
        var freighters = new FreighterSystem();
        autopilot.Update(world, Dt); // The plan is created here, holding at the spawn point.

        freighter.Dispatch(planet, StructureType.Base);
        freighter.Position = new Vector2(planet.Position.X + Planet.Radius + Freighter.Radius, 0f);
        freighter.Velocity = Vector2.Zero;
        freighter.BeginDocking(planet, Vector2.UnitX);
        freighter.CompleteDocking();
        planet.Claim(Faction.Red);

        for (var step = 0; step < (int)(60f / Dt); step++)
        {
            autopilot.Update(world, Dt);
            physics.Update(world, default, Dt);
            docking.Update(world, Dt);
            collisions.Update(world);
            freighters.Update(world, Dt);
        }

        freighter.State.Should().Be(FreighterState.Idle);
        var surfaceDistance = Vector2.Distance(freighter.Position, planet.Position) - Planet.Radius - Freighter.Radius;
        surfaceDistance.Should().BeGreaterThanOrEqualTo(ApproachTuning.ForFreighter.ApproachClearance - 10f);
        freighter.Velocity.Length().Should().BeLessThan(1f);
    }

    [Test]
    public void RecalledFreighter_CanBeSentBackToTheSamePlanet()
    {
        // The flight plan must forget its old target, or a second order to the same planet would reuse a
        // stale approach.
        var planet = new Planet(new Vector2(300f, 0f), owner: Faction.Blue);
        var freighter = new Freighter(new Vector2(-200f, 0f), Faction.Blue) { Rotation = 90f };
        var world = new GameWorld([planet], [freighter]);
        world.Player.Position = new Vector2(10000f, 10000f);
        var autopilot = new FreighterAutopilot();
        var physics = new PhysicsSystem();
        var docking = new DockingSystem();
        var collisions = new CollisionSystem();
        var freighters = new FreighterSystem();
        freighter.Dispatch(planet, StructureType.Base);
        for (var step = 0; step < (int)(3f / Dt); step++)
        {
            autopilot.Update(world, Dt);
            physics.Update(world, default, Dt);
            docking.Update(world, Dt);
            collisions.Update(world);
        }
        freighter.Recall();
        autopilot.Update(world, Dt);

        freighter.Dispatch(planet, StructureType.Base);
        for (var step = 0; step < (int)(120f / Dt); step++)
        {
            autopilot.Update(world, Dt);
            physics.Update(world, default, Dt);
            docking.Update(world, Dt);
            collisions.Update(world);
            freighters.Update(world, Dt);
        }

        planet.HasOperationalStructure(StructureType.Base).Should().BeTrue();
    }
}
