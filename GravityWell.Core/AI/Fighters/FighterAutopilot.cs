using GravityWell.Core.Entities;
using GravityWell.Core.Physics;
using GravityWell.Core.World;

namespace GravityWell.Core.AI.Fighters;

/// <summary>Flies each AI fighter to the nearest uncolonized planet so that docking there claims it. See <see cref="PeacefulFighterStrategy"/>.</summary>
public sealed class FighterAutopilot
{
    private readonly ShipAutopilot<Fighter> autopilot;

    public FighterAutopilot(DockingSystem? docking = null, PhysicsSystem? physics = null,
        ApproachTuning? tuning = null) =>
        autopilot = new ShipAutopilot<Fighter>(new PeacefulFighterStrategy(
            docking ?? new DockingSystem(), physics ?? new PhysicsSystem(), tuning ?? ApproachTuning.ForFighter));

    public Planet? GetTarget(Fighter fighter) => autopilot.PlanOf(fighter)?.Target;

    public void Update(GameWorld world, float deltaTime) => autopilot.Update(world, deltaTime);
}
