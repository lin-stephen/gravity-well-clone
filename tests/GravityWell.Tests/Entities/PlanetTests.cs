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
}
