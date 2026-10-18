using System.Numerics;
using GravityWell.Entities;
using Raylib_cs;

namespace GravityWell.Rendering;

public sealed class StructureRenderer
{
    public void Draw(Planet planet)
    {
        foreach (var structure in planet.Structures)
        {
            var position = GetPosition(planet, structure.Type);
            var color = GetColor(structure);
            DrawGlyph(structure.Type, position, color);

            if (structure.State == StructureState.Building)
                DrawProgress(position, structure.ConstructionProgress);
        }
    }

    private static Vector2 GetPosition(Planet planet, StructureType type) => type switch
    {
        // The original places the four surface facilities in a compact diamond:
        // Tech Center and Comm Center above, Base central, Colony below.
        StructureType.Base => planet.Position,
        StructureType.Colony => planet.Position + new Vector2(0f, 13f),
        StructureType.TechCenter => planet.Position + new Vector2(-12f, -11f),
        StructureType.CommCenter => planet.Position + new Vector2(12f, -10f),

        // Space Dock and Sensor Array are additions to the Starbase rather than
        // separate orbiting objects, so all three share one orbital anchor.
        StructureType.Starbase or StructureType.SpaceDock or StructureType.SensorArray =>
            planet.Position + new Vector2(0f, -40f),
        _ => planet.Position
    };

    private static Color GetColor(PlanetStructure structure) => structure.State switch
    {
        StructureState.Operational => Palette.PlayerShip,
        StructureState.Building => Palette.StructureBuilding,
        StructureState.Planned => Palette.StructurePlanned,
        _ => Palette.StructureDestroyed
    };

    private static void DrawGlyph(StructureType type, Vector2 position, Color color)
    {
        switch (type)
        {
            case StructureType.Base:
                Raylib.DrawRectangleLines((int)position.X - 5, (int)position.Y - 5, 10, 10, color);
                break;
            case StructureType.Colony:
                Raylib.DrawRectangleLines((int)position.X - 10, (int)position.Y - 4, 20, 8, color);
                break;
            case StructureType.TechCenter:
                Raylib.DrawRectangleLines((int)position.X - 5, (int)position.Y - 4, 10, 8, color);
                Raylib.DrawLineV(position + new Vector2(-3f, -4f), position + new Vector2(3f, -8f), color);
                Raylib.DrawLineV(position + new Vector2(3f, -8f), position + new Vector2(3f, -4f), color);
                break;
            case StructureType.CommCenter:
                Raylib.DrawRectangleLines((int)position.X - 4, (int)position.Y - 4, 8, 9, color);
                Raylib.DrawLineV(position + new Vector2(0f, -4f), position + new Vector2(5f, -10f), color);
                Raylib.DrawLineV(position + new Vector2(2f, -8f), position + new Vector2(6f, -7f), color);
                break;
            case StructureType.Starbase:
                Raylib.DrawLineEx(position + new Vector2(-13f, 0f), position + new Vector2(13f, 0f), 2f, color);
                Raylib.DrawTriangleLines(
                    position + new Vector2(-13f, 0f),
                    position + new Vector2(-8f, -4f),
                    position + new Vector2(-8f, 4f), color);
                Raylib.DrawTriangleLines(
                    position + new Vector2(13f, 0f),
                    position + new Vector2(8f, -4f),
                    position + new Vector2(8f, 4f), color);
                break;
            case StructureType.SpaceDock:
                Raylib.DrawRectangleLines((int)position.X - 8, (int)position.Y - 7, 6, 7, color);
                Raylib.DrawLineV(position + new Vector2(-8f, -7f), position + new Vector2(-3f, -10f), color);
                break;
            case StructureType.SensorArray:
                Raylib.DrawLineV(position + new Vector2(5f, 0f), position + new Vector2(5f, -8f), color);
                Raylib.DrawTriangleLines(
                    position + new Vector2(1f, -8f),
                    position + new Vector2(9f, -8f),
                    position + new Vector2(5f, -4f), color);
                break;
        }
    }

    private static void DrawProgress(Vector2 position, float progress)
    {
        const int width = 14;
        var x = (int)position.X - width / 2;
        var y = (int)position.Y + 8;
        Raylib.DrawRectangleLines(x, y, width, 3, Palette.StructurePlanned);
        Raylib.DrawRectangle(x + 1, y + 1, (int)((width - 2) * progress), 1, Palette.StructureBuilding);
    }
}
