using System.Numerics;
using AwesomeAssertions;
using GravityWell.Core.Entities;

namespace GravityWell.Tests.Entities;

public class StructureLayoutTests
{
    private static readonly Planet Planet = new(new Vector2(100f, 200f));

    [TestCase(StructureType.Base, 100f, 200f)]
    [TestCase(StructureType.Colony, 100f, 219f)]
    [TestCase(StructureType.TechCenter, 83f, 185f)]
    [TestCase(StructureType.CommCenter, 114f, 189f)]
    [TestCase(StructureType.Starbase, 100f, 146f)]
    [TestCase(StructureType.SpaceDock, 100f, 146f)]
    [TestCase(StructureType.SensorArray, 100f, 146f)]
    public void GetAnchor_MatchesTheDrawnPosition(StructureType type, float x, float y)
    {
        StructureLayout.GetAnchor(Planet, type).Should().Be(new Vector2(x, y));
    }

    [TestCase(StructureType.Base, -8f, -8f, 8f, 8f)]
    [TestCase(StructureType.Colony, -15f, -6f, 15f, 6f)]
    [TestCase(StructureType.TechCenter, -7f, -12f, 7f, 6f)]
    [TestCase(StructureType.CommCenter, -6f, -15f, 9f, 8f)]
    [TestCase(StructureType.Starbase, -25f, -7f, 25f, 7f)]
    [TestCase(StructureType.SpaceDock, -15f, -17f, -5f, 0f)]
    [TestCase(StructureType.SensorArray, 3f, -14f, 17f, 0f)]
    public void GetBounds_EnclosesTheDrawnGlyph(StructureType type, float minX, float minY, float maxX, float maxY)
    {
        var anchor = StructureLayout.GetAnchor(Planet, type);

        var bounds = StructureLayout.GetBounds(Planet, type);

        bounds.Min.Should().Be(anchor + new Vector2(minX, minY));
        bounds.Max.Should().Be(anchor + new Vector2(maxX, maxY));
    }
}
