using System.Numerics;
using AwesomeAssertions;
using GravityWell.Core.AI;
using GravityWell.Core.AI.Fighters;
using GravityWell.Core.Colonization;
using GravityWell.Core.Entities;
using GravityWell.Core.Input;
using GravityWell.Core.Physics;
using GravityWell.Core.World;

namespace GravityWell.Tests.AI.Fighters;

public class FighterAutopilotTests
{
    private const float Dt = 1f / 60f;

    private static GameWorld NewWorld(Planet[] planets, params Fighter[] aiFighters)
    {
        var world = new GameWorld(planets, [], aiFighters)
        {
            Player =
            {
                Position = new Vector2(10000f, 10000f)
            }
        };
        return world;
    }

    private static Fighter NewFighter(Faction faction, Vector2 position) =>
        new() { Faction = faction, Position = position, Velocity = Vector2.Zero, Rotation = 90f };

    /// <summary>Runs the real loop (autopilot, physics, docking, collisions, colonization) for the given time.</summary>
    private static void Run(GameWorld world, FighterAutopilot autopilot, float seconds, Action<int>? afterStep = null)
    {
        var physics = new PhysicsSystem();
        var docking = new DockingSystem();
        var collisions = new CollisionSystem();
        var colonization = new ColonizationSystem();
        for (var step = 0; step < (int)(seconds / Dt); step++)
        {
            autopilot.Update(world, Dt);
            physics.Update(world, default, Dt);
            docking.Update(world, Dt);
            collisions.Update(world);
            colonization.Update(world);
            afterStep?.Invoke(step);
        }
    }

    [Test]
    public void Update_PicksNearestUncolonizedPlanet()
    {
        var nearest = new Planet(new Vector2(-600f, 0f));
        var far = new Planet(new Vector2(1500f, 0f));
        var ownedButCloser = new Planet(new Vector2(100f, 300f), owner: Faction.Blue);
        var fighter = NewFighter(Faction.Red, Vector2.Zero);
        var world = NewWorld([far, ownedButCloser, nearest], fighter);
        var autopilot = new FighterAutopilot();

        autopilot.Update(world, Dt);

        autopilot.GetTarget(fighter).Should().BeSameAs(nearest);
    }

    [Test]
    public void Update_ClaimsPlanetThenFliesToNext()
    {
        var first = new Planet(new Vector2(-700f, 0f));
        var second = new Planet(new Vector2(1100f, 300f));
        var fighter = NewFighter(Faction.Red, Vector2.Zero);
        var world = NewWorld([first, second], fighter);
        Planet? firstClaimed = null;
        var dockedAtSecond = false;

        Run(world, new FighterAutopilot(), 180f, _ =>
        {
            if (firstClaimed is null)
                firstClaimed = first.IsColonized ? first : second.IsColonized ? second : null;
            dockedAtSecond |= fighter.IsDocked && ReferenceEquals(fighter.DockingTarget, second);
        });

        firstClaimed.Should().BeSameAs(first);
        first.Owner.Should().Be(Faction.Red);
        second.Owner.Should().Be(Faction.Red);
        dockedAtSecond.Should().BeTrue();
        fighter.DockingTarget.Should().BeNull("a fighter that has claimed everything leaves the last planet");
    }

    [Test]
    public void Update_TargetClaimedByAnotherFaction_RePicksNext()
    {
        var nearer = new Planet(new Vector2(-700f, 0f));
        var farther = new Planet(new Vector2(900f, 0f));
        var fighter = NewFighter(Faction.Red, Vector2.Zero);
        var world = NewWorld([nearer, farther], fighter);
        var autopilot = new FighterAutopilot();
        Run(world, autopilot, 3f);
        autopilot.GetTarget(fighter).Should().BeSameAs(nearer);

        nearer.Claim(Faction.Purple);
        Run(world, autopilot, 1f);
        autopilot.GetTarget(fighter).Should().BeSameAs(farther);

        Run(world, autopilot, 120f);
        nearer.Owner.Should().Be(Faction.Purple);
        farther.Owner.Should().Be(Faction.Red);
    }

    [Test]
    public void Update_NoFreePlanet_HoldsPosition()
    {
        var owned = new Planet(Vector2.Zero, owner: Faction.Blue);
        var start = new Vector2(0f, 500f);
        var fighter = NewFighter(Faction.Red, start);
        var world = NewWorld([owned], fighter);

        Run(world, new FighterAutopilot(), 20f);

        Vector2.Distance(fighter.Position, start).Should().BeLessThan(25f);
        fighter.Velocity.Length().Should().BeLessThan(1f);
    }

    [Test]
    public void Update_ZeroHealthFighter_HoldsAndNeverClaims()
    {
        var free = new Planet(Vector2.Zero);
        var start = new Vector2(-300f, 0f);
        var fighter = NewFighter(Faction.Red, start);
        fighter.Health = 0;
        var world = NewWorld([free], fighter);

        Run(world, new FighterAutopilot(), 30f);

        free.Owner.Should().BeNull();
        Vector2.Distance(fighter.Position, start).Should().BeLessThan(25f);
        fighter.Velocity.Length().Should().BeLessThan(1f);
    }

    [Test]
    public void Update_ZeroHealthFighterAlreadyDocking_NeverClaimsAndHealthyFighterStillCan()
    {
        var planet = new Planet(Vector2.Zero);
        var wreck = NewFighter(Faction.Red, new Vector2(0f, -(Planet.Radius + Fighter.Radius) - 2f));
        wreck.Rotation = 0f;
        wreck.Health = 0;
        wreck.BeginDocking(planet, -Vector2.UnitY);
        var healthy = NewFighter(Faction.Purple, new Vector2(400f, 0f));
        var world = NewWorld([planet], wreck, healthy);

        Run(world, new FighterAutopilot(), 120f);

        planet.Owner.Should().Be(Faction.Purple, "a wreck must not claim, and must not block a healthy fighter");
    }

    [Test]
    public void Update_ZeroHealthFighterDockingAtAnotherFactionsUnbasedPlanet_ReleasesItWithoutTakingIt()
    {
        // Planets can now be taken from a faction that has no base there, so a wreck that finished docking
        // would take the planet, and keep taking it every tick because nothing repairs a wreck.
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        var wreck = NewFighter(Faction.Red, new Vector2(0f, -(Planet.Radius + Fighter.Radius) - 2f));
        wreck.Rotation = 0f;
        wreck.Health = 0;
        wreck.BeginDocking(planet, -Vector2.UnitY);
        var world = NewWorld([planet], wreck);

        Run(world, new FighterAutopilot(), 30f);

        planet.Owner.Should().Be(Faction.Blue);
    }

    [Test]
    public void Update_DockedAtOwnedNonTargetPlanet_LeavesAndCarriesOn()
    {
        var owned = new Planet(Vector2.Zero, owner: Faction.Red);
        var free = new Planet(new Vector2(900f, 0f));
        var fighter = NewFighter(Faction.Red, new Vector2(0f, Planet.Radius + Fighter.Radius));
        fighter.Rotation = 180f;
        fighter.BeginDocking(owned, Vector2.UnitY);
        fighter.CompleteDocking();
        var world = NewWorld([owned, free], fighter);
        var autopilot = new FighterAutopilot();

        Run(world, autopilot, 5f);
        fighter.DockingTarget.Should().BeNull();

        var dockedAtFree = false;
        Run(world, autopilot, 120f, _ =>
            dockedAtFree |= fighter.IsDocked && ReferenceEquals(fighter.DockingTarget, free));
        dockedAtFree.Should().BeTrue();
        free.Owner.Should().Be(Faction.Red);
    }

    [Test]
    public void Update_TwoFightersRacingForOnePlanet_AllPlanetsClaimedWithoutDamage()
    {
        var contested = new Planet(new Vector2(0f, -700f));
        var eastern = new Planet(new Vector2(1500f, 300f));
        var western = new Planet(new Vector2(-1500f, 300f));
        var red = NewFighter(Faction.Red, new Vector2(-100f, 0f));
        var purple = NewFighter(Faction.Purple, new Vector2(100f, 0f));
        var world = NewWorld([contested, eastern, western], red, purple);

        Run(world, new FighterAutopilot(), 400f);

        world.Planets.Should().OnlyContain(planet => planet.IsColonized);
        red.Health.Should().Be(Fighter.MaxHealth);
        purple.Health.Should().Be(Fighter.MaxHealth);
    }

    [Test]
    public void Update_TwoFightersDockingAtSameFreePlanet_OneYieldsAndOtherClaims()
    {
        // Both capture the same planet in the same tick, side by side. They overlap each other, so
        // without a tie-break neither docking ever completes and the planet is never claimed.
        var planet = new Planet(Vector2.Zero);
        var other = new Planet(new Vector2(2000f, 0f));
        var red = NewFighter(Faction.Red, new Vector2(-10f, -(Planet.Radius + Fighter.Radius)));
        var purple = NewFighter(Faction.Purple, new Vector2(10f, -(Planet.Radius + Fighter.Radius)));
        red.Rotation = purple.Rotation = 0f;
        red.BeginDocking(planet, -Vector2.UnitY);
        purple.BeginDocking(planet, -Vector2.UnitY);
        var world = NewWorld([planet, other], red, purple);

        Run(world, new FighterAutopilot(), 30f);

        planet.Owner.Should().Be(Faction.Red, "the first fighter in world order keeps a contested planet");
        red.Health.Should().Be(Fighter.MaxHealth);
        purple.Health.Should().Be(Fighter.MaxHealth);
    }

    [Test]
    public void Update_DoesNotTouchPlayerControls()
    {
        var world = NewWorld([new Planet(new Vector2(500f, 0f))], NewFighter(Faction.Red, Vector2.Zero));
        var controls = new ShipControls(0.5f, 0.25f);
        world.Player.Controls = controls;

        new FighterAutopilot().Update(world, Dt);

        world.Player.Controls.Should().Be(controls);
    }
}
