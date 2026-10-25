using GravityWell.Core.Entities;
using Raylib_cs;

namespace GravityWell.Rendering;

public static class Palette
{
    public static readonly Color UncolonizedPlanet = new(92, 183, 126, 255);
    public static readonly Color BarrenPlanet = new(125, 130, 139, 255);
    public static readonly Color StructureBuilding = new(255, 205, 92, 255);
    public static readonly Color StructurePlanned = new(90, 103, 116, 255);
    public static readonly Color StructureDestroyed = new(116, 54, 54, 255);

    // Yellow leans lime so it stays distinct from the amber StructureBuilding colour.
    public static Color ForFaction(Faction faction) => faction switch
    {
        Faction.Blue => new Color(92, 170, 255, 255),
        Faction.Red => new Color(240, 92, 92, 255),
        Faction.Purple => new Color(176, 124, 240, 255),
        Faction.Yellow => new Color(205, 232, 84, 255),
        _ => throw new ArgumentOutOfRangeException(nameof(faction), faction, null)
    };

    public static Color ForPlanet(Planet planet)
    {
        if (planet.Owner is { } owner)
        {
            return ForFaction(owner);
        }

        return planet.Type == PlanetType.Habitable ? UncolonizedPlanet : BarrenPlanet;
    }
}
