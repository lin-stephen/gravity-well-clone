using System.Numerics;
using GravityWell.Core.Economy;
using GravityWell.Core.Entities;
using GravityWell.Core.Input;
using GravityWell.Core.Physics;
using GravityWell.Core.World;

internal static class RepairChecks
{
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static (GameWorld world, Planet planet, Fighter ship) Setup(bool orbital = false, int materials = 10)
    {
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        planet.AddStructure(StructureType.Base, StructureState.Operational);
        if (orbital)
        {
            planet.AddStructure(StructureType.Starbase, StructureState.Operational);
            planet.AddStructure(StructureType.SpaceDock, StructureState.Operational);
        }
        var world = new GameWorld([planet], []);
        var ship = world.Player;
        ship.Health = 1;
        var target = orbital ? (ICollisionBody)planet.SpaceDock : planet;
        ship.Position = target.Position + new Vector2(0, -(ship.CollisionRadius + target.CollisionRadius));
        ship.Velocity = Vector2.Zero;
        ship.Rotation = 0;
        ship.BeginDocking(target, -Vector2.UnitY);
        ship.CompleteDocking();
        (orbital ? planet.StarbaseResources : planet.BaseResources).AddFinishedMaterials(materials);
        return (world, planet, ship);
    }

    public static void Run()
    {
        var repairs = new RepairSystem();
        var (world, planet, ship) = Setup();
        repairs.Update(world, 1f);
        Check(ship.Health == 1 && planet.BaseResources.FinishedMaterials == 10, "no charge before completed repair");
        repairs.Update(world, 1f);
        Check(ship.Health == 2 && planet.BaseResources.FinishedMaterials == 9, "surface 1 HP per 2 seconds");
        Check(ship.RepairStatus == RepairStatus.Repairing, "repairing status");
        repairs.Update(world, 20f);
        Check(ship.Health == Fighter.MaxHealth && planet.BaseResources.FinishedMaterials == 5, "repairs capped at maximum health");
        repairs.Update(world, 20f);
        Check(planet.BaseResources.FinishedMaterials == 5 && ship.RepairStatus == RepairStatus.FullyRepaired,
            "full health consumes no supplies");
        Console.WriteLine("PASS surface repair rate, cost, and health cap");

        (world, planet, ship) = Setup(orbital: true);
        planet.BaseResources.AddFinishedMaterials(7);
        repairs.Update(world, 1f);
        Check(ship.Health == 2 && planet.StarbaseResources.FinishedMaterials == 9 &&
            planet.BaseResources.FinishedMaterials == 7, "orbital repair rate and separate supplies");
        Console.WriteLine("PASS orbital repair rate and separate inventories");

        (world, planet, ship) = Setup(materials: 1);
        repairs.Update(world, 20f);
        Check(ship.Health == 2 && planet.BaseResources.FinishedMaterials == 0 &&
            ship.RepairStatus == RepairStatus.AwaitingSupplies, "depleted supplies stop repairs");
        repairs.Update(world, 100f);
        planet.BaseResources.AddFinishedMaterials(1);
        repairs.Update(world, 1f);
        Check(ship.Health == 2 && planet.BaseResources.FinishedMaterials == 1, "waiting does not bank free repair time");
        repairs.Update(world, 1f);
        Check(ship.Health == 3 && planet.BaseResources.FinishedMaterials == 0, "repairs resume after replenishment");
        Console.WriteLine("PASS supply exhaustion and replenishment");

        (world, planet, ship) = Setup();
        repairs.Update(world, 1.5f);
        new PhysicsSystem().Update(world, new PlayerControls(false, false, true, false, false), 1f / 60);
        Check(!ship.IsDocked && ship.RepairStatus == RepairStatus.Inactive, "thrust stops repair immediately");
        repairs.Update(world, 10f);
        Check(ship.Health == 1 && planet.BaseResources.FinishedMaterials == 10, "undocked ships consume no supplies");
        ship.BeginDocking(planet, -Vector2.UnitY);
        ship.CompleteDocking();
        repairs.Update(world, 0.5f);
        Check(ship.Health == 1, "new docking session starts a fresh repair interval");
        ship.Reset();
        Check(ship.RepairStatus == RepairStatus.Inactive && ship.RepairProgress == 0, "reset clears repair state");
        Console.WriteLine("PASS undocking and reset cancel repair progress");

        (world, planet, ship) = Setup();
        ship.Undock();
        ship.BeginDocking(planet, -Vector2.UnitY);
        repairs.Update(world, 10f);
        Check(ship.Health == 1 && planet.BaseResources.FinishedMaterials == 10, "docking in progress cannot repair");
        Console.WriteLine("PASS repairs wait for completed docking");

        foreach (var owner in new Faction?[] { null, Faction.Red })
        {
            var hostile = new Planet(Vector2.Zero, owner: owner);
            if (owner.HasValue) hostile.AddStructure(StructureType.Base, StructureState.Operational);
            hostile.BaseResources.AddFinishedMaterials(10);
            world = new GameWorld([hostile], []);
            world.Player.Health = 1;
            world.Player.BeginDocking(hostile, Vector2.UnitY);
            world.Player.CompleteDocking();
            repairs.Update(world, 10f);
            Check(world.Player.Health == 1 && hostile.BaseResources.FinishedMaterials == 10 &&
                world.Player.RepairStatus == RepairStatus.Unavailable, "only friendly facilities repair");
        }
        Console.WriteLine("PASS hostile and unowned worlds cannot repair");

        foreach (var state in new StructureState?[] { null, StructureState.Planned, StructureState.Building, StructureState.Destroyed })
        {
            var undeveloped = new Planet(Vector2.Zero, owner: Faction.Blue);
            if (state.HasValue) undeveloped.AddStructure(StructureType.Base, state.Value);
            world = new GameWorld([undeveloped], []);
            world.Player.Health = 1;
            world.Player.BeginDocking(undeveloped, Vector2.UnitY);
            world.Player.CompleteDocking();
            undeveloped.BaseResources.AddFinishedMaterials(10);
            repairs.Update(world, 10f);
            Check(world.Player.Health == 1 && world.Player.RepairStatus == RepairStatus.Unavailable,
                "repair requires operational facility");
        }
        (world, planet, ship) = Setup(orbital: true);
        ship.Health = 0;
        repairs.Update(world, 10f);
        Check(ship.Health == 0 && planet.StarbaseResources.FinishedMaterials == 10, "no resurrection");
        Console.WriteLine("PASS facility eligibility and zero-health exclusion");

        (world, planet, ship) = Setup(materials: 1);
        var freighter = new Freighter(Vector2.Zero, Faction.Blue) { Health = 1 };
        freighter.BeginDocking(planet, Vector2.UnitY);
        freighter.CompleteDocking();
        world = new GameWorld([planet], [freighter]);
        world.Player.Health = 1;
        world.Player.BeginDocking(planet, -Vector2.UnitY);
        world.Player.CompleteDocking();
        repairs.Update(world, 2f);
        Check(world.Player.Health + freighter.Health == 3 && planet.BaseResources.FinishedMaterials == 0,
            "multiple ships cannot overspend shared materials");
        planet.BaseResources.AddFinishedMaterials(2);
        repairs.Update(world, 2f);
        Check(freighter.Health == 2, "freighters share repair rules");
        Console.WriteLine("PASS freighter repairs and shared inventory spending");

        Production();
        DockingIntegration();
    }

    private static void DockingIntegration()
    {
        var (world, planet, ship) = Setup();
        ship.Undock();
        ship.Position = new Vector2(0, 90);
        ship.Velocity = new Vector2(0, -10);
        ship.Rotation = 180;
        var physics = new PhysicsSystem();
        var docking = new DockingSystem();
        var collisions = new CollisionSystem();
        var economy = new EconomySystem();
        var repairs = new RepairSystem();
        for (var frame = 0; frame < 60 * 30; frame++)
        {
            physics.Update(world, default, 1f / 60);
            docking.Update(world, 1f / 60);
            collisions.Update(world);
            economy.Update(world, 1f / 60);
            repairs.Update(world, 1f / 60);
            if (!ship.IsDocked) Check(ship.Health == 1, "no repairs before landing settles");
        }
        Check(ship.IsDocked && ship.Health == Fighter.MaxHealth && planet.BaseResources.FinishedMaterials == 5,
            "actual landing transitions into resource-funded repair");
        Console.WriteLine("PASS landing-to-repair integration");
    }

    private static void Production()
    {
        var economy = new EconomySystem();
        var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
        planet.AddStructure(StructureType.Base, StructureState.Operational);
        planet.AddStructure(StructureType.Colony, StructureState.Operational);
        planet.AddStructure(StructureType.Starbase, StructureState.Operational);
        var world = new GameWorld([planet], []);
        economy.Update(world, 2f);
        Check(planet.BaseResources.RawMaterials == 1 && planet.BaseResources.FinishedMaterials == 0,
            "colony creates raw materials before refining");
        economy.Update(world, 2f);
        Check(planet.BaseResources.RawMaterials == 1 && planet.BaseResources.FinishedMaterials == 1 &&
            planet.StarbaseResources.FinishedMaterials == 1, "both facilities refine independently");
        economy.Update(world, 1000f);
        Check(planet.BaseResources.RawMaterials == ResourceInventory.RawCapacity &&
            planet.BaseResources.FinishedMaterials == ResourceInventory.FinishedCapacity, "storage caps");
        Console.WriteLine("PASS colony supply, refining, and storage limits");

        var barren = new Planet(Vector2.Zero, PlanetType.Barren, Faction.Blue);
        barren.AddStructure(StructureType.Base, StructureState.Operational);
        barren.BaseResources.AddRawMaterials(2);
        world = new GameWorld([barren], []);
        economy.Update(world, 10f);
        Check(barren.BaseResources.RawMaterials == 0 && barren.BaseResources.FinishedMaterials == 2,
            "barren base refines imported stock without producing raw materials");
        economy.Update(world, 100f);
        Check(barren.BaseResources.FinishedMaterials == 2, "barren stock does not regenerate");
        Console.WriteLine("PASS barren outposts depend on supplied raw materials");

        var unfinished = new Planet(Vector2.Zero, owner: Faction.Blue);
        unfinished.AddStructure(StructureType.Base, StructureState.Building);
        unfinished.BaseResources.AddRawMaterials(2);
        world = new GameWorld([unfinished], []);
        economy.Update(world, 10f);
        Check(unfinished.BaseResources.RawMaterials == 2 && unfinished.BaseResources.FinishedMaterials == 0,
            "unfinished refinery cannot process materials");
        Console.WriteLine("PASS unfinished facilities cannot refine");

        foreach (var dt in new[] { 1f / 30, 1f / 60, 1f / 120, 20f })
        {
            var home = new Planet(Vector2.Zero, owner: Faction.Blue);
            home.BuildHomeworld();
            world = new GameWorld([home], []);
            var repairs = new RepairSystem();
            world.Player.Health = 1;
            world.Player.BeginDocking(home, Vector2.UnitY);
            world.Player.CompleteDocking();
            for (var step = 0; step < (int)MathF.Round(20f / dt); step++)
            {
                economy.Update(world, dt);
                repairs.Update(world, dt);
            }
            Check(world.Player.Health == 6 && home.BaseResources.FinishedMaterials == 15 &&
                home.BaseResources.RawMaterials == 10 && home.StarbaseResources.FinishedMaterials == 20,
                "production and repair rates independent of frame size");
        }
        Console.WriteLine("PASS production and repair rates across frame sizes");
    }
}
