using System.Numerics;
using AwesomeAssertions;
using GravityWell.Core.Entities;

namespace GravityWell.Tests.Entities;

public class FreighterTests
{
    private static Freighter DispatchedFreighter(Planet planet)
    {
        var freighter = new Freighter(new Vector2(500f, 0f), Faction.Blue);
        freighter.Dispatch(planet, StructureType.Base);
        return freighter;
    }

    private static PlanetStructure BuildingSite(Planet planet, StructureType type)
    {
        if (type == StructureType.Colony)
        {
            planet.AddStructure(StructureType.Base, StructureState.Operational);
        }

        return planet.AddStructure(type, StructureState.Building);
    }

    [Test]
    public void Recall_ReturnsATravelingFreighterToIdleWithItsCargo()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        var freighter = DispatchedFreighter(planet);

        freighter.Recall();

        freighter.State.Should().Be(FreighterState.Idle);
        freighter.TargetPlanet.Should().BeNull();
        freighter.TargetStructure.Should().BeNull();
        freighter.RawMaterials.Should().Be(Freighter.CargoCapacity);
        freighter.Invoking(ship => ship.Dispatch(planet, StructureType.Base)).Should().NotThrow();
    }

    [Test]
    public void Recall_ReleasesADockedFreighter()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        var freighter = DispatchedFreighter(planet);
        freighter.BeginDocking(planet, Vector2.UnitY);
        freighter.CompleteDocking();

        freighter.Recall();

        freighter.DockingTarget.Should().BeNull();
        freighter.IsDocked.Should().BeFalse();
    }

    [Test]
    public void Recall_OfAFreighterThatIsNotTraveling_ChangesNothing()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        var idle = new Freighter(Vector2.Zero, Faction.Blue);
        var building = DispatchedFreighter(planet);
        building.BeginDocking(planet, Vector2.UnitY);
        building.CompleteDocking();
        building.BeginBuilding(BuildingSite(planet, StructureType.Base));

        idle.Recall();
        building.Recall();

        idle.State.Should().Be(FreighterState.Idle);
        building.State.Should().Be(FreighterState.Building);
        building.TargetPlanet.Should().BeSameAs(planet);
    }

    [Test]
    public void Abandon_ReturnsABuildingFreighterToIdleWithItsCargo()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        var freighter = new Freighter(new Vector2(0f, -46f), Faction.Blue);
        freighter.Dispatch(planet, StructureType.Base);
        freighter.BeginDocking(planet, -Vector2.UnitY);
        freighter.CompleteDocking();
        freighter.BeginBuilding(BuildingSite(planet, StructureType.Base));

        freighter.Abandon();

        freighter.State.Should().Be(FreighterState.Idle);
        freighter.TargetPlanet.Should().BeNull();
        freighter.TargetStructure.Should().BeNull();
        freighter.BuildSite.Should().BeNull();
        freighter.DockingTarget.Should().BeNull();
        freighter.RawMaterials.Should().Be(Freighter.CargoCapacity);
    }

    [Test]
    public void Abandon_OfAFreighterThatIsNotBuilding_ChangesNothing()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        var traveling = new Freighter(new Vector2(500f, 0f), Faction.Blue);
        traveling.Dispatch(planet, StructureType.Base);
        var idle = new Freighter(new Vector2(500f, 0f), Faction.Blue);

        traveling.Abandon();
        idle.Abandon();

        traveling.State.Should().Be(FreighterState.Traveling);
        traveling.TargetPlanet.Should().BeSameAs(planet);
        idle.State.Should().Be(FreighterState.Idle);
    }

    [Test]
    public void Redirect_ChangesWhatATravelingFreighterWillBuild_AndKeepsItsDockAndCargo()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        var freighter = new Freighter(new Vector2(0f, -46f), Faction.Blue);
        freighter.Dispatch(planet, StructureType.Colony);
        freighter.BeginDocking(planet, -Vector2.UnitY);

        freighter.Redirect(StructureType.Base);

        freighter.TargetStructure.Should().Be(StructureType.Base);
        freighter.TargetPlanet.Should().BeSameAs(planet);
        freighter.State.Should().Be(FreighterState.Traveling);
        freighter.DockingTarget.Should().BeSameAs(planet);
        freighter.RawMaterials.Should().Be(Freighter.CargoCapacity);
    }

    [Test]
    public void Redirect_OfAFreighterThatIsNotTraveling_ChangesNothing()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        var idle = new Freighter(new Vector2(500f, 0f), Faction.Blue);
        var building = new Freighter(new Vector2(0f, -46f), Faction.Blue);
        building.Dispatch(planet, StructureType.Colony);
        building.BeginDocking(planet, -Vector2.UnitY);
        building.CompleteDocking();
        building.BeginBuilding(BuildingSite(planet, StructureType.Colony));

        idle.Redirect(StructureType.Base);
        building.Redirect(StructureType.Base);

        idle.TargetStructure.Should().BeNull();
        building.TargetStructure.Should().Be(StructureType.Colony);
    }

    [Test]
    public void HasArrived_IsTrueOnlyOnceDockedAtTheTargetPlanet()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        var freighter = new Freighter(new Vector2(0f, -46f), Faction.Blue);
        freighter.HasArrived.Should().BeFalse("an idle freighter has nowhere to be");
        freighter.Dispatch(planet, StructureType.Base);
        freighter.HasArrived.Should().BeFalse("it has not docked yet");
        freighter.BeginDocking(planet, -Vector2.UnitY);
        freighter.CompleteDocking();

        freighter.HasArrived.Should().BeTrue();
    }

    [Test]
    public void HasArrived_IsFalseWhenDockedAtADifferentPlanet()
    {
        var target = new Planet(Vector2.Zero, owner: Faction.Blue);
        var other = new Planet(new Vector2(500f, 0f), owner: Faction.Blue);
        var freighter = new Freighter(new Vector2(500f, -46f), Faction.Blue);
        freighter.Dispatch(target, StructureType.Base);
        freighter.BeginDocking(other, -Vector2.UnitY);
        freighter.CompleteDocking();

        freighter.HasArrived.Should().BeFalse();
    }

    [Test]
    public void BeginBuilding_HoldsTheSiteItIsBuilding()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        var freighter = new Freighter(new Vector2(0f, -46f), Faction.Blue);
        freighter.Dispatch(planet, StructureType.Base);
        freighter.BeginDocking(planet, -Vector2.UnitY);
        freighter.CompleteDocking();
        var site = BuildingSite(planet, StructureType.Base);

        freighter.BeginBuilding(site);

        freighter.State.Should().Be(FreighterState.Building);
        freighter.BuildSite.Should().BeSameAs(site);
    }

    [Test]
    public void BeginBuilding_BeforeArriving_Throws()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        var freighter = DispatchedFreighter(planet);
        var site = new PlanetStructure(StructureType.Base, StructureState.Building);

        freighter.Invoking(ship => ship.BeginBuilding(site)).Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void BeginBuilding_OfAStructureTypeThatWasNotOrdered_Throws()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        var freighter = new Freighter(new Vector2(0f, -46f), Faction.Blue);
        freighter.Dispatch(planet, StructureType.Base);
        freighter.BeginDocking(planet, -Vector2.UnitY);
        freighter.CompleteDocking();
        var colony = BuildingSite(planet, StructureType.Colony);

        freighter.Invoking(ship => ship.BeginBuilding(colony)).Should().Throw<ArgumentException>();
    }

    [Test]
    public void BeginBuilding_OfAStructureThatIsNotOnTheTargetPlanet_Throws()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        var freighter = new Freighter(new Vector2(0f, -46f), Faction.Blue);
        freighter.Dispatch(planet, StructureType.Base);
        freighter.BeginDocking(planet, -Vector2.UnitY);
        freighter.CompleteDocking();
        var elsewhere = new PlanetStructure(StructureType.Base, StructureState.Building);

        freighter.Invoking(ship => ship.BeginBuilding(elsewhere)).Should().Throw<ArgumentException>();
    }

    [Test]
    public void BeginBuilding_OfAStructureThatIsNotUnderConstruction_Throws()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        var freighter = new Freighter(new Vector2(0f, -46f), Faction.Blue);
        freighter.Dispatch(planet, StructureType.Base);
        freighter.BeginDocking(planet, -Vector2.UnitY);
        freighter.CompleteDocking();
        var planned = planet.AddStructure(StructureType.Base);

        freighter.Invoking(ship => ship.BeginBuilding(planned)).Should().Throw<ArgumentException>();
    }

    [Test]
    public void BeginBuilding_OfADestroyedStructure_Throws()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        var freighter = new Freighter(new Vector2(0f, -46f), Faction.Blue);
        freighter.Dispatch(planet, StructureType.Base);
        freighter.BeginDocking(planet, -Vector2.UnitY);
        freighter.CompleteDocking();
        var wreck = planet.AddStructure(StructureType.Base, StructureState.Destroyed);

        freighter.Invoking(ship => ship.BeginBuilding(wreck)).Should().Throw<ArgumentException>();
    }
}
