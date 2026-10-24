using GravityWell.Core.Entities;
using GravityWell.Core.Physics;
using GravityWell.Core.World;

namespace GravityWell.Core.AI.Fighters;

/// <summary>Flies each AI fighter to the nearest uncolonized planet so that docking there claims it. See <see cref="PeacefulFighterStrategy"/>.</summary>
public sealed class FighterAutopilot
{
    private readonly ShipAutopilot<Fighter> autopilot;
    private readonly PeacefulFighterStrategy peaceful;
    private readonly NullFighterStrategy nullStrategy;

    public FighterAutopilot(DockingSystem? docking = null, PhysicsSystem? physics = null,
        ApproachTuning? tuning = null)
    {
        nullStrategy = new NullFighterStrategy();
        peaceful = new PeacefulFighterStrategy(
            docking ?? new DockingSystem(), physics ?? new PhysicsSystem(), tuning ?? ApproachTuning.ForFighter);
        autopilot = new ShipAutopilot<Fighter>(world => world.AiFighters, StrategyFor);
    }

    private IAutopilotStrategy<Fighter> StrategyFor(Fighter fighter) => fighter.Behavior switch
    {
        FighterBehavior.Peaceful => peaceful,
        FighterBehavior.Null => nullStrategy,
        var unknown => throw new ArgumentOutOfRangeException(
            nameof(fighter), unknown, "No strategy is registered for this fighter behavior."),
    };

    public Planet? GetTarget(Fighter fighter) => autopilot.PlanOf(fighter)?.Target;

    public void Update(GameWorld world, float deltaTime) => autopilot.Update(world, deltaTime);
}
