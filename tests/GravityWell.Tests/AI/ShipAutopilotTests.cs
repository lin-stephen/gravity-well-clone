using System.Numerics;
using AwesomeAssertions;
using GravityWell.Core.AI;
using GravityWell.Core.Entities;
using GravityWell.Core.Input;
using GravityWell.Core.World;

namespace GravityWell.Tests.AI;

public class ShipAutopilotTests
{
    private sealed class FakeStrategy : IAutopilotStrategy<Fighter>
    {
        public List<Fighter> Ships { get; } = [];
        public List<(Fighter Ship, FlightPlan Plan, float DeltaTime)> Decisions { get; } = [];
        public ShipControls Result { get; set; } = new(0.5f, 0.25f);
        public Vector2 InitialHold { get; set; } = new(7f, 9f);

        public IEnumerable<Fighter> ShipsToPilot(GameWorld world) => Ships;
        public Vector2 InitialHoldPosition(GameWorld world, Fighter ship) => InitialHold;

        public ShipControls Decide(GameWorld world, Fighter ship, FlightPlan plan, float deltaTime)
        {
            Decisions.Add((ship, plan, deltaTime));
            return Result;
        }
    }

    private static GameWorld NewWorld() => new([], []);

    [Test]
    public void Update_WritesTheStrategysControlsToEveryShip()
    {
        var strategy = new FakeStrategy();
        var first = new Fighter();
        var second = new Fighter();
        strategy.Ships.AddRange([first, second]);

        new ShipAutopilot<Fighter>(strategy).Update(NewWorld(), 0.1f);

        first.Controls.Should().Be(strategy.Result);
        second.Controls.Should().Be(strategy.Result);
    }

    [Test]
    public void Update_PassesTheDeltaTimeToTheStrategy()
    {
        var strategy = new FakeStrategy();
        strategy.Ships.Add(new Fighter());

        new ShipAutopilot<Fighter>(strategy).Update(NewWorld(), 0.25f);

        strategy.Decisions.Should().ContainSingle().Which.DeltaTime.Should().Be(0.25f);
    }

    [Test]
    public void Update_KeepsOnePlanPerShipStartingAtTheStrategysHoldPosition()
    {
        var strategy = new FakeStrategy();
        var ship = new Fighter();
        strategy.Ships.Add(ship);
        var autopilot = new ShipAutopilot<Fighter>(strategy);

        autopilot.Update(NewWorld(), 0.1f);
        autopilot.Update(NewWorld(), 0.1f);

        strategy.Decisions.Should().HaveCount(2);
        strategy.Decisions[1].Plan.Should().BeSameAs(strategy.Decisions[0].Plan);
        strategy.Decisions[0].Plan.HoldPosition.Should().Be(strategy.InitialHold);
        autopilot.PlanOf(ship).Should().BeSameAs(strategy.Decisions[0].Plan);
    }

    [Test]
    public void Update_ForgetsAShipOnceTheStrategyStopsYieldingIt()
    {
        var strategy = new FakeStrategy();
        var ship = new Fighter();
        strategy.Ships.Add(ship);
        var autopilot = new ShipAutopilot<Fighter>(strategy);
        autopilot.Update(NewWorld(), 0.1f);
        var firstPlan = autopilot.PlanOf(ship);

        strategy.Ships.Clear();
        autopilot.Update(NewWorld(), 0.1f);
        autopilot.PlanOf(ship).Should().BeNull();

        strategy.Ships.Add(ship);
        autopilot.Update(NewWorld(), 0.1f);
        autopilot.PlanOf(ship).Should().NotBeNull().And.NotBeSameAs(firstPlan);
    }

    [Test]
    public void PlanOf_AShipThatWasNeverPiloted_IsNull()
    {
        new ShipAutopilot<Fighter>(new FakeStrategy()).PlanOf(new Fighter()).Should().BeNull();
    }
}
