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
}
