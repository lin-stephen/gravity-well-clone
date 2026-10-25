using System.Numerics;
using AwesomeAssertions;
using GravityWell.Core.Colonization;
using GravityWell.Core.Entities;
using GravityWell.Core.World;

namespace GravityWell.Tests.Colonization;

public class FreighterSystemTests
{
    private const float Dt = 0.1f;

    [Test]
    public void Update_PlanetTakenByAnotherFaction_RecallsTheFreighterAndTheNewOwnerBuilds()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        var blue = new Freighter(new Vector2(500f, 0f), Faction.Blue);
        var red = new Freighter(new Vector2(-500f, 0f), Faction.Red);
        var world = new GameWorld([planet], [blue, red]);
        blue.Dispatch(planet, StructureType.Base);
        planet.Claim(Faction.Red);

        new FreighterSystem().Update(world, Dt);

        blue.State.Should().Be(FreighterState.Idle);
        blue.TargetPlanet.Should().BeNull();
        red.State.Should().Be(FreighterState.Traveling);
        red.TargetPlanet.Should().BeSameAs(planet);
    }

    [Test]
    public void Update_SeveralIdleFreighters_DispatchesTheClosestToThePlanet()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        var far = new Freighter(new Vector2(900f, 0f), Faction.Blue);
        var near = new Freighter(new Vector2(0f, 300f), Faction.Blue);
        var world = new GameWorld([planet], [far, near]);

        new FreighterSystem().Update(world, Dt);

        near.TargetPlanet.Should().BeSameAs(planet);
        far.State.Should().Be(FreighterState.Idle);
    }

    [Test]
    public void Update_FreighterRecalledMidFlight_IsPickedOverAFartherIdleOne()
    {
        var lost = new Planet(Vector2.Zero, owner: Faction.Blue);
        var contested = new Planet(new Vector2(1000f, 0f), owner: Faction.Blue);
        var midway = new Freighter(new Vector2(800f, 0f), Faction.Blue);
        var idleAtHome = new Freighter(new Vector2(-900f, 0f), Faction.Blue);
        var world = new GameWorld([lost, contested], [idleAtHome, midway]);
        midway.Dispatch(lost, StructureType.Base);
        lost.Claim(Faction.Red);

        new FreighterSystem().Update(world, Dt);

        midway.TargetPlanet.Should().BeSameAs(contested);
        idleAtHome.State.Should().Be(FreighterState.Idle);
    }

    [Test]
    public void Update_RecallingADockedFreighter_ReleasesItsDock()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        var blue = new Freighter(new Vector2(0f, -46f), Faction.Blue);
        var world = new GameWorld([planet], [blue]);
        blue.Dispatch(planet, StructureType.Base);
        blue.BeginDocking(planet, -Vector2.UnitY);
        blue.CompleteDocking();
        planet.Claim(Faction.Red);

        new FreighterSystem().Update(world, Dt);

        blue.DockingTarget.Should().BeNull();
        blue.State.Should().Be(FreighterState.Idle);
        planet.Structures.Should().BeEmpty("a freighter must not build for the planet's new owner");
    }

    [Test]
    public void Update_FreighterTargetingAnUnownedPlanet_IsNotRecalled()
    {
        var planet = new Planet(Vector2.Zero);
        var blue = new Freighter(new Vector2(500f, 0f), Faction.Blue);
        var world = new GameWorld([planet], [blue]);
        blue.Dispatch(planet, StructureType.Base);

        new FreighterSystem().Update(world, Dt);

        blue.State.Should().Be(FreighterState.Traveling);
        blue.TargetPlanet.Should().BeSameAs(planet);
    }

    [Test]
    public void Update_FreighterTargetingItsOwnFactionsPlanet_IsNotRecalled()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        var blue = new Freighter(new Vector2(500f, 0f), Faction.Blue);
        var world = new GameWorld([planet], [blue]);
        blue.Dispatch(planet, StructureType.Base);

        new FreighterSystem().Update(world, Dt);

        blue.State.Should().Be(FreighterState.Traveling);
    }
}
