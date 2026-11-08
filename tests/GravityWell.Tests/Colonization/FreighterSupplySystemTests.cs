using System.Numerics;
using GravityWell.Core.AI.Freighters;
using GravityWell.Core.Colonization;
using GravityWell.Core.Economy;
using GravityWell.Core.Entities;
using GravityWell.Core.Lifecycle;
using GravityWell.Core.Physics;
using GravityWell.Core.World;

namespace GravityWell.Tests.Colonization;

public class FreighterSupplySystemTests
{
    [Test]
    public void DeliveryRequiresArrival_AndReturnsReusableFreighterToColony()
    {
        var (world, home, outpost, ship) = Route();
        var system = new FreighterSupplySystem();
        system.Update(world, 100f);
        Assert.That(ship.Mission, Is.EqualTo(FreighterMission.SupplyBase));
        Assert.That(outpost.BaseResources.RawMaterials, Is.Zero);
        Dock(ship);
        system.Update(world, 0f);
        Assert.Multiple(() =>
        {
            Assert.That(outpost.BaseResources.RawMaterials, Is.EqualTo(Freighter.CargoCapacity));
            Assert.That(ship.RawMaterials, Is.Zero);
            Assert.That(ship.State, Is.EqualTo(FreighterState.Traveling));
            Assert.That(ship.Mission, Is.EqualTo(FreighterMission.LoadCargo));
            Assert.That(ship.TargetPlanet, Is.SameAs(home));
            Assert.That(ship.IsDocked, Is.False);
            Assert.That(world.Freighters, Does.Contain(ship));
        });
    }

    [Test]
    public void LoadingIsTimed_AndOnlyHappensAfterArrival()
    {
        var (world, _, outpost, ship) = Route();
        var system = new FreighterSupplySystem();
        system.Update(world, 0f);
        Dock(ship);
        system.Update(world, 0f);
        system.Update(world, 100f);
        Assert.That(ship.RawMaterials, Is.Zero);
        Dock(ship);
        system.Update(world, 1f);
        Assert.That(ship.RawMaterials, Is.Zero);
        system.Update(world, 1f);
        Assert.That(ship.RawMaterials, Is.EqualTo(1));
        Assert.That(ship.IsDocked, Is.True);
        system.Update(world, 18f);
        Assert.That(ship.RawMaterials, Is.EqualTo(Freighter.CargoCapacity));
        Assert.That(ship.TargetPlanet, Is.SameAs(outpost));
        Assert.That(ship.Mission, Is.EqualTo(FreighterMission.SupplyBase));
    }

    [Test]
    public void PartialDeliveryConservesCargo_AndSuppliesBothStores()
    {
        var (world, _, outpost, ship) = Route();
        outpost.BaseResources.AddFinishedMaterials(18);
        outpost.StarbaseResources.AddFinishedMaterials(17);
        var system = new FreighterSupplySystem();
        system.Update(world, 0f);
        Dock(ship);
        system.Update(world, 0f);
        Assert.Multiple(() =>
        {
            Assert.That(outpost.BaseResources.RawMaterials, Is.EqualTo(2));
            Assert.That(outpost.StarbaseResources.RawMaterials, Is.EqualTo(3));
            Assert.That(ship.RawMaterials, Is.EqualTo(Freighter.CargoCapacity - 5));
            Assert.That(ship.Mission, Is.EqualTo(FreighterMission.LoadCargo));
        });
    }

    [Test]
    public void StarbaseReceivesItsOwnDelivery_WhenBaseAlreadyStocked()
    {
        var (world, _, outpost, ship) = Route();
        outpost.BaseResources.AddFinishedMaterials(20);
        var system = new FreighterSupplySystem();
        system.Update(world, 0f);
        Assert.That(ship.Mission, Is.EqualTo(FreighterMission.SupplyStarbase));
        Dock(ship);
        system.Update(world, 0f);
        Assert.That(outpost.BaseResources.RawMaterials, Is.Zero);
        Assert.That(outpost.StarbaseResources.RawMaterials, Is.EqualTo(Freighter.CargoCapacity));
    }

    [Test]
    public void OnlyOneFreighterIsAssignedPerDestination()
    {
        var (_, home, outpost, first) = Route();
        var other = new Freighter(Vector2.One, Faction.Blue);
        var world = new GameWorld([home, outpost], [first, other]);
        new FreighterSupplySystem().Update(world, 0f);
        Assert.That(first.TargetPlanet, Is.SameAs(outpost));
        Assert.That(other.State, Is.EqualTo(FreighterState.Idle));
    }

    [Test]
    public void LostDestinationIsRecalled_WithoutUnloadingForNewOwner()
    {
        var (world, _, outpost, ship) = Route();
        var system = new FreighterSupplySystem();
        system.Update(world, 0f);
        Dock(ship);
        Structure(outpost, StructureType.Base).TakeDamage(PlanetStructure.MaxHealth);
        outpost.Claim(Faction.Red);
        system.Update(world, 0f);
        Assert.That(outpost.BaseResources.RawMaterials, Is.Zero);
        Assert.That(ship.RawMaterials, Is.EqualTo(Freighter.CargoCapacity));
        Assert.That(ship.TargetPlanet, Is.Null);
        Assert.That(ship.IsDocked, Is.False);
    }

    [Test]
    public void ColonyLostDuringLoading_StopsProducingCargo()
    {
        var (world, home, _, ship) = Route();
        var system = new FreighterSupplySystem();
        system.Update(world, 0f);
        Dock(ship);
        system.Update(world, 0f);
        Dock(ship);
        Structure(home, StructureType.Colony).TakeDamage(PlanetStructure.MaxHealth);
        system.Update(world, 100f);
        Assert.That(ship.RawMaterials, Is.Zero);
        Assert.That(ship.State, Is.EqualTo(FreighterState.Idle));
        Assert.That(ship.IsDocked, Is.False);
    }

    [Test]
    public void ReloadedFreighterGetsConstructionBeforeAnotherDelivery()
    {
        var (_, home, outpost, ship) = Route();
        var supply = new FreighterSupplySystem();
        var route = new GameWorld([home, outpost], [ship]);
        supply.Update(route, 0f);
        Dock(ship);
        supply.Update(route, 0f);
        Dock(ship);
        var expansion = new Planet(new Vector2(1200, 0), owner: Faction.Blue);
        var world = new GameWorld([home, outpost, expansion], [ship]);
        new FreighterSystem().Update(world, 20f);
        Assert.That(ship.RawMaterials, Is.EqualTo(Freighter.CargoCapacity));
        Assert.That(ship.Mission, Is.EqualTo(FreighterMission.Construction));
        Assert.That(ship.TargetPlanet, Is.SameAs(expansion));
    }

    [Test]
    public void DestroyedCargoShipDoesNotDeliver_AndReplacementCanTakeItsRoute()
    {
        var (world, home, outpost, ship) = Route();
        var system = new FreighterSupplySystem();
        system.Update(world, 0f);
        Dock(ship);
        ship.Health = 0;
        new ShipLifecycleSystem().ResolveDestruction(world);
        system.Update(world, 100f);
        Assert.That(outpost.BaseResources.RawMaterials, Is.Zero);
        Assert.That(world.Freighters, Is.Empty);
        world.AddFreighter(home, Faction.Blue);
        system.Update(world, 0f);
        Assert.That(world.Freighters[0].TargetPlanet, Is.SameAs(outpost));
    }

    [Test]
    public void FullyStockedOrLocallySuppliedWorldDoesNotReceiveDeliveries()
    {
        var (world, _, outpost, ship) = Route();
        outpost.BaseResources.AddFinishedMaterials(20);
        outpost.StarbaseResources.AddRawMaterials(20);
        new FreighterSupplySystem().Update(world, 0f);
        Assert.That(ship.State, Is.EqualTo(FreighterState.Idle));

        var colony = new Planet(new Vector2(1000, 0), owner: Faction.Blue);
        colony.AddStructure(StructureType.Base, StructureState.Operational);
        colony.AddStructure(StructureType.Colony, StructureState.Operational);
        new FreighterSupplySystem().Update(new GameWorld([colony], [ship]), 0f);
        Assert.That(ship.State, Is.EqualTo(FreighterState.Idle));
    }

    [Test]
    public void EmptyFreighterChoosesNearestFriendlyOperationalColony()
    {
        var (world, home, _, ship) = Route();
        var system = new FreighterSupplySystem();
        system.Update(world, 0f);
        Dock(ship);
        system.Update(world, 0f);
        ship.Recall();
        var enemy = new Planet(ship.Position, owner: Faction.Red);
        enemy.BuildHomeworld();
        var near = new Planet(new Vector2(500, 0), owner: Faction.Blue);
        near.BuildHomeworld();
        var far = new Planet(new Vector2(2000, 0), owner: Faction.Blue);
        far.BuildHomeworld();
        system.Update(new GameWorld([enemy, home, far, near], [ship]), 0f);
        Assert.That(ship.TargetPlanet, Is.SameAs(near));
        Assert.That(ship.Mission, Is.EqualTo(FreighterMission.LoadCargo));
    }

    [Test]
    public void FlyingCargoRouteLetsBarrenWorldBuildTechAndCommCenters()
    {
        var (world, _, outpost, ship) = Route();
        world.Player.Position = new Vector2(-5000, -5000);
        var supply = new FreighterSupplySystem();
        var physics = new PhysicsSystem();
        var docking = new DockingSystem();
        var collisions = new CollisionSystem();
        var lifecycle = new ShipLifecycleSystem();
        var pilot = new FreighterAutopilot(docking, physics);
        var economy = new EconomySystem();
        var construction = new BaseConstructionSystem();
        const float dt = 1f / 60;
        var reloaded = false;
        for (var frame = 0; frame < 240 * 60; frame++)
        {
            pilot.Update(world, dt);
            physics.Update(world, default, dt);
            docking.Update(world, dt);
            collisions.Update(world);
            lifecycle.ResolveDestruction(world);
            supply.Update(world, dt);
            economy.Update(world, dt);
            construction.Update(world, dt);
            reloaded |= ship.Mission == FreighterMission.LoadCargo && ship.RawMaterials > 0 && ship.IsDocked;
        }
        Assert.Multiple(() =>
        {
            Assert.That(outpost.HasOperationalStructure(StructureType.TechCenter), Is.True);
            Assert.That(outpost.HasOperationalStructure(StructureType.CommCenter), Is.True);
            Assert.That(reloaded, Is.True);
            Assert.That(outpost.StarbaseResources.RawMaterials + outpost.StarbaseResources.FinishedMaterials, Is.GreaterThan(0));
            Assert.That(ship.State, Is.Not.EqualTo(FreighterState.Consumed));
        });
    }

    private static PlanetStructure Structure(Planet planet, StructureType type) =>
        planet.Structures.Single(site => site.Type == type);

    private static void Dock(Freighter ship)
    {
        var target = ship.TargetPlanet!;
        ship.Position = target.Position + Vector2.UnitY * (Planet.Radius + Freighter.Radius);
        ship.Velocity = Vector2.Zero;
        ship.BeginDocking(target, Vector2.UnitY);
        ship.CompleteDocking();
    }

    private static (GameWorld World, Planet Home, Planet Outpost, Freighter Ship) Route()
    {
        var home = new Planet(Vector2.Zero, owner: Faction.Blue);
        home.BuildHomeworld();
        var outpost = new Planet(new Vector2(600, 0), PlanetType.Barren, Faction.Blue);
        outpost.AddStructure(StructureType.Base, StructureState.Operational);
        outpost.AddStructure(StructureType.Starbase, StructureState.Operational);
        var ship = new Freighter(new Vector2(0, 100), Faction.Blue) { Rotation = 180 };
        return (new GameWorld([home, outpost], [ship]), home, outpost, ship);
    }
}
