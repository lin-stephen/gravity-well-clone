using GravityWell.Entities;
using GravityWell.World;

namespace GravityWell.Colonization;

public sealed class ColonizationSystem
{
    public void Update(GameWorld world)
    {
        var fighter = world.Player;
        if (fighter.IsDocked && fighter.DockingTarget is Planet planet && !planet.IsColonized)
            planet.Claim(fighter.Faction);
    }
}
