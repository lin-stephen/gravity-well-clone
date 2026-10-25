using GravityWell.Core.Entities;
using GravityWell.Core.World;

namespace GravityWell.Core.Colonization;

public sealed class ColonizationSystem
{
    public void Update(GameWorld world)
    {
        foreach (var fighter in world.Fighters)
        {
            if (fighter is { IsDocked: true, DockingTarget: Planet planet })
            {
                planet.Claim(fighter.Faction);
            }
        }
    }
}
