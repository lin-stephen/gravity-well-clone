using System.Numerics;
using GravityWell.Core.AI;
using GravityWell.Core.AI.Freighters;
using GravityWell.Core.Entities;
using GravityWell.Core.Input;
using GravityWell.Core.Physics;
using GravityWell.Core.World;

static void Check(bool ok, string name) { if (!ok) throw new Exception(name); }
static void Flight(string name, Vector2 start, Vector2 velocity, Planet[]? obstacles = null,
    float dt = 1f / 60f, bool bump = false, bool withDock = false)
{
    var target = new Planet(Vector2.Zero, owner: Faction.Blue);
    if (withDock) target.BuildHomeworld();
    var ship = new Freighter(start, Faction.Blue) { Velocity = velocity, Rotation = 90 };
    ship.Dispatch(target, StructureType.Base);
    var world = new GameWorld((obstacles ?? []).Append(target), [ship]);
    world.Player.Position = new Vector2(10000, 10000);
    var physics = new PhysicsSystem();
    var docking = new DockingSystem();
    var collisions = new CollisionSystem();
    var ai = new FreighterAutopilot();
    for (var step = 0; step < (int)(180 / dt); step++)
    {
        if (bump && step == (int)(4f / dt))
            ship.ApplyCollision(Vector2.Zero, new Vector2(-60, 65));
        ai.Update(world, dt);
        physics.Update(world, default, dt);
        docking.Update(world, dt);
        foreach (var obstacle in RoutePlanner.Obstacles(world))
            if (ship.DockingTarget != obstacle)
                Check(Vector2.Distance(ship.Position, obstacle.Position) >=
                    ship.CollisionRadius + obstacle.CollisionRadius - 0.1f, name + " avoids obstacle contact");
        collisions.Update(world);
        Check(float.IsFinite(ship.Position.X) && float.IsFinite(ship.Velocity.X), name + " finite motion");
        if (!ship.IsDocked) continue;
        Check(ship.DockingTarget == target, name + " right target");
        Check(ship.Health == Freighter.MaxHealth, name + " no collision damage");
        Check(docking.IsRearAligned(ship, target), name + " rear-first landing");
        Check(ship.BeginBuilding(), name + " construction after docking");
        Console.WriteLine($"PASS {name}: docked in {step * dt:F1}s");
        return;
    }
    throw new Exception(name + " did not dock");
}
Flight("clear landing", new Vector2(-500, 0), Vector2.Zero);
Flight("blocked route", new Vector2(-700, 0), Vector2.Zero, [new Planet(new Vector2(-350, 0))]);
Flight("sideways drift", new Vector2(-500, 0), new Vector2(0, 70));

// Two freighters meet head-on and must yield without touching, then land.
{
    var left = new Planet(new Vector2(-500, 0), owner: Faction.Blue);
    var right = new Planet(new Vector2(500, 0), owner: Faction.Blue);
    var a = new Freighter(new Vector2(-300, 0), Faction.Blue) { Rotation = 90, Velocity = new Vector2(65, 0) };
    var b = new Freighter(new Vector2(300, 0), Faction.Blue) { Rotation = -90, Velocity = new Vector2(-65, 0) };
    a.Dispatch(right, StructureType.Base);
    b.Dispatch(left, StructureType.Base);
    var world = new GameWorld([left, right], [a, b]);
    world.Player.Position = new Vector2(10000, 10000);
    var ai = new FreighterAutopilot();
    var physics = new PhysicsSystem();
    var docking = new DockingSystem();
    var collisions = new CollisionSystem();
    var closest = float.PositiveInfinity;
    for (var step = 0; step < 60 * 120 && !(a.IsDocked && b.IsDocked); step++)
    {
        ai.Update(world, 1f / 60);
        physics.Update(world, default, 1f / 60);
        docking.Update(world, 1f / 60);
        closest = MathF.Min(closest, Vector2.Distance(a.Position, b.Position));
        collisions.Update(world);
    }

    Check(closest >= Freighter.Radius * 2, "crossing traffic avoids contact");
    Check(a.IsDocked && b.IsDocked, "crossing traffic completes both landings");
    Check(a.Health == 6 && b.Health == 6, "crossing traffic no damage");
    Console.WriteLine("PASS crossing traffic");
}

// Idle fleets, construction and replacement production use the same game loop.
{
    var world = new GameWorld();
    world.Planets[2].Claim(Faction.Blue);
    var ai = new FreighterAutopilot();
    var physics = new PhysicsSystem();
    var docking = new DockingSystem();
    var collisions = new CollisionSystem();
    var freighters = new GravityWell.Core.Colonization.FreighterSystem();
    for (var step = 0; step < 60 * 180; step++)
    {
        ai.Update(world, 1f / 60);
        physics.Update(world, default, 1f / 60);
        docking.Update(world, 1f / 60);
        collisions.Update(world);
        freighters.Update(world, 1f / 60);
    }
    Check(world.Planets[2].HasOperationalStructure(StructureType.Base), "fleet builds base");
    Check(world.Planets[2].HasOperationalStructure(StructureType.Colony), "fleet builds colony");
    Check(world.Planets[2].HasOperationalStructure(StructureType.Starbase), "fleet builds starbase");
    Check(world.Freighters.All(ship => ship.Health == 6), "fleet avoids damaging collisions");
    Console.WriteLine("PASS fleet construction and replacements");
}

Flight("landing at 30 Hz", new Vector2(-500, 0), Vector2.Zero, dt: 1f / 30);
Flight("landing at 120 Hz", new Vector2(-500, 0), Vector2.Zero, dt: 1f / 120);
Flight("bounce recovery", new Vector2(-500, 0), Vector2.Zero, bump: true);
Flight("orbital dock avoidance", new Vector2(0, -500), Vector2.Zero, withDock: true);

// Player controls and AI use identical movement and docking mechanics.
{
    var planet = new Planet(Vector2.Zero);
    var world = new GameWorld([planet], []);
    var fighter = world.Player;
    var physics = new PhysicsSystem();
    var docking = new DockingSystem();
    fighter.Position = new Vector2(0, 44);
    fighter.Velocity = new Vector2(0, -20);
    fighter.Rotation = 160;
    for (var step = 0; step < 180; step++)
    {
        physics.Update(world, default, 1f / 60);
        docking.Update(world, 1f / 60);
    }
    Check(fighter.IsDocked && Vector2.Dot(fighter.Forward, Vector2.UnitY) > 0.999f, "player landing aligns");
    physics.Update(world, new PlayerControls(false, false, true, false, false), 1f / 60);
    docking.Update(world, 1f / 60);
    Check(fighter.DockingTarget is null && fighter.Velocity.Y > 0, "player thrust undocks");
    physics.Update(world, new PlayerControls(false, false, false, true, false), 1f / 60);
    Check(fighter.DockingTarget is null && !fighter.IsDocked && fighter.ReleasedDockingTarget is null,
        "player reset clears docking");
    Console.WriteLine("PASS shared player movement and docking");
}

for (var i = 0; i < 8; i++)
{
    var angle = i * MathF.Tau / 8;
    var start = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 350;
    Flight($"landing direction {i}", start, Vector2.Zero, dt: 1f / 30);
}

RepairChecks.Run();

// Destruction belongs to the lifecycle system; docking evaluates the approach
// of bodies supplied to it without interpreting their health as lifecycle state.
foreach (var orbital in new[] { false, true })
foreach (var health in new[] { 0, 1, 6 })
{
    var planet = new Planet(Vector2.Zero, owner: Faction.Blue);
    planet.BuildHomeworld();
    planet.Health = 0;
    var world = new GameWorld([planet], []);
    ICollisionBody target = orbital ? planet.SpaceDock : planet;
    var ship = world.Player;
    ship.Health = health;
    ship.Position = target.Position - Vector2.UnitY * (ship.CollisionRadius + target.CollisionRadius);
    ship.Velocity = Vector2.Zero;
    ship.Rotation = 0;
    var docking = new DockingSystem();
    for (var frame = 0; frame < 180; frame++) docking.Update(world, 1f / 60);
    Check(ship.IsDocked && ship.DockingTarget == target, "docking is independent of body health");
    Check(ship.Health == health && planet.Health == 0, "docking does not modify health");
}
Console.WriteLine("PASS docking does not interpret ship or target health");
