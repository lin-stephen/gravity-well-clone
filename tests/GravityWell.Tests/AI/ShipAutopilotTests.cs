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
        public List<(Fighter Ship, FlightPlan Plan, float DeltaTime)> Decisions { get; } = [];
        public ShipControls Result { get; init; } = new(0.5f, 0.25f);
        public Vector2 InitialHold { get; init; } = new(7f, 9f);

        public Vector2 InitialHoldPosition(GameWorld world, Fighter ship) => InitialHold;

        public ShipControls Decide(GameWorld world, Fighter ship, FlightPlan plan, float deltaTime)
        {
            Decisions.Add((ship, plan, deltaTime));
            return Result;
        }
    }

    private static GameWorld NewWorld() => new([], []);

    private static ShipAutopilot<Fighter> Autopilot(List<Fighter> ships, IAutopilotStrategy<Fighter> strategy) =>
        new(_ => ships, _ => strategy);

    [Test]
    public void Update_WritesTheStrategysControlsToEveryShip()
    {
        var strategy = new FakeStrategy();
        var first = new Fighter();
        var second = new Fighter();

        Autopilot([first, second], strategy).Update(NewWorld(), 0.1f);

        first.Controls.Should().Be(strategy.Result);
        second.Controls.Should().Be(strategy.Result);
    }

    [Test]
    public void Update_PassesTheDeltaTimeToTheStrategy()
    {
        var strategy = new FakeStrategy();

        Autopilot([new Fighter()], strategy).Update(NewWorld(), 0.25f);

        strategy.Decisions.Should().ContainSingle().Which.DeltaTime.Should().Be(0.25f);
    }

    [Test]
    public void Update_KeepsOnePlanPerShipStartingAtTheStrategysHoldPosition()
    {
        var strategy = new FakeStrategy();
        var ship = new Fighter();
        var autopilot = Autopilot([ship], strategy);

        autopilot.Update(NewWorld(), 0.1f);
        autopilot.Update(NewWorld(), 0.1f);

        strategy.Decisions.Should().HaveCount(2);
        strategy.Decisions[1].Plan.Should().BeSameAs(strategy.Decisions[0].Plan);
        strategy.Decisions[0].Plan.HoldPosition.Should().Be(strategy.InitialHold);
        autopilot.PlanOf(ship).Should().BeSameAs(strategy.Decisions[0].Plan);
    }

    [Test]
    public void Update_ForgetsAShipOnceItIsNoLongerListed()
    {
        var strategy = new FakeStrategy();
        var ship = new Fighter();
        var ships = new List<Fighter> { ship };
        var autopilot = Autopilot(ships, strategy);
        autopilot.Update(NewWorld(), 0.1f);
        var firstPlan = autopilot.PlanOf(ship);

        ships.Clear();
        autopilot.Update(NewWorld(), 0.1f);
        autopilot.PlanOf(ship).Should().BeNull();

        ships.Add(ship);
        autopilot.Update(NewWorld(), 0.1f);
        autopilot.PlanOf(ship).Should().NotBeNull().And.NotBeSameAs(firstPlan);
    }

    [Test]
    public void Update_UsesTheStrategyChosenForEachShip()
    {
        var calm = new FakeStrategy { Result = new ShipControls(0f, 0.1f), InitialHold = new Vector2(1f, 1f) };
        var bold = new FakeStrategy { Result = new ShipControls(1f, 1f), InitialHold = new Vector2(2f, 2f) };
        var calmShip = new Fighter();
        var boldShip = new Fighter();
        var autopilot = new ShipAutopilot<Fighter>(_ => [calmShip, boldShip],
            ship => ReferenceEquals(ship, boldShip) ? bold : calm);

        autopilot.Update(NewWorld(), 0.1f);

        calmShip.Controls.Should().Be(calm.Result);
        boldShip.Controls.Should().Be(bold.Result);
        calm.Decisions.Should().ContainSingle().Which.Ship.Should().BeSameAs(calmShip);
        bold.Decisions.Should().ContainSingle().Which.Ship.Should().BeSameAs(boldShip);
        autopilot.PlanOf(calmShip)!.HoldPosition.Should().Be(calm.InitialHold);
        autopilot.PlanOf(boldShip)!.HoldPosition.Should().Be(bold.InitialHold);
    }

    [Test]
    public void Update_KeepsTheStrategyAShipWasFirstGiven()
    {
        // A ship's strategy is fixed when it is first seen, so a lookup that later answers differently
        // must not move a ship onto a strategy whose plan state it never started with.
        var first = new FakeStrategy();
        var later = new FakeStrategy();
        var lookups = 0;
        var ship = new Fighter();
        var autopilot = new ShipAutopilot<Fighter>(_ => [ship], _ => lookups++ == 0 ? first : later);

        autopilot.Update(NewWorld(), 0.1f);
        autopilot.Update(NewWorld(), 0.1f);
        autopilot.Update(NewWorld(), 0.1f);

        first.Decisions.Should().HaveCount(3);
        later.Decisions.Should().BeEmpty();
        lookups.Should().Be(1, "the strategy is looked up once, when the ship is first seen");
    }

    [Test]
    public void PlanOf_AShipThatWasNeverPiloted_IsNull()
    {
        Autopilot([], new FakeStrategy()).PlanOf(new Fighter()).Should().BeNull();
    }
}
