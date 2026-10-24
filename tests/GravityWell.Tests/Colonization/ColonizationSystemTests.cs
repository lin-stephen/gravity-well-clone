using System.Numerics;
using AwesomeAssertions;
using GravityWell.Core.Colonization;
using GravityWell.Core.Entities;
using GravityWell.Core.World;

namespace GravityWell.Tests.Colonization;

public class ColonizationSystemTests
{
    private static void Dock(Ship ship, Planet planet)
    {
        ship.BeginDocking(planet, Vector2.UnitY);
        ship.CompleteDocking();
    }

    [Test]
    public void Update_AiFighterDockedAtFreePlanet_ClaimsForItsFaction()
    {
        var planet = new Planet(Vector2.Zero);
        var fighter = new Fighter { Faction = Faction.Red };
        var world = new GameWorld([planet], [], [fighter]);
        Dock(fighter, planet);

        new ColonizationSystem().Update(world);

        planet.Owner.Should().Be(Faction.Red);
    }

    [Test]
    public void Update_FighterDockedAtAnotherFactionsPlanetWithoutABase_TakesIt()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        var fighter = new Fighter { Faction = Faction.Red };
        var world = new GameWorld([planet], [], [fighter]);
        Dock(fighter, planet);

        new ColonizationSystem().Update(world);

        planet.Owner.Should().Be(Faction.Red);
    }

    [Test]
    public void Update_FighterDockedAtAnotherFactionsPlanetWithABase_KeepsOwner()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        planet.AddStructure(StructureType.Base, StructureState.Building);
        var fighter = new Fighter { Faction = Faction.Red };
        var world = new GameWorld([planet], [], [fighter]);
        Dock(fighter, planet);

        new ColonizationSystem().Update(world);

        planet.Owner.Should().Be(Faction.Blue);
    }

    [Test]
    public void Update_PlayerDockedAtAnEnemyPlanetWithoutABase_TakesIt()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Red);
        var world = new GameWorld([planet], []);
        Dock(world.Player, planet);

        new ColonizationSystem().Update(world);

        planet.Owner.Should().Be(world.Player.Faction);
    }

    [Test]
    public void Update_PlayerDockedAtFreePlanet_StillClaims()
    {
        var planet = new Planet(Vector2.Zero);
        var world = new GameWorld([planet], []);
        Dock(world.Player, planet);

        new ColonizationSystem().Update(world);

        planet.Owner.Should().Be(world.Player.Faction);
    }

    [Test]
    public void Update_ClaimedByRed_DispatchesIdleRedFreighterOnly()
    {
        var planet = new Planet(Vector2.Zero);
        var fighter = new Fighter { Faction = Faction.Red };
        var red = new Freighter(new Vector2(500f, 0f), Faction.Red);
        var blue = new Freighter(new Vector2(-500f, 0f), Faction.Blue);
        var world = new GameWorld([planet], [blue, red], [fighter]);
        Dock(fighter, planet);

        new ColonizationSystem().Update(world);
        new FreighterSystem().Update(world, 0.1f);

        red.State.Should().Be(FreighterState.Traveling);
        red.TargetPlanet.Should().BeSameAs(planet);
        blue.State.Should().Be(FreighterState.Idle);
    }

    [Test]
    public void Update_RedTakesAnUnbasedBluePlanet_BlueFreighterIsRecalledAndRedIsDispatched()
    {
        // Integration check across ColonizationSystem and FreighterSystem; each half is tested on its own.
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        var fighter = new Fighter { Faction = Faction.Red };
        var blue = new Freighter(new Vector2(500f, 0f), Faction.Blue);
        var red = new Freighter(new Vector2(-500f, 0f), Faction.Red);
        var world = new GameWorld([planet], [blue, red], [fighter]);
        blue.Dispatch(planet, StructureType.Base);
        Dock(fighter, planet);

        new ColonizationSystem().Update(world);
        new FreighterSystem().Update(world, 0.1f);

        planet.Owner.Should().Be(Faction.Red);
        blue.State.Should().Be(FreighterState.Idle);
        red.State.Should().Be(FreighterState.Traveling);
        red.TargetPlanet.Should().BeSameAs(planet);
    }
}
