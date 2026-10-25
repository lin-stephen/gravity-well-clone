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
        building.BeginBuilding().Should().BeTrue();

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
        freighter.BeginBuilding().Should().BeTrue();

        freighter.Abandon();

        freighter.State.Should().Be(FreighterState.Idle);
        freighter.TargetPlanet.Should().BeNull();
        freighter.TargetStructure.Should().BeNull();
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
        building.BeginBuilding().Should().BeTrue();

        idle.Redirect(StructureType.Base);
        building.Redirect(StructureType.Base);

        idle.TargetStructure.Should().BeNull();
        building.TargetStructure.Should().Be(StructureType.Colony);
    }
}
