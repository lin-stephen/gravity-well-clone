using GravityWell.Entities;
using Raylib_cs;

namespace GravityWell.Rendering;

public sealed class PlanetRenderer
{
    private readonly StructureRenderer structures = new();

    public void Draw(Planet planet)
    {
        Raylib.DrawCircleLinesV(planet.Position, Planet.Radius, GetColor(planet));
        structures.Draw(planet);
    }

    private static Color GetColor(Planet planet)
    {
        if (planet.IsColonized)
            return Palette.PlayerShip;

        return planet.Type == PlanetType.Habitable
            ? Palette.UncolonizedPlanet
            : Palette.BarrenPlanet;
    }
}
