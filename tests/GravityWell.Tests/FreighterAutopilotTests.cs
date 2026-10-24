using System.Numerics;
using GravityWell.Core.AI;
using GravityWell.Core.Entities;
using GravityWell.Core.Physics;
using GravityWell.Core.World;
using AwesomeAssertions;

namespace GravityWell.Tests;

/// <summary>
/// Characterization tests: each scenario flies a freighter through the real game loop and
/// pins the observable result, so refactoring the autopilot internals can't silently change behavior.
/// </summary>
public class FreighterAutopilotTests
{
    private const float Dt = 1f / 60f;

    private sealed record Flight(string Phases, int DockStep, Vector2 FinalPosition);

    private static Flight Simulate(Vector2 start, Vector2 velocity, Planet[]? obstacles = null,
        bool bump = false, bool withDock = false)
    {
        var target = new Planet(Vector2.Zero, owner: Faction.Blue);
        if (withDock) target.BuildHomeworld();
        var ship = new Freighter(start, Faction.Blue) { Velocity = velocity, Rotation = 90 };
        ship.Dispatch(target, StructureType.Base);
        var world = new GameWorld((obstacles ?? []).Append(target), [ship]);
        world.Player.Position = new Vector2(10000, 10000);

        var physics = new PhysicsSystem();
        var docking = new DockingSystem();
        var collisions = new CollisionSystem();
        var autopilot = new FreighterAutopilot();

        var phases = new List<FlightPhase>();
        for (var step = 0; step < 180 * 60; step++)
        {
            if (bump && step == 4 * 60)
                ship.ApplyCollision(Vector2.Zero, new Vector2(-60, 65));
            autopilot.Update(world, Dt);
            var phase = autopilot.GetPhase(ship);
            if (phases.Count == 0 || phases[^1] != phase) phases.Add(phase);
            physics.Update(world, default, Dt);
            docking.Update(world, Dt);
            collisions.Update(world);
            if (ship.IsDocked)
                return new Flight(string.Join(">", phases), step, ship.Position);
        }
        return new Flight(string.Join(">", phases), -1, ship.Position);
    }

    private static IEnumerable<TestCaseData> Scenarios()
    {
        yield return new TestCaseData(new Vector2(-500, 0), Vector2.Zero, null, false, false,
            "Cruise>Brake>Align>Approach>Dock", 838, new Vector2(-46f, -0.1f)).SetName("Clear landing");
        yield return new TestCaseData(new Vector2(-700, 0), Vector2.Zero, new[] { new Planet(new Vector2(-350, 0)) }, false, false,
            "Cruise>Brake>Cruise>Brake>Cruise>Brake>Align>Approach>Dock", 1338, new Vector2(-46f, 0.1f)).SetName("Blocked route replans around planet");
        yield return new TestCaseData(new Vector2(-500, 0), new Vector2(0, 70), null, false, false,
            "Cruise>Brake>Align>Approach>Dock", 863, new Vector2(-46f, 0.3f)).SetName("Sideways drift");
        yield return new TestCaseData(new Vector2(-500, 0), Vector2.Zero, null, true, false,
            "Cruise>Brake>Cruise>Brake>Align>Approach>Dock", 1008, new Vector2(-46f, 1f)).SetName("Bounce returns to corridor");
        yield return new TestCaseData(new Vector2(0, -500), Vector2.Zero, null, false, true,
            "Cruise>Brake>Align>Approach>Dock", 1057, new Vector2(46f, -0.6f)).SetName("Orbital dock avoidance lands on far side");
    }

    [TestCaseSource(nameof(Scenarios))]
    public void Freighter_FlightMatchesRecordedBehavior(Vector2 start, Vector2 velocity, Planet[]? obstacles,
        bool bump, bool withDock, string phases, int dockStep, Vector2 finalPosition)
    {
        var flight = Simulate(start, velocity, obstacles, bump, withDock);

        flight.Phases.Should().Be(phases);
        flight.DockStep.Should().BeCloseTo(dockStep, 3);
        flight.FinalPosition.X.Should().BeApproximately(finalPosition.X, 0.5f);
        flight.FinalPosition.Y.Should().BeApproximately(finalPosition.Y, 0.5f);
    }
}
