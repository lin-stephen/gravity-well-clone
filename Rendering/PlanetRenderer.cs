using GravityWell.Entities;
using Raylib_cs;

namespace GravityWell.Rendering;

public sealed class PlanetRenderer
{
    private readonly StructureRenderer structures = new();

    public void Draw(Planet planet)
    {
        Raylib.DrawCircleLinesV(planet.Position, Planet.Radius, Palette.ForPlanet(planet));
        structures.Draw(planet);
    }
}
