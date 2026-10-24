using GravityWell.Entities;
using GravityWell.Physics;
using Raylib_cs;

namespace GravityWell.Rendering;

public sealed class PlanetRenderer
{
    private readonly StructureRenderer structures = new();

    public void Draw(Planet planet, Fighter fighter)
    {
        var color = Palette.ForPlanet(planet);
        if (DockingSystem.ShouldHighlight(fighter, planet))
            Raylib.DrawRing(planet.Position, Planet.Radius - 1.5f, Planet.Radius + 1.5f,
                0f, 360f, 96, color);
        else
            Raylib.DrawCircleLinesV(planet.Position, Planet.Radius, color);
        structures.Draw(planet, fighter);
    }
}
