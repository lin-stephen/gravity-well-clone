using GravityWell.Core.Colonization.Policies;
using GravityWell.Core.World;

namespace GravityWell.Core.Colonization;

/// <summary>
/// Runs every freighter. Each kind of job is a policy; this class only fixes their priority and keeps the fleet stocked.
/// </summary>
public sealed class FreighterSystem
{
    /// <summary>
    /// In dispatch priority order: construction picks its idle freighters before supply does, so a freighter holding a
    /// full load is spent on a build site before it is sent to restock an outpost.
    /// </summary>
    private readonly IFreighterPolicy[] policies = [new FreighterConstructionPolicy(), new FreighterSupplyPolicy()];

    private readonly FreighterProduction production = new();

    public void Update(GameWorld world, float deltaTime)
    {
        // Active ships are advanced lowest priority first. Supply reads structure states that a construction site
        // finishing this tick would change, so supply must see the state from before that tick's building.
        for (var i = policies.Length - 1; i >= 0; i--)
        {
            policies[i].UpdateActive(world, deltaTime);
        }

        foreach (var policy in policies)
        {
            policy.DispatchIdle(world);
        }

        production.Update(world, deltaTime);
    }
}
