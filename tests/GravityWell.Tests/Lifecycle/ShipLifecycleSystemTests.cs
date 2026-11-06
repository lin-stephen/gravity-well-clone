using System.Numerics;
using GravityWell.Core.Colonization;
using GravityWell.Core.Entities;
using GravityWell.Core.Input;
using GravityWell.Core.Lifecycle;
using GravityWell.Core.Physics;
using GravityWell.Core.Weapons;
using GravityWell.Core.World;

namespace GravityWell.Tests.Lifecycle;

public class ShipLifecycleSystemTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void LethalBolt_DestroysEitherShipType(bool freighter)
    {
        var enemyFighter = new Fighter { Faction = Faction.Red, Position = new Vector2(200, 0) };
        var enemyFreighter = new Freighter(new Vector2(200, 0), Faction.Red);
        Ship target = freighter ? enemyFreighter : enemyFighter;
        target.Health = 1;
        var world = new GameWorld([], freighter ? [enemyFreighter] : [], freighter ? [] : [enemyFighter]);
        var shooter = world.Player;
        world.Bolts.Add(new Bolt(shooter, Vector2.Zero, new Vector2(500, 0), 2f));

        new WeaponSystem(new PhysicsSystem()).Update(world, default, 0.5f);
        new ShipLifecycleSystem().ResolveDestruction(world);

        Assert.That(target.IsDestroyed, Is.True);
        Assert.That(world.Ships, Does.Not.Contain(target));
        Assert.That(world.Explosions, Has.Count.EqualTo(1));
    }

    [Test]
    public void LethalCollision_DestroysFighter_AndStopsFurtherCollisions()
    {
        var planet = new Planet(Vector2.Zero);
        var world = new GameWorld([planet], []);
        world.Player.Position = new Vector2(0, -Planet.Radius);
        world.Player.Velocity = new Vector2(0, 260);
        world.Player.Health = 1;
        var collisions = new CollisionSystem();
        collisions.Update(world);
        new ShipLifecycleSystem().ResolveDestruction(world);
        Assert.That(world.Player.IsDestroyed, Is.True);
        var position = world.Player.Position;
        collisions.Update(world);
        Assert.That(world.Player.Position, Is.EqualTo(position));
    }

    [Test]
    public void Death_DisablesFighterOnce_AndResetRestoresIt()
    {
        var world = new GameWorld([], []);
        var fighter = world.Player;
        fighter.BeginDocking(new Planet(Vector2.Zero), Vector2.UnitY);
        fighter.CompleteDocking();
        fighter.Health = 0;
        var lifecycle = new ShipLifecycleSystem();

        lifecycle.ResolveDestruction(world);
        lifecycle.ResolveDestruction(world);

        Assert.Multiple(() =>
        {
            Assert.That(fighter.IsDestroyed, Is.True);
            Assert.That(fighter.DockingTarget, Is.Null);
            Assert.That(fighter.Velocity, Is.EqualTo(Vector2.Zero));
            Assert.That(world.Ships, Is.Empty);
            Assert.That(world.Explosions, Has.Count.EqualTo(1));
        });
        var position = fighter.Position;
        new PhysicsSystem().Update(world, new PlayerControls { Thrust = true }, 1f);
        new WeaponSystem(new PhysicsSystem()).Update(world, new PlayerControls { Fire = true }, 1f);
        Assert.That(fighter.Position, Is.EqualTo(position));
        Assert.That(world.Bolts, Is.Empty);

        fighter.Reset();
        Assert.That(world.Ships, Does.Contain(fighter));
        lifecycle.Update(world, Explosion.DurationSeconds);
        Assert.That(world.Explosions, Is.Empty);
    }

    [Test]
    public void DestroyedBuilder_CancelsConstruction_AndCanBeReplaced()
    {
        var home = Home(Vector2.Zero);
        var target = new Planet(new Vector2(1000, 0), owner: Faction.Blue);
        var freighter = new Freighter(target.Position, Faction.Blue);
        var world = new GameWorld([home, target], [freighter]);
        freighter.Dispatch(target, StructureType.Base);
        freighter.BeginDocking(target, Vector2.UnitY);
        freighter.CompleteDocking();
        var site = target.AddStructure(StructureType.Base);
        site.BeginConstruction();
        freighter.BeginBuilding(site);
        freighter.Health = 0;

        new ShipLifecycleSystem().ResolveDestruction(world);
        Assert.Multiple(() =>
        {
            Assert.That(freighter.IsDestroyed, Is.True);
            Assert.That(site.State, Is.EqualTo(StructureState.Destroyed));
            Assert.That(world.Freighters, Is.Empty);
            Assert.That(target.HasStructure(StructureType.Base), Is.False);
        });
        new FreighterSystem().Update(world, 5f);
        Assert.That(world.Freighters, Has.Count.EqualTo(1));
        Assert.That(world.Freighters[0].IsDestroyed, Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Fighter_RespawnsAtLastVisitedShipyard_AfterDelay(bool ai)
    {
        var home = Home(Vector2.Zero);
        var visited = Home(new Vector2(1000, 0));
        var fighter = new Fighter();
        var world = new GameWorld([home, visited], [], ai ? [fighter] : []);
        fighter = ai ? fighter : world.Player;
        var lifecycle = new ShipLifecycleSystem();
        lifecycle.Update(world, 0f);
        fighter.BeginDocking(visited.SpaceDock, -Vector2.UnitY);
        fighter.CompleteDocking();
        // The landing and lethal hit can happen between lifecycle ticks.
        fighter.Health = 0;
        lifecycle.ResolveDestruction(world);

        lifecycle.Update(world, ShipLifecycleSystem.RespawnDelaySeconds - 1);
        Assert.That(fighter.IsDestroyed, Is.True);
        lifecycle.Update(world, 1f);
        Assert.Multiple(() =>
        {
            Assert.That(fighter.IsDestroyed, Is.False);
            Assert.That(fighter.Health, Is.EqualTo(Fighter.MaxHealth));
            Assert.That(fighter.IsDocked, Is.True);
            Assert.That(fighter.DockingTarget, Is.SameAs(visited.SpaceDock));
            Assert.That(fighter.Velocity, Is.EqualTo(Vector2.Zero));
        });
    }

    [Test]
    public void DestroyedRespawnFacility_DoesNotResurrectFighter()
    {
        var home = Home(Vector2.Zero);
        var world = new GameWorld([home], []);
        var lifecycle = new ShipLifecycleSystem();
        lifecycle.Update(world, 0f);
        world.Player.Health = 0;
        lifecycle.ResolveDestruction(world);
        home.Structures.Single(site => site.Type == StructureType.TechCenter).TakeDamage(PlanetStructure.MaxHealth);

        lifecycle.Update(world, 100f);

        Assert.That(world.Player.IsDestroyed, Is.True);
        Assert.That(world.Ships, Is.Empty);
    }

    private static Planet Home(Vector2 position)
    {
        var planet = new Planet(position, owner: Faction.Blue);
        planet.BuildHomeworld();
        return planet;
    }
}
