using GravityWell.Core.Entities;
using GravityWell.Core.Physics;
using GravityWell.Core.World;

namespace GravityWell.Core.AI.Freighters;

/// <summary>Flies every freighter to the planet it was dispatched to. See <see cref="FreighterStrategy"/>.</summary>
public sealed class FreighterAutopilot
{
    private readonly ShipAutopilot<Freighter> autopilot;

    public FreighterAutopilot(DockingSystem? docking = null, PhysicsSystem? physics = null) =>
        autopilot = new ShipAutopilot<Freighter>(
            new FreighterStrategy(docking ?? new DockingSystem(), physics ?? new PhysicsSystem()));

    public FlightPhase GetPhase(Freighter ship) => autopilot.PlanOf(ship)?.Phase ?? FlightPhase.Holding;

    public void Update(GameWorld world, float deltaTime) => autopilot.Update(world, deltaTime);
}
