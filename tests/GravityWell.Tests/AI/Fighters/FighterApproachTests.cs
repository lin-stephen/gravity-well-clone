using System.Numerics;
using AwesomeAssertions;
using GravityWell.Core.AI;
using GravityWell.Core.AI.Fighters;
using GravityWell.Core.Entities;
using GravityWell.Core.Physics;
using GravityWell.Core.World;

namespace GravityWell.Tests.AI.Fighters;

/// <summary>
/// Flies a Red AI fighter at full fighter speed to a free planet at the origin through the real
/// game loop, so the fighter's approach tuning is proven rather than assumed.
/// </summary>
public class FighterApproachTests
{
    private const float Dt = 1f / 60f;

    // TestCaseData takes positional arguments, so these name the otherwise opaque null/true/false values.
    private static readonly Planet[]? NoObstacles = null;
    private const bool NoBump = false;
    private const bool Bump = true;

    private sealed record Flight(Fighter Fighter, Planet Target, Freighter? Freighter, bool Finite,
        float ClosestFreighter, float PeakSpeed, float DockSeconds, float PeakSpeedBeforeX,
        Planet? FighterTarget);

    private static Flight Fly(Vector2 start, Vector2 velocity, Planet[]? obstacles = null, bool bump = false,
        Freighter? freighter = null, float seconds = 120f, float? measureBeforeX = null)
    {
        var target = new Planet(Vector2.Zero);
        var fighter = new Fighter { Faction = Faction.Red, Position = start, Velocity = velocity, Rotation = 90f };
        var world = new GameWorld((obstacles ?? []).Append(target), freighter is null ? [] : [freighter], [fighter]);
        world.Player.Position = new Vector2(10000f, 10000f);

        var autopilot = new FighterAutopilot();
        var physics = new PhysicsSystem();
        var docking = new DockingSystem();
        var collisions = new CollisionSystem();

        var finite = true;
        var closest = float.PositiveInfinity;
        var peakSpeed = 0f;
        var dockSeconds = float.NaN;
        var peakBeforeX = 0f;
        for (var step = 0; step < (int)(seconds / Dt); step++)
        {
            if (bump && step == (int)(4f / Dt))
            {
                fighter.ApplyCollision(Vector2.Zero, new Vector2(-60f, 65f));
            }

            autopilot.Update(world, Dt);
            physics.Update(world, default, Dt);
            docking.Update(world, Dt);
            collisions.Update(world);
            finite &= float.IsFinite(fighter.Position.X) && float.IsFinite(fighter.Position.Y) &&
                      float.IsFinite(fighter.Velocity.X) && float.IsFinite(fighter.Velocity.Y);
            peakSpeed = MathF.Max(peakSpeed, fighter.Velocity.Length());
            if (measureBeforeX is { } limit && fighter.Position.X < limit)
            {
                peakBeforeX = MathF.Max(peakBeforeX, fighter.Velocity.Length());
            }

            if (freighter is not null)
            {
                closest = MathF.Min(closest, Vector2.Distance(fighter.Position, freighter.Position));
            }

            if (fighter.IsDocked)
            {
                dockSeconds = (step + 1) * Dt;
                break;
            }
        }
        return new Flight(fighter, target, freighter, finite, closest, peakSpeed, dockSeconds, peakBeforeX,
            autopilot.GetTarget(fighter));
    }

    private static IEnumerable<TestCaseData> Approaches()
    {
        yield return new TestCaseData(
            new Vector2(-900f, 0f),
            Vector2.Zero,
            NoObstacles,
            NoBump).SetName("Fighter docks from rest");

        yield return new TestCaseData(
            new Vector2(-900f, 0f),
            new Vector2(260f, 0f),
            NoObstacles,
            NoBump).SetName("Fighter docks after arriving at full speed");

        yield return new TestCaseData(
            new Vector2(-500f, 0f),
            new Vector2(260f, 0f),
            NoObstacles,
            NoBump).SetName("Fighter docks after a late brake from full speed");

        yield return new TestCaseData(
            new Vector2(0f, 800f),
            new Vector2(200f, -150f),
            NoObstacles,
            NoBump).SetName("Fighter docks despite sideways drift");

        yield return new TestCaseData(
            new Vector2(-1200f, 0f),
            Vector2.Zero,
            new[] { new Planet(new Vector2(-600f, 0f), owner: Faction.Blue) },
            NoBump).SetName("Fighter docks around a blocking planet");

        yield return new TestCaseData(
            new Vector2(-900f, 0f),
            Vector2.Zero,
            NoObstacles,
            Bump).SetName("Fighter recovers from a bump");
    }

    [TestCaseSource(nameof(Approaches))]
    public void Fighter_DocksRearFirstWithoutDamage(Vector2 start, Vector2 velocity, Planet[]? obstacles, bool bump)
    {
        var flight = Fly(start, velocity, obstacles, bump);

        flight.Finite.Should().BeTrue();
        flight.Fighter.IsDocked.Should().BeTrue();
        flight.Fighter.DockingTarget.Should().BeSameAs(flight.Target);
        flight.Fighter.Health.Should().Be(Fighter.MaxHealth);
        new DockingSystem().IsRearAligned(flight.Fighter, flight.Target).Should().BeTrue();
    }

    [Test]
    public void Fighter_FliesAtFighterSpeedNotFreighterPace()
    {
        var flight = Fly(new Vector2(-3000f, 0f), Vector2.Zero);

        flight.Fighter.IsDocked.Should().BeTrue();
        flight.PeakSpeed.Should().BeGreaterThan(Freighter.Speed * 2f);
        flight.PeakSpeed.Should().BeLessThanOrEqualTo(Fighter.MaxSpeed);
    }

    [Test]
    public void Fighter_UsesFighterSpeedOnTypicalMapLegs()
    {
        // A typical leg on the real map is under 1000 units, far short of the distance at which
        // a conservative braking profile would ever allow the fighter's top speed.
        var flight = Fly(new Vector2(-900f, 0f), Vector2.Zero);

        flight.Fighter.IsDocked.Should().BeTrue();
        flight.PeakSpeed.Should().BeGreaterThan(200f);
        flight.DockSeconds.Should().BeLessThan(14f);
    }

    [Test]
    public void Fighter_DetourLegsAreNotSlowerThanFreighterPace()
    {
        var blocker = new Planet(new Vector2(-600f, 0f), owner: Faction.Blue);

        // Only the approach to the blocker is measured: the straight run after it uses the faster
        // cruise cap and would hide a slow waypoint cap.
        var flight = Fly(new Vector2(-1200f, 0f), Vector2.Zero, [blocker], measureBeforeX: -600f);

        flight.Fighter.IsDocked.Should().BeTrue();
        flight.PeakSpeedBeforeX.Should().BeGreaterThan(Freighter.Speed * 1.5f);
    }

    [TestCase(-450f, 0f, 0f, 0f, TestName = "Fighter steers around a stationary freighter in its path")]
    [TestCase(-300f, 0f, -40f, 0f, TestName = "Fighter passes a freighter flying head-on")]
    [TestCase(-450f, -300f, 0f, 90f, TestName = "Fighter crosses paths with a freighter at 90 u/s")]
    public void Fighter_AvoidsFreighterInItsPath(float freighterX, float freighterY, float velocityX, float velocityY)
    {
        var freighter = new Freighter(new Vector2(freighterX, freighterY), Faction.Red)
            { Velocity = new Vector2(velocityX, velocityY) };

        var flight = Fly(new Vector2(-900f, 0f), Vector2.Zero, freighter: freighter);

        flight.Finite.Should().BeTrue();
        // The limit is the two hulls' radii, so failing it means they touched. The 90 u/s crossing case
        // clears it by only about 3.6 units (25.6 against 22 when this was written), so a small change to
        // Seek's avoidance constants can tip it over. Read the measured closest approach: a value just
        // under 22 means that margin was lost, while one far below means avoidance has broken.
        flight.ClosestFreighter.Should().BeGreaterThanOrEqualTo(Fighter.Radius + Freighter.Radius,
            "the crossing case had only about 3.6 units to spare when this test was written");
        flight.Fighter.Health.Should().Be(Fighter.MaxHealth);
        flight.Freighter!.Health.Should().Be(Freighter.MaxHealth);
        flight.Fighter.IsDocked.Should().BeTrue();
        flight.Fighter.DockingTarget.Should().BeSameAs(flight.Target);
    }

    [Test]
    public void Fighter_BlockedByEnclosingPlanets_KeepsRetryingSafely()
    {
        // Twelve owned planets on a ring of radius 130 overlap into a wall around the free target.
        // They are owned so the fighter treats them as obstacles, not as targets.
        Planet[] ring = [.. Enumerable.Range(0, 12).Select(i =>
            new Planet(new Vector2(MathF.Cos(i * MathF.Tau / 12f), MathF.Sin(i * MathF.Tau / 12f)) * 130f,
                owner: Faction.Blue))];

        var flight = Fly(new Vector2(-900f, 0f), Vector2.Zero, ring, seconds: 30f);

        flight.Finite.Should().BeTrue();
        flight.Fighter.Health.Should().Be(Fighter.MaxHealth);
        flight.Fighter.IsDocked.Should().BeFalse();
        flight.FighterTarget.Should().BeSameAs(flight.Target, "an unreachable target is retried, not given up on");
    }
}
