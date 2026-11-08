using GravityWell.Core.World;

namespace GravityWell.Core.Colonization;

/// <summary>
/// One kind of freighter job. <see cref="FreighterSystem"/> owns the ships and runs each policy in priority order, so a
/// policy only decides what its own missions do.
/// </summary>
internal interface IFreighterPolicy
{
    /// <summary>Advances the ships already carrying out this policy's missions.</summary>
    void UpdateActive(GameWorld world, float deltaTime);

    /// <summary>Gives idle freighters a job. Ships claimed here are no longer idle for later policies.</summary>
    void DispatchIdle(GameWorld world);
}
