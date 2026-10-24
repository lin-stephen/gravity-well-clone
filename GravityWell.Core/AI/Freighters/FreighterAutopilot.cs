using GravityWell.Core.Entities;
using GravityWell.Core.Physics;
using GravityWell.Core.World;

namespace GravityWell.Core.AI.Freighters;

/// <summary>Flies every freighter to the planet it was dispatched to. See <see cref="FreighterStrategy"/>.</summary>
public sealed class FreighterAutopilot
{
    private readonly ShipAutopilot<Freighter> autopilot;

    public FreighterAutopilot(DockingSystem? docking = null, PhysicsSystem? physics = null)
    {
        var strategy = new FreighterStrategy(docking ?? new DockingSystem(), physics ?? new PhysicsSystem());
        autopilot = new ShipAutopilot<Freighter>(
            world => world.Freighters.Where(ship => ship.State != FreighterState.Consumed),
            _ => strategy);
    }

    public FlightPhase GetPhase(Freighter ship) => autopilot.PlanOf(ship)?.Phase ?? FlightPhase.Holding;

    public void Update(GameWorld world, float deltaTime) => autopilot.Update(world, deltaTime);
}
