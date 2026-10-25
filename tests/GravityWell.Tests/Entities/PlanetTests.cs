using System.Numerics;
using AwesomeAssertions;
using GravityWell.Core.Entities;

namespace GravityWell.Tests.Entities;

public class PlanetTests
{
    [Test]
    public void Claim_OnAnUnownedPlanet_SetsTheOwner()
    {
        var planet = new Planet(Vector2.Zero);

        planet.Claim(Faction.Red);

        planet.Owner.Should().Be(Faction.Red);
    }

    [Test]
    public void Claim_ByAnotherFactionOnAPlanetWithoutABase_TakesIt()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);

        planet.Claim(Faction.Red);

        planet.Owner.Should().Be(Faction.Red);
    }

    [TestCase(StructureState.Planned)]
    [TestCase(StructureState.Building)]
    [TestCase(StructureState.Operational)]
    public void Claim_ByAnotherFactionOnAPlanetWithABase_IsRefused(StructureState baseState)
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        planet.AddStructure(StructureType.Base, baseState);

        planet.Claim(Faction.Red);

        planet.Owner.Should().Be(Faction.Blue);
    }

    [Test]
    public void Claim_ByTheOwner_ChangesNothing()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);

        planet.Claim(Faction.Blue);

        planet.Owner.Should().Be(Faction.Blue);
    }

    [Test]
    public void Claim_TakingAPlanetKeepsItColonized()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);

        planet.Claim(Faction.Red);

        planet.IsColonized.Should().BeTrue();
    }

    [Test]
    public void AddStructure_AfterTheSameTypeWasDestroyed_ReplacesIt()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Red);
        planet.AddStructure(StructureType.Base, StructureState.Operational).TakeDamage(PlanetStructure.MaxHealth);

        var rebuilt = planet.AddStructure(StructureType.Base);

        planet.Structures.Should().ContainSingle().Which.Should().BeSameAs(rebuilt);
        rebuilt.Health.Should().Be(PlanetStructure.MaxHealth);
        rebuilt.State.Should().Be(StructureState.Planned);
    }

    [Test]
    public void AddStructure_OfAnotherType_KeepsADestroyedStructure()
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Red);
        planet.AddStructure(StructureType.Base, StructureState.Operational);
        var colony = planet.AddStructure(StructureType.Colony, StructureState.Operational);
        colony.TakeDamage(PlanetStructure.MaxHealth);

        planet.AddStructure(StructureType.TechCenter);

        planet.Structures.Should().Contain(colony);
        planet.Structures.Should().HaveCount(3);
    }

    [Test]
    public void StructureQueries_DoNotAllocate()
    {
        // These run for every planet, every frame, from several systems, so any garbage here adds up.
        var planet = new Planet(Vector2.Zero, owner: Faction.Red);
        planet.BuildHomeworld();
        var found = 0;
        for (var i = 0; i < 100; i++)
        {
            found += planet.HasStructure(StructureType.Base) ? 1 : 0; // warm up
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            found += planet.HasStructure(StructureType.SensorArray) ? 1 : 0;
            found += planet.HasOperationalStructure(StructureType.SpaceDock) ? 1 : 0;
        }

        found.Should().BeGreaterThan(0);
        (GC.GetAllocatedBytesForCurrentThread() - before).Should().BeLessThan(1024);
    }
}
