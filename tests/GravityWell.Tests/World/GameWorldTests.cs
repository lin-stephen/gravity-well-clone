using System.Numerics;
using AwesomeAssertions;
using GravityWell.Core.Entities;
using GravityWell.Core.World;

namespace GravityWell.Tests.World;

public class GameWorldTests
{
    [Test]
    public void DefaultWorld_HasOneRedAiFighterAndNoBlueAiFighter()
    {
        var world = new GameWorld();

        world.AiFighters.Should().ContainSingle().Which.Faction.Should().Be(Faction.Red);
        world.Player.Faction.Should().Be(Faction.Blue);
        world.AiFighters.Should().NotContain(world.Player);
    }

    [Test]
    public void DefaultWorld_AiFighterSpawnsClearOfEveryPlanetAndFreighter()
    {
        var world = new GameWorld();
        var fighter = world.AiFighters.Single();
        var homeworld = world.Planets.Single(planet => planet.Owner == fighter.Faction);

        foreach (var planet in world.Planets)
            Vector2.Distance(fighter.Position, planet.Position)
                .Should().BeGreaterThanOrEqualTo(Planet.Radius + Fighter.Radius);
        foreach (var freighter in world.Freighters)
            Vector2.Distance(fighter.Position, freighter.Position)
                .Should().BeGreaterThanOrEqualTo(Fighter.Radius + Freighter.Radius);
        Vector2.Distance(fighter.Position, homeworld.Position).Should().BeLessThan(200f);
    }

    [Test]
    public void Ships_YieldsPlayerThenAiFightersThenFreighters()
    {
        var world = new GameWorld();

        var ships = world.Ships.ToList();

        ships[0].Should().BeSameAs(world.Player);
        ships[1].Should().BeSameAs(world.AiFighters[0]);
        ships.Skip(2).Should().Equal(world.Freighters);
        world.Fighters.Should().Equal([world.Player, .. world.AiFighters]);
    }

    [Test]
    public void CreateAiFighters_OneFighterPerAiHomeworldFaction()
    {
        var purple = new Planet(new Vector2(0, 0), owner: Faction.Purple);
        purple.BuildHomeworld();
        var red = new Planet(new Vector2(1000, 0), owner: Faction.Red);
        red.BuildHomeworld();
        var blue = new Planet(new Vector2(2000, 0), owner: Faction.Blue);
        blue.BuildHomeworld();
        var outpost = new Planet(new Vector2(3000, 0), owner: Faction.Yellow);
        var secondRed = new Planet(new Vector2(4000, 0), owner: Faction.Red);
        secondRed.BuildHomeworld();

        var fighters = GameWorld.CreateAiFighters([purple, red, blue, outpost, secondRed], Faction.Blue);

        fighters.Select(fighter => fighter.Faction).Should().BeEquivalentTo([Faction.Purple, Faction.Red]);
        fighters.Single(fighter => fighter.Faction == Faction.Red).Position
            .Should().Be(red.Position + new Vector2(0f, 100f)); // The first homeworld wins.
    }

    [Test]
    public void DefaultWorld_AiFightersAreAllPeaceful()
    {
        new GameWorld().AiFighters.Should().OnlyContain(fighter => fighter.Behavior == FighterBehavior.Peaceful);
    }

    [Test]
    public void CreateAiFighters_AsksForTheBehaviorOfEachAiFactionAndAssignsIt()
    {
        var purple = new Planet(new Vector2(0, 0), owner: Faction.Purple);
        purple.BuildHomeworld();
        var red = new Planet(new Vector2(1000, 0), owner: Faction.Red);
        red.BuildHomeworld();
        var blue = new Planet(new Vector2(2000, 0), owner: Faction.Blue);
        blue.BuildHomeworld();
        var asked = new List<Faction>();
        // Only one FighterBehavior exists yet, so a faction-specific value outside the defined range is what
        // tells "assigned from the function" apart from "left at the default".
        static FighterBehavior Marker(Faction faction) => (FighterBehavior)(100 + (int)faction);

        var fighters = GameWorld.CreateAiFighters([purple, red, blue], Faction.Blue, faction =>
        {
            asked.Add(faction);
            return Marker(faction);
        });

        asked.Should().BeEquivalentTo([Faction.Purple, Faction.Red], "the player's faction gets no AI fighter");
        fighters.ToDictionary(fighter => fighter.Faction, fighter => fighter.Behavior).Should().BeEquivalentTo(
            new Dictionary<Faction, FighterBehavior>
            {
                [Faction.Purple] = Marker(Faction.Purple),
                [Faction.Red] = Marker(Faction.Red),
            });
    }
}
