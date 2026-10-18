using System.Numerics;
using GravityWell.Entities;
using GravityWell.World;

namespace GravityWell.Colonization;

public sealed class ColonizationSystem
{
    // Until real collision detection exists, touching a planet's edge is enough to
    // colonize it. Collision work should replace this check with a proper landing.
    public void Update(GameWorld world)
    {
        var fighter = world.Player;

        foreach (var planet in world.Planets)
        {
            if (planet.IsColonized) continue;
            if (!IsTouching(fighter, planet)) continue;

            planet.Claim(Faction.Blue);
            planet.AddStructure(StructureType.Base);
        }
    }

    private static bool IsTouching(Fighter fighter, Planet planet)
    {
        var touchDistance = Fighter.Radius + Planet.Radius;
        return Vector2.DistanceSquared(fighter.Position, planet.Position) <= touchDistance * touchDistance;
    }
}
