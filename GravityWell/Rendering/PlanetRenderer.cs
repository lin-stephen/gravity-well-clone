using GravityWell.Core.Entities;
using GravityWell.Core.Physics;
using Raylib_cs;

namespace GravityWell.Rendering;

public sealed class PlanetRenderer
{
    private readonly DockingSystem docking;
    private readonly StructureRenderer structures;

    public PlanetRenderer(DockingSystem? docking = null)
    {
        this.docking = docking ?? new DockingSystem();
        structures = new StructureRenderer(this.docking);
    }

    public void Draw(Planet planet, Fighter fighter)
    {
        var color = Palette.ForPlanet(planet);
        if (docking.ShouldHighlight(fighter, planet))
            Raylib.DrawRing(planet.Position, Planet.Radius - 1.5f, Planet.Radius + 1.5f,
                0f, 360f, 96, color);
        else
            Raylib.DrawCircleLinesV(planet.Position, Planet.Radius, color);
        structures.Draw(planet, fighter);
    }
}
