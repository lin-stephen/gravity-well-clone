using System.Numerics;
using AwesomeAssertions;
using GravityWell.Core.Entities;

namespace GravityWell.Tests.Entities;

public class PlanetStructureTests
{
    [TestCase(StructureState.Planned)]
    [TestCase(StructureState.Building)]
    [TestCase(StructureState.Operational)]
    public void NewStructure_HasFullHealth(StructureState state)
    {
        var structure = new PlanetStructure(StructureType.Base, state);

        structure.Health.Should().Be(PlanetStructure.MaxHealth);
    }

    [Test]
    public void TakeDamage_OperationalStructure_LosesHealth()
    {
        var structure = new PlanetStructure(StructureType.Base, StructureState.Operational);

        structure.TakeDamage(1);

        structure.Health.Should().Be(PlanetStructure.MaxHealth - 1);
        structure.State.Should().Be(StructureState.Operational);
    }

    [Test]
    public void TakeDamage_ToZero_DestroysStructureAndClamps()
    {
        var structure = new PlanetStructure(StructureType.Base, StructureState.Operational);

        structure.TakeDamage(PlanetStructure.MaxHealth + 3);

        structure.Health.Should().Be(0);
        structure.State.Should().Be(StructureState.Destroyed);
    }

    [Test]
    public void TakeDamage_BuildingStructure_KeepsConstructionProgress()
    {
        var structure = new PlanetStructure(StructureType.Base, StructureState.Building);
        structure.AdvanceConstruction(0.5f);

        structure.TakeDamage(1);

        structure.ConstructionProgress.Should().Be(0.5f);
        structure.Health.Should().Be(PlanetStructure.MaxHealth - 1);
    }

    [Test]
    public void TakeDamage_PlannedStructure_IsUnchanged()
    {
        var structure = new PlanetStructure(StructureType.Base, StructureState.Planned);

        structure.TakeDamage(1);

        structure.Health.Should().Be(PlanetStructure.MaxHealth);
        structure.State.Should().Be(StructureState.Planned);
    }

    [Test]
    public void TakeDamage_DestroyedStructure_IsUnchanged()
    {
        var structure = new PlanetStructure(StructureType.Base, StructureState.Operational);
        structure.TakeDamage(PlanetStructure.MaxHealth);

        structure.TakeDamage(1);

        structure.Health.Should().Be(0);
        structure.State.Should().Be(StructureState.Destroyed);
    }

    [TestCase(StructureState.Planned, false)]
    [TestCase(StructureState.Building, true)]
    [TestCase(StructureState.Operational, true)]
    public void IsTargetable_OnlyForBuildingAndOperational(StructureState state, bool expected)
    {
        new PlanetStructure(StructureType.Base, state).IsTargetable.Should().Be(expected);
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(int.MinValue)]
    public void TakeDamage_NonPositiveDamage_IsIgnored(int damage)
    {
        var structure = new PlanetStructure(StructureType.Base, StructureState.Operational);
        structure.TakeDamage(1);

        structure.TakeDamage(damage);

        structure.Health.Should().Be(PlanetStructure.MaxHealth - 1);
        structure.State.Should().Be(StructureState.Operational);
    }

    [Test]
    public void NewDestroyedStructure_HasNoHealthAndIsNotTargetable()
    {
        var structure = new PlanetStructure(StructureType.Base, StructureState.Destroyed);

        structure.Health.Should().Be(0);
        structure.IsTargetable.Should().BeFalse();
    }

    [Test]
    public void TakeDamage_ExactlyMaxHealth_DestroysAndIsNoLongerTargetable()
    {
        var structure = new PlanetStructure(StructureType.Base, StructureState.Operational);

        structure.TakeDamage(PlanetStructure.MaxHealth);

        structure.Health.Should().Be(0);
        structure.State.Should().Be(StructureState.Destroyed);
        structure.IsTargetable.Should().BeFalse();
    }

    [Test]
    public void TakeDamage_LethalHitOnBuildingStructure_DestroysIt()
    {
        var structure = new PlanetStructure(StructureType.Base, StructureState.Building);

        structure.TakeDamage(PlanetStructure.MaxHealth);

        structure.State.Should().Be(StructureState.Destroyed);
    }

    [Test]
    public void DestroyedStructure_NoLongerCountsOnPlanet()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Red);
        var planetBase = planet.AddStructure(StructureType.Base, StructureState.Operational);

        planetBase.TakeDamage(PlanetStructure.MaxHealth);

        planet.HasStructure(StructureType.Base).Should().BeFalse();
        planet.HasOperationalStructure(StructureType.Base).Should().BeFalse();
    }
}
