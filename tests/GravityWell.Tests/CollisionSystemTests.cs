using GravityWell.Physics;

namespace GravityWell.Tests;

public class CollisionSystemTests
{
    [TestCase(0f, 0)]
    [TestCase(100f, 0)]
    [TestCase(101f, 1)]
    [TestCase(125f, 1)]
    [TestCase(126f, 2)]
    public void CalculateDamage_AppliesThresholdAndRoundsUp(float closingSpeed, int expected)
    {
        Assert.That(CollisionSystem.CalculateDamage(closingSpeed), Is.EqualTo(expected));
    }
}
