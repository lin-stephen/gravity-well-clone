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
    public void Update_CloserFreighterOfAnotherFaction_IsNeverDispatched()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        var red = new Freighter(new Vector2(0f, 100f), Faction.Red);
        var blue = new Freighter(new Vector2(900f, 0f), Faction.Blue);
        var world = new GameWorld([planet], [red, blue]);

        new FreighterSystem().Update(world, Dt);

        blue.TargetPlanet.Should().BeSameAs(planet);
        red.State.Should().Be(FreighterState.Idle);
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

    [Test]
    public void Update_FreighterRebuildsADestroyedBase_ReplacesItWithoutThrowing()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Red);
        planet.AddStructure(StructureType.Base, StructureState.Operational).TakeDamage(PlanetStructure.MaxHealth);
        var freighter = new Freighter(new Vector2(0f, -46f), Faction.Red);
        var world = new GameWorld([planet], [freighter]);
        var system = new FreighterSystem();
        system.Update(world, Dt);
        freighter.State.Should().Be(FreighterState.Traveling);
        freighter.BeginDocking(planet, -Vector2.UnitY);
        freighter.CompleteDocking();

        Assert.DoesNotThrow(() => system.Update(world, Dt));

        var bases = planet.Structures.Where(structure => structure.Type == StructureType.Base).ToList();
        bases.Should().ContainSingle("the destroyed Base is replaced, not kept beside the new one");
        bases[0].State.Should().Be(StructureState.Building);
        bases[0].Health.Should().Be(PlanetStructure.MaxHealth);
    }

    [Test]
    public void Update_ArrivedFreighter_BuildsAndHoldsTheStructureItCreated()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Red);
        var freighter = new Freighter(new Vector2(0f, -46f), Faction.Red);
        var world = new GameWorld([planet], [freighter]);
        var system = new FreighterSystem();
        system.Update(world, Dt);
        freighter.BeginDocking(planet, -Vector2.UnitY);
        freighter.CompleteDocking();

        system.Update(world, Dt);

        freighter.BuildSite.Should().BeSameAs(planet.Structures.Single());
        freighter.BuildSite!.ConstructionProgress.Should().BeGreaterThan(0f);
    }

    [Test]
    public void Update_FinishedBuild_ConsumesTheFreighterAndReleasesTheSite()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Red);
        var freighter = new Freighter(new Vector2(0f, -46f), Faction.Red);
        var world = new GameWorld([planet], [freighter]);
        var system = new FreighterSystem();
        system.Update(world, Dt);
        freighter.BeginDocking(planet, -Vector2.UnitY);
        freighter.CompleteDocking();

        for (var step = 0; step < 100 && freighter.State != FreighterState.Consumed; step++)
        {
            system.Update(world, Dt);
        }

        freighter.State.Should().Be(FreighterState.Consumed);
        planet.Structures.Single().State.Should().Be(StructureState.Operational);
        freighter.BuildSite.Should().BeNull();
    }

    [Test]
    public void Update_StructureDestroyedWhileBuilding_FreesTheFreighterToTryAgain()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Red);
        var freighter = new Freighter(new Vector2(0f, -46f), Faction.Red);
        var world = new GameWorld([planet], [freighter]);
        var system = new FreighterSystem();
        system.Update(world, Dt);
        freighter.BeginDocking(planet, -Vector2.UnitY);
        freighter.CompleteDocking();
        system.Update(world, Dt);
        freighter.State.Should().Be(FreighterState.Building);
        planet.Structures.Single().TakeDamage(PlanetStructure.MaxHealth);

        system.Update(world, Dt);

        freighter.State.Should().Be(FreighterState.Traveling, "it is sent again to rebuild");
        freighter.IsDocked.Should().BeFalse();
        freighter.RawMaterials.Should().Be(Freighter.CargoCapacity);
        freighter.BuildSite.Should().BeNull();
    }

    [Test]
    public void Update_AfterAbandoningADestroyedSite_TheRebuildHoldsTheReplacementNotTheWreck()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Red);
        var freighter = new Freighter(new Vector2(0f, -46f), Faction.Red);
        var world = new GameWorld([planet], [freighter]);
        var system = new FreighterSystem();
        system.Update(world, Dt);
        freighter.BeginDocking(planet, -Vector2.UnitY);
        freighter.CompleteDocking();
        system.Update(world, Dt);
        var wreck = planet.Structures.Single();
        wreck.TakeDamage(PlanetStructure.MaxHealth);
        system.Update(world, Dt);
        freighter.BeginDocking(planet, -Vector2.UnitY);
        freighter.CompleteDocking();

        system.Update(world, Dt);

        var replacement = planet.Structures.Single();
        replacement.Should().NotBeSameAs(wreck);
        replacement.State.Should().Be(StructureState.Building);
        freighter.BuildSite.Should().BeSameAs(replacement);
    }

    private static (Planet Planet, Freighter Freighter, GameWorld World, FreighterSystem System) ColonyFreighterOnItsWay()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Red);
        planet.AddStructure(StructureType.Base, StructureState.Operational);
        var freighter = new Freighter(new Vector2(0f, -46f), Faction.Red);
        var world = new GameWorld([planet], [freighter]);
        var system = new FreighterSystem();
        system.Update(world, Dt);
        freighter.State.Should().Be(FreighterState.Traveling);
        freighter.TargetStructure.Should().Be(StructureType.Colony);
        return (planet, freighter, world, system);
    }

    [Test]
    public void Update_BaseDestroyedWhileColonyFreighterIsDocking_DoesNotThrowAndRebuildsTheBase()
    {
        var (planet, freighter, world, system) = ColonyFreighterOnItsWay();
        planet.Structures.Single().TakeDamage(PlanetStructure.MaxHealth);
        freighter.BeginDocking(planet, -Vector2.UnitY);
        freighter.CompleteDocking();

        Assert.DoesNotThrow(() => system.Update(world, Dt));

        planet.HasStructure(StructureType.Colony).Should().BeFalse("a Colony cannot stand without a Base");
        var rebuilt = planet.Structures.Should().ContainSingle().Subject;
        rebuilt.Type.Should().Be(StructureType.Base);
        rebuilt.State.Should().Be(StructureState.Building);
        freighter.State.Should().Be(FreighterState.Building, "it was already touching the planet, so it carries on");
        freighter.TargetStructure.Should().Be(StructureType.Base);
    }

    [Test]
    public void Update_BaseDestroyedMidFlight_RedirectsTheColonyFreighterToRebuildTheBase()
    {
        var (planet, freighter, world, system) = ColonyFreighterOnItsWay();
        planet.Structures.Single().TakeDamage(PlanetStructure.MaxHealth);

        system.Update(world, Dt);

        freighter.State.Should().Be(FreighterState.Traveling);
        freighter.TargetPlanet.Should().BeSameAs(planet);
        freighter.TargetStructure.Should().Be(StructureType.Base);
        freighter.RawMaterials.Should().Be(Freighter.CargoCapacity, "a redirected freighter keeps its cargo");
    }

    [Test]
    public void Update_BaseDestroyedWhileFreighterIsBeingCaptured_KeepsItsDock()
    {
        var (planet, freighter, world, system) = ColonyFreighterOnItsWay();
        freighter.BeginDocking(planet, -Vector2.UnitY);
        planet.Structures.Single().TakeDamage(PlanetStructure.MaxHealth);

        system.Update(world, Dt);

        freighter.DockingTarget.Should().BeSameAs(planet, "it is redirected in place, not sent away and back");
        freighter.ReleasedDockingTarget.Should().BeNull();
        freighter.TargetStructure.Should().Be(StructureType.Base);
        freighter.State.Should().Be(FreighterState.Traveling);
    }

    [Test]
    public void Update_ColonyDestroyedMidFlight_RedirectsTheStarbaseFreighterToRebuildTheColony()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Red);
        planet.AddStructure(StructureType.Base, StructureState.Operational);
        var colony = planet.AddStructure(StructureType.Colony, StructureState.Operational);
        var freighter = new Freighter(new Vector2(0f, -46f), Faction.Red);
        var world = new GameWorld([planet], [freighter]);
        var system = new FreighterSystem();
        system.Update(world, Dt);
        freighter.TargetStructure.Should().Be(StructureType.Starbase);
        colony.TakeDamage(PlanetStructure.MaxHealth);

        system.Update(world, Dt);

        freighter.TargetStructure.Should().Be(StructureType.Colony);
    }

    [Test]
    public void Update_OrderStillMatchesWhatThePlanetNeeds_IsNotRecalled()
    {
        var (planet, freighter, world, system) = ColonyFreighterOnItsWay();

        system.Update(world, Dt);
        system.Update(world, Dt);

        freighter.State.Should().Be(FreighterState.Traveling);
        freighter.TargetStructure.Should().Be(StructureType.Colony);
    }

    [Test]
    public void Update_BaseDestroyedWhileAColonyIsBeingBuilt_TheColonyIsStillFinished()
    {
        // Decided: a build that has started is not cancelled when the structure it depended on is destroyed. The
        // planet then asks for a new Base next, because the Base is what is missing.
        var (planet, freighter, world, system) = ColonyFreighterOnItsWay();
        freighter.BeginDocking(planet, -Vector2.UnitY);
        freighter.CompleteDocking();
        system.Update(world, Dt);
        freighter.State.Should().Be(FreighterState.Building);
        var planetBase = planet.Structures.Single(structure => structure.Type == StructureType.Base);
        planetBase.TakeDamage(PlanetStructure.MaxHealth);

        for (var i = 0; i < 5; i++)
        {
            system.Update(world, 1f);
        }

        var colony = planet.Structures.Single(structure => structure.Type == StructureType.Colony);
        colony.State.Should().Be(StructureState.Operational);
        freighter.State.Should().Be(FreighterState.Consumed);
        planetBase.State.Should().Be(StructureState.Destroyed);
    }
}
