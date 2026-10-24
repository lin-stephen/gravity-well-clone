using GravityWell.Core.Entities;
using GravityWell.Core.World;

namespace GravityWell.Core.Colonization;

public sealed class ColonizationSystem
{
    public void Update(GameWorld world)
    {
        var fighter = world.Player;
        if (fighter.IsDocked && fighter.DockingTarget is Planet planet && !planet.IsColonized)
            planet.Claim(fighter.Faction);
    }
}
