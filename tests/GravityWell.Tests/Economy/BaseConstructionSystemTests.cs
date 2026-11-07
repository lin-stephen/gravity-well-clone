using System.ComponentModel.DataAnnotations;
using System.Numerics;
using GravityWell.Core.Economy;
using GravityWell.Core.Entities;
using GravityWell.Core.World;

namespace GravityWell.Tests.Economy;

public class BaseConstructionSystemTests
{
    [Test]
    public void BuildsTechThenComm_AndChargesEachOnlyOnce()
    {
        var (world, planet) = Settlement();
        planet.BaseResources.AddFinishedMaterials(12);
        var system = new BaseConstructionSystem();

        system.Update(world, 2f);
        var tech = Facility(planet, StructureType.TechCenter);
        Assert.Multiple(() =>
        {
            Assert.That(tech.State, Is.EqualTo(StructureState.Building));
            Assert.That(tech.ConstructionProgress, Is.EqualTo(0.4f).Within(0.0001));
            Assert.That(planet.BaseResources.FinishedMaterials, Is.EqualTo(7));
            Assert.That(planet.HasStructure(StructureType.CommCenter), Is.False);
        });
        system.Update(world, 3f);
        Assert.That(tech.State, Is.EqualTo(StructureState.Operational));
        system.Update(world, 5f);
        system.Update(world, 100f);
        Assert.Multiple(() =>
        {
            Assert.That(planet.HasOperationalStructure(StructureType.CommCenter), Is.True);
            Assert.That(planet.BaseResources.FinishedMaterials, Is.EqualTo(2));
            Assert.That(planet.Structures.Count(site => site.Type == StructureType.TechCenter), Is.EqualTo(1));
            Assert.That(planet.Structures.Count(site => site.Type == StructureType.CommCenter), Is.EqualTo(1));
        });
    }

    [Test]
    public void WaitsForFullCost_AndNeverUsesOrbitalStores()
    {
        var (world, planet) = Settlement();
        planet.BaseResources.AddFinishedMaterials(4);
        planet.StarbaseResources.AddFinishedMaterials(20);
        var system = new BaseConstructionSystem();
        system.Update(world, 100f);
        Assert.That(planet.HasStructure(StructureType.TechCenter), Is.False);
        Assert.That(planet.BaseResources.FinishedMaterials, Is.EqualTo(4));

        planet.BaseResources.AddFinishedMaterials(1);
        system.Update(world, 1f);
        Assert.That(planet.BaseResources.FinishedMaterials, Is.Zero);
        Assert.That(Facility(planet, StructureType.TechCenter).ConstructionProgress, Is.EqualTo(0.2f));
        Assert.That(planet.StarbaseResources.FinishedMaterials, Is.EqualTo(20));
        system.Update(world, 4f);
        Assert.That(planet.HasOperationalStructure(StructureType.TechCenter), Is.True);
        Assert.That(planet.HasStructure(StructureType.CommCenter), Is.False);
    }

    [TestCase(StructureState.Planned)]
    [TestCase(StructureState.Building)]
    [TestCase(StructureState.Destroyed)]
    public void RequiresOperationalBase(StructureState state)
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Red);
        planet.AddStructure(StructureType.Base, state);
        planet.BaseResources.AddFinishedMaterials(20);
        new BaseConstructionSystem().Update(new GameWorld([planet], []), 100f);
        Assert.That(planet.HasStructure(StructureType.TechCenter), Is.False);
        Assert.That(planet.BaseResources.FinishedMaterials, Is.EqualTo(20));
    }

    [Test]
    public void DeadPlanetDoesNotBuild()
    {
        var (world, planet) = Settlement();
        planet.Health = 0;
        planet.BaseResources.AddFinishedMaterials(20);
        new BaseConstructionSystem().Update(world, 100f);
        Assert.That(planet.HasStructure(StructureType.TechCenter), Is.False);
    }

    [Test]
    public void BarrenPlanetBuildsFromImportedStock_WithCustomCostsAndDuration()
    {
        var (world, planet) = Settlement(PlanetType.Barren);
        planet.BaseResources.AddFinishedMaterials(7);
        var system = new BaseConstructionSystem(new BaseConstructionSettings
        {
            TechCenterCost = 3, CommCenterCost = 4, ConstructionDurationSeconds = 2
        });
        system.Update(world, 4f);
        Assert.That(planet.HasOperationalStructure(StructureType.TechCenter), Is.True);
        Assert.That(planet.HasOperationalStructure(StructureType.CommCenter), Is.True);
        Assert.That(planet.BaseResources.FinishedMaterials, Is.Zero);
    }

    [Test]
    public void DestroyedFacilityIsRebuilt_WithNewCostAndProgress()
    {
        var (world, planet) = Settlement();
        planet.BaseResources.AddFinishedMaterials(15);
        var system = new BaseConstructionSystem();
        system.Update(world, 10f);
        var old = Facility(planet, StructureType.TechCenter);
        old.TakeDamage(PlanetStructure.MaxHealth);
        system.Update(world, 1f);
        var replacement = Facility(planet, StructureType.TechCenter);
        Assert.That(replacement, Is.Not.SameAs(old));
        Assert.That(replacement.State, Is.EqualTo(StructureState.Building));
        Assert.That(replacement.ConstructionProgress, Is.EqualTo(0.2f));
        Assert.That(planet.BaseResources.FinishedMaterials, Is.Zero);
    }

    [Test]
    public void LosingBaseCancelsPaidBuild_WithoutRefund()
    {
        var (world, planet) = Settlement();
        planet.BaseResources.AddFinishedMaterials(10);
        var system = new BaseConstructionSystem();
        system.Update(world, 2f);
        var tech = Facility(planet, StructureType.TechCenter);
        Facility(planet, StructureType.Base).TakeDamage(PlanetStructure.MaxHealth);
        system.Update(world, 1f);
        Assert.That(tech.State, Is.EqualTo(StructureState.Destroyed));
        Assert.That(planet.BaseResources.FinishedMaterials, Is.EqualTo(5));
        planet.AddStructure(StructureType.Base, StructureState.Operational);
        system.Update(world, 1f);
        Assert.That(Facility(planet, StructureType.TechCenter).ConstructionProgress, Is.EqualTo(0.2f));
        Assert.That(planet.BaseResources.FinishedMaterials, Is.Zero);
    }

    [Test]
    public void PlannedFacilityIsStartedWithoutDuplication()
    {
        var (world, planet) = Settlement();
        var planned = planet.AddStructure(StructureType.TechCenter);
        planet.BaseResources.AddFinishedMaterials(5);
        new BaseConstructionSystem().Update(world, 5f);
        Assert.That(planned.State, Is.EqualTo(StructureState.Operational));
        Assert.That(planet.Structures.Count(site => site.Type == StructureType.TechCenter), Is.EqualTo(1));
    }

    [TestCase(0f)]
    [TestCase(-1f)]
    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    public void InvalidTimeDoesNotSpendOrBuild(float deltaTime)
    {
        var (world, planet) = Settlement();
        planet.BaseResources.AddFinishedMaterials(20);
        new BaseConstructionSystem().Update(world, deltaTime);
        Assert.That(planet.BaseResources.FinishedMaterials, Is.EqualTo(20));
        Assert.That(planet.HasStructure(StructureType.TechCenter), Is.False);
    }

    [TestCase(1f / 30)]
    [TestCase(1f / 60)]
    [TestCase(1f / 120)]
    [TestCase(11f)]
    public void ConstructionCompletesAcrossStepSizes(float step)
    {
        var (world, planet) = Settlement();
        planet.BaseResources.AddFinishedMaterials(10);
        var system = new BaseConstructionSystem();
        var steps = (int)MathF.Round(11f / step);
        for (var i = 0; i < steps; i++)
        {
            system.Update(world, step);
        }
        Assert.That(planet.HasOperationalStructure(StructureType.TechCenter), Is.True);
        Assert.That(planet.HasOperationalStructure(StructureType.CommCenter), Is.True);
        Assert.That(planet.BaseResources.FinishedMaterials, Is.Zero);
    }

    [Test]
    public void InvalidSettingsAreRejected()
    {
        Assert.Throws<ValidationException>(() => new BaseConstructionSystem(new BaseConstructionSettings { TechCenterCost = 0 }));
        Assert.Throws<ValidationException>(() => new BaseConstructionSystem(new BaseConstructionSettings { CommCenterCost = 21 }));
        Assert.Throws<ValidationException>(() => new BaseConstructionSystem(new BaseConstructionSettings { ConstructionDurationSeconds = 0 }));
    }

    private static PlanetStructure Facility(Planet planet, StructureType type) =>
        planet.Structures.Single(site => site.Type == type);

    private static (GameWorld World, Planet Planet) Settlement(PlanetType type = PlanetType.Habitable)
    {
        var planet = new Planet(Vector2.Zero, type, Faction.Blue);
        planet.AddStructure(StructureType.Base, StructureState.Operational);
        return (new GameWorld([planet], []), planet);
    }
}
