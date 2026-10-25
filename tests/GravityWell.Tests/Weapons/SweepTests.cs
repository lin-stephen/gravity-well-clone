using System.Numerics;
using AwesomeAssertions;
using GravityWell.Core.Entities;
using GravityWell.Core.Weapons;

namespace GravityWell.Tests.Weapons;

public class SweepTests
{
    private const float Radius = 10f;
    private static readonly Aabb Box = new(new Vector2(-5f, -5f), new Vector2(5f, 5f));

    [Test]
    public void SegmentCircle_CrossingThrough()
    {
        var hit = Sweep.SegmentCircle(new Vector2(-20f, 0f), new Vector2(20f, 0f), Vector2.Zero, Radius,
            out var enter, out var exit);

        hit.Should().BeTrue();
        enter.Should().BeApproximately(0.25f, 0.001f);
        exit.Should().BeApproximately(0.75f, 0.001f);
    }

    [Test]
    public void SegmentCircle_StartsInside()
    {
        var hit = Sweep.SegmentCircle(Vector2.Zero, new Vector2(20f, 0f), Vector2.Zero, Radius,
            out var enter, out var exit);

        hit.Should().BeTrue();
        enter.Should().Be(0f);
        exit.Should().BeApproximately(0.5f, 0.001f);
    }

    [Test]
    public void SegmentCircle_EndsInside()
    {
        var hit = Sweep.SegmentCircle(new Vector2(-20f, 0f), Vector2.Zero, Vector2.Zero, Radius,
            out var enter, out var exit);

        hit.Should().BeTrue();
        enter.Should().BeApproximately(0.5f, 0.001f);
        exit.Should().BeGreaterThan(1f);
    }

    [Test]
    public void SegmentCircle_StopsShort()
    {
        Sweep.SegmentCircle(new Vector2(-30f, 0f), new Vector2(-20f, 0f), Vector2.Zero, Radius,
            out _, out _).Should().BeFalse();
    }

    [Test]
    public void SegmentCircle_MissesAlongside()
    {
        Sweep.SegmentCircle(new Vector2(-20f, 15f), new Vector2(20f, 15f), Vector2.Zero, Radius,
            out _, out _).Should().BeFalse();
    }

    [Test]
    public void SegmentCircle_ZeroLengthInside_IsAHit()
    {
        var hit = Sweep.SegmentCircle(new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.Zero, Radius,
            out var enter, out _);

        hit.Should().BeTrue();
        enter.Should().Be(0f);
    }

    [Test]
    public void SegmentCircle_ZeroLengthOutside_IsAMiss()
    {
        Sweep.SegmentCircle(new Vector2(30f, 0f), new Vector2(30f, 0f), Vector2.Zero, Radius,
            out _, out _).Should().BeFalse();
    }

    [Test]
    public void SegmentBox_CrossingThrough()
    {
        var hit = Sweep.SegmentBox(new Vector2(-15f, 0f), new Vector2(15f, 0f), Box, out var enter);

        hit.Should().BeTrue();
        enter.Should().BeApproximately(1f / 3f, 0.001f);
    }

    [Test]
    public void SegmentBox_StartsInside_EnterIsZero()
    {
        var hit = Sweep.SegmentBox(Vector2.Zero, new Vector2(15f, 0f), Box, out var enter);

        hit.Should().BeTrue();
        enter.Should().Be(0f);
    }

    [Test]
    public void SegmentBox_StopsShort()
    {
        Sweep.SegmentBox(new Vector2(-15f, 0f), new Vector2(-8f, 0f), Box, out _).Should().BeFalse();
    }

    [Test]
    public void SegmentBox_MissesAbove()
    {
        Sweep.SegmentBox(new Vector2(-15f, 8f), new Vector2(15f, 12f), Box, out _).Should().BeFalse();
    }

    [Test]
    public void SegmentBox_AxisParallelOutsideSlab_IsAMiss()
    {
        Sweep.SegmentBox(new Vector2(-15f, 8f), new Vector2(15f, 8f), Box, out _).Should().BeFalse();
    }

    [Test]
    public void SegmentBox_ZeroLengthInside_IsAHit()
    {
        var hit = Sweep.SegmentBox(new Vector2(1f, 1f), new Vector2(1f, 1f), Box, out var enter);

        hit.Should().BeTrue();
        enter.Should().Be(0f);
    }

    [Test]
    public void SegmentBox_ZeroLengthOutside_IsAMiss()
    {
        Sweep.SegmentBox(new Vector2(9f, 9f), new Vector2(9f, 9f), Box, out _).Should().BeFalse();
    }

    [Test]
    public void SegmentCircle_CircleBehindStart_IsAMiss()
    {
        Sweep.SegmentCircle(new Vector2(20f, 0f), new Vector2(30f, 0f), Vector2.Zero, Radius,
            out _, out _).Should().BeFalse();
    }

    [Test]
    public void SegmentCircle_OffOriginCenter_CrossingThrough()
    {
        var center = new Vector2(100f, 200f);

        var hit = Sweep.SegmentCircle(center + new Vector2(-20f, 0f), center + new Vector2(20f, 0f), center, Radius,
            out var enter, out var exit);

        hit.Should().BeTrue();
        enter.Should().BeApproximately(0.25f, 0.001f);
        exit.Should().BeApproximately(0.75f, 0.001f);
    }

    [Test]
    public void SegmentCircle_OffOriginCenter_MissesWhenOnlyTheOriginIsNear()
    {
        Sweep.SegmentCircle(new Vector2(-20f, 0f), new Vector2(20f, 0f), new Vector2(100f, 200f), Radius,
            out _, out _).Should().BeFalse();
    }

    [Test]
    public void SegmentCircle_Tangent_CountsAsAHit()
    {
        var hit = Sweep.SegmentCircle(new Vector2(-20f, Radius), new Vector2(20f, Radius), Vector2.Zero, Radius,
            out var enter, out var exit);

        hit.Should().BeTrue();
        enter.Should().BeApproximately(0.5f, 0.001f);
        exit.Should().BeApproximately(0.5f, 0.001f);
    }

    [Test]
    public void SegmentCircle_StartsAndEndsInside()
    {
        var hit = Sweep.SegmentCircle(new Vector2(-2f, 0f), new Vector2(2f, 0f), Vector2.Zero, Radius,
            out var enter, out var exit);

        hit.Should().BeTrue();
        enter.Should().Be(0f);
        exit.Should().BeGreaterThan(1f);
    }

    [TestCase(float.NaN, 0f, 20f, 0f, 0f, 0f, 10f)]
    [TestCase(-20f, 0f, 20f, 0f, float.NaN, 0f, 10f)]
    [TestCase(-20f, 0f, 20f, 0f, 0f, 0f, float.NaN)]
    [TestCase(-20f, 0f, float.PositiveInfinity, 0f, 0f, 0f, 10f)]
    public void SegmentCircle_NonFiniteInput_IsAMiss(float fromX, float fromY, float toX, float toY,
        float centerX, float centerY, float radius)
    {
        Sweep.SegmentCircle(new Vector2(fromX, fromY), new Vector2(toX, toY), new Vector2(centerX, centerY),
            radius, out _, out _).Should().BeFalse();
    }

    [Test]
    public void SegmentBox_UnevenOffOriginBox_EntersThroughAYFace()
    {
        var box = new Aabb(new Vector2(10f, 20f), new Vector2(30f, 26f));

        var hit = Sweep.SegmentBox(new Vector2(15f, 0f), new Vector2(15f, 40f), box, out var enter);

        hit.Should().BeTrue();
        enter.Should().BeApproximately(0.5f, 0.001f);
    }

    [Test]
    public void SegmentBox_UnevenOffOriginBox_EntersThroughAnXFace()
    {
        var box = new Aabb(new Vector2(10f, 20f), new Vector2(30f, 26f));

        var hit = Sweep.SegmentBox(new Vector2(0f, 23f), new Vector2(40f, 23f), box, out var enter);

        hit.Should().BeTrue();
        enter.Should().BeApproximately(0.25f, 0.001f);
    }

    [Test]
    public void SegmentBox_UnevenOffOriginBox_MissesAlongAnotherAxis()
    {
        var box = new Aabb(new Vector2(10f, 20f), new Vector2(30f, 26f));

        Sweep.SegmentBox(new Vector2(0f, 10f), new Vector2(40f, 10f), box, out _).Should().BeFalse();
    }

    [Test]
    public void SegmentBox_BoxBehindStart_IsAMiss()
    {
        Sweep.SegmentBox(new Vector2(15f, 0f), new Vector2(25f, 0f), Box, out _).Should().BeFalse();
    }

    [Test]
    public void SegmentBox_NonFiniteInput_IsAMiss()
    {
        Sweep.SegmentBox(new Vector2(float.NaN, 0f), new Vector2(15f, 0f), Box, out _).Should().BeFalse();
    }
}
