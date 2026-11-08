using System.Numerics;
using AwesomeAssertions;
using GravityWell.Core.Colonization;
using GravityWell.Core.Entities;
using GravityWell.Core.World;

namespace GravityWell.Tests.Colonization;

public class FreighterProductionTests
{
    private const float BuildSeconds = 5f;

    private static Planet ProducerPlanet(Faction owner, StructureType producerType = StructureType.TechCenter)
    {
        var planet = new Planet(Vector2.Zero, owner: owner);
        planet.AddStructure(StructureType.Base, StructureState.Operational);
        if (producerType == StructureType.SpaceDock)
        {
            planet.AddStructure(StructureType.Starbase, StructureState.Operational);
        }

        planet.AddStructure(producerType, StructureState.Operational);
        return planet;
    }

    private static int CountOf(GameWorld world, Faction faction) =>
        world.Freighters.Count(freighter => freighter.Faction == faction);

    [Test]
    public void Update_FactionWithTechCenterAndRoomInItsFleet_BuildsAFreighterAfterTheBuildTime()
    {
        var world = new GameWorld([ProducerPlanet(Faction.Blue)], []);
        var production = new FreighterProduction();

        production.Update(world, BuildSeconds - 0.5f);
        CountOf(world, Faction.Blue).Should().Be(0);

        production.Update(world, 0.5f);
        CountOf(world, Faction.Blue).Should().Be(1);
    }

    [Test]
    public void Update_FactionWithSpaceDock_AlsoBuildsAFreighter()
    {
        var world = new GameWorld([ProducerPlanet(Faction.Red, StructureType.SpaceDock)], []);

        new FreighterProduction().Update(world, BuildSeconds);

        CountOf(world, Faction.Red).Should().Be(1);
    }

    [Test]
    public void Update_FactionAtFleetCapacity_BuildsNothing()
    {
        var planet = ProducerPlanet(Faction.Blue);
        var fleet = Enumerable.Range(0, 3).Select(_ => new Freighter(new Vector2(500f, 0f), Faction.Blue)).ToList();
        var world = new GameWorld([planet], fleet);

        new FreighterProduction().Update(world, BuildSeconds * 2);

        CountOf(world, Faction.Blue).Should().Be(3);
    }

    [Test]
    public void Update_ConsumedFreightersDoNotCountTowardsCapacity()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Red);
        var builder = new Freighter(new Vector2(0f, -46f), Faction.Red);
        var spare1 = new Freighter(new Vector2(500f, 0f), Faction.Red);
        var spare2 = new Freighter(new Vector2(-500f, 0f), Faction.Red);
        var world = new GameWorld([planet], [builder, spare1, spare2]);
        var freighterSystem = new FreighterSystem();
        freighterSystem.Update(world, 0.1f);
        builder.BeginDocking(planet, -Vector2.UnitY);
        builder.CompleteDocking();
        for (var step = 0; step < 100 && builder.State != FreighterState.Consumed; step++)
        {
            freighterSystem.Update(world, 0.1f);
        }

        builder.State.Should().Be(FreighterState.Consumed);
        planet.AddStructure(StructureType.TechCenter, StructureState.Operational);
        var fleetBefore = world.Freighters.Count;

        new FreighterProduction().Update(world, BuildSeconds);

        world.Freighters.Count.Should().Be(fleetBefore + 1);
    }

    [Test]
    public void Update_FactionWithoutAProducer_BuildsNothing()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        planet.AddStructure(StructureType.Base, StructureState.Operational);
        var world = new GameWorld([planet], []);

        new FreighterProduction().Update(world, BuildSeconds * 2);

        world.Freighters.Should().BeEmpty();
    }

    [Test]
    public void Update_FactionWithNoPlanetsInTheWorld_BuildsNothing()
    {
        var world = new GameWorld([ProducerPlanet(Faction.Blue)], []);

        new FreighterProduction().Update(world, BuildSeconds * 2);

        CountOf(world, Faction.Purple).Should().Be(0);
        CountOf(world, Faction.Red).Should().Be(0);
        CountOf(world, Faction.Yellow).Should().Be(0);
    }

    [Test]
    public void Update_EachProducingFactionBuildsOnItsOwnTimer()
    {
        var redPlanet = new Planet(new Vector2(2000f, 0f), owner: Faction.Red);
        redPlanet.AddStructure(StructureType.Base, StructureState.Operational);
        var world = new GameWorld([ProducerPlanet(Faction.Blue), redPlanet], []);
        var production = new FreighterProduction();

        production.Update(world, BuildSeconds - 1f);
        redPlanet.AddStructure(StructureType.TechCenter, StructureState.Operational);
        production.Update(world, 1f);

        CountOf(world, Faction.Blue).Should().Be(1);
        CountOf(world, Faction.Red).Should().Be(0);

        production.Update(world, BuildSeconds - 1f);

        CountOf(world, Faction.Red).Should().Be(1);
    }

    [Test]
    public void Update_SeveralFactionsFinishOnTheSameTick_AddsFreightersInFactionOrder()
    {
        var yellow = new Planet(new Vector2(4000f, 0f), owner: Faction.Yellow);
        yellow.AddStructure(StructureType.Base, StructureState.Operational);
        yellow.AddStructure(StructureType.TechCenter, StructureState.Operational);
        var red = new Planet(new Vector2(2000f, 0f), owner: Faction.Red);
        red.AddStructure(StructureType.Base, StructureState.Operational);
        red.AddStructure(StructureType.TechCenter, StructureState.Operational);
        var world = new GameWorld([yellow, red, ProducerPlanet(Faction.Blue)], []);

        new FreighterProduction().Update(world, BuildSeconds);

        world.Freighters.Select(freighter => freighter.Faction)
            .Should().Equal(Faction.Blue, Faction.Red, Faction.Yellow);
    }

    [Test]
    public void Update_ProducerStillUnderConstruction_BuildsNothing()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        planet.AddStructure(StructureType.Base, StructureState.Operational);
        planet.AddStructure(StructureType.TechCenter, StructureState.Building);
        var world = new GameWorld([planet], []);

        new FreighterProduction().Update(world, BuildSeconds * 2);

        world.Freighters.Should().BeEmpty();
    }
}
