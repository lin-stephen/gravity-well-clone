using System.Numerics;
using GravityWell.Core.Entities;
using GravityWell.Core.Physics;
using Raylib_cs;

namespace GravityWell.Rendering;

public sealed class StructureRenderer
{
    private readonly DockingSystem docking;

    public StructureRenderer(DockingSystem? docking = null)
    {
        this.docking = docking ?? new DockingSystem();
    }

    public void Draw(Planet planet, Fighter fighter)
    {
        foreach (var structure in planet.Structures)
        {
            var position = GetPosition(planet, structure.Type);
            var color = GetColor(planet, structure);
            var dockThickness = structure.Type == StructureType.SpaceDock &&
                structure.State == StructureState.Operational &&
                docking.ShouldHighlight(fighter, planet.SpaceDock) ? 3f : 1f;
            DrawGlyph(structure.Type, position, color, dockThickness);

            if (structure.State == StructureState.Building)
                DrawProgress(position, structure.ConstructionProgress);
        }
    }

    private static Vector2 GetPosition(Planet planet, StructureType type) => type switch
    {
        // The original places the four surface facilities in a compact diamond:
        // Tech Center and Comm Center above, Base central, Colony below.
        StructureType.Base => planet.Position,
        StructureType.Colony => planet.Position + new Vector2(0f, 19f),
        StructureType.TechCenter => planet.Position + new Vector2(-17f, -15f),
        StructureType.CommCenter => planet.Position + new Vector2(14f, -11f),

        // Space Dock and Sensor Array are additions to the Starbase rather than
        // separate orbiting objects, so all three share one orbital anchor.
        StructureType.Starbase or StructureType.SpaceDock or StructureType.SensorArray =>
            planet.Position + new Vector2(0f, -54f),
        _ => planet.Position
    };

    private static Color GetColor(Planet planet, PlanetStructure structure) => structure.State switch
    {
        StructureState.Operational => Palette.ForPlanet(planet),
        StructureState.Building => Palette.StructureBuilding,
        StructureState.Planned => Palette.StructurePlanned,
        _ => Palette.StructureDestroyed
    };

    private static void DrawGlyph(StructureType type, Vector2 position, Color color, float dockThickness)
    {
        switch (type)
        {
            case StructureType.Base:
                Raylib.DrawRectangleLines((int)position.X - 8, (int)position.Y - 8, 16, 16, color);
                break;
            case StructureType.Colony:
                Raylib.DrawRectangleLines((int)position.X - 15, (int)position.Y - 6, 30, 12, color);
                break;
            case StructureType.TechCenter:
                Raylib.DrawRectangleLines((int)position.X - 7, (int)position.Y - 6, 14, 12, color);
                Raylib.DrawLineV(position + new Vector2(-4f, -6f), position + new Vector2(5f, -12f), color);
                Raylib.DrawLineV(position + new Vector2(5f, -12f), position + new Vector2(5f, -6f), color);
                break;
            case StructureType.CommCenter:
                Raylib.DrawRectangleLines((int)position.X - 6, (int)position.Y - 6, 12, 14, color);
                Raylib.DrawLineV(position + new Vector2(0f, -6f), position + new Vector2(8f, -15f), color);
                Raylib.DrawLineV(position + new Vector2(3f, -12f), position + new Vector2(9f, -10f), color);
                break;
            case StructureType.Starbase:
                Raylib.DrawLineEx(position + new Vector2(-25f, 0f), position + new Vector2(25f, 0f), 3f, color);
                Raylib.DrawTriangleLines(
                    position + new Vector2(-25f, 0f),
                    position + new Vector2(-16f, -7f),
                    position + new Vector2(-16f, 7f), color);
                Raylib.DrawTriangleLines(
                    position + new Vector2(25f, 0f),
                    position + new Vector2(16f, -7f),
                    position + new Vector2(16f, 7f), color);
                break;
            case StructureType.SpaceDock:
                Raylib.DrawRectangleLinesEx(
                    new Rectangle(position.X - 15f, position.Y - 12f, 10f, 12f), dockThickness, color);
                Raylib.DrawLineEx(position + new Vector2(-15f, -12f),
                    position + new Vector2(-6f, -17f), dockThickness, color);
                break;
            case StructureType.SensorArray:
                Raylib.DrawLineV(position + new Vector2(10f, 0f), position + new Vector2(10f, -14f), color);
                Raylib.DrawTriangleLines(
                    position + new Vector2(3f, -14f),
                    position + new Vector2(17f, -14f),
                    position + new Vector2(10f, -7f), color);
                break;
        }
    }

    private static void DrawProgress(Vector2 position, float progress)
    {
        const int width = 20;
        var x = (int)position.X - width / 2;
        var y = (int)position.Y + 11;
        Raylib.DrawRectangleLines(x, y, width, 4, Palette.StructurePlanned);
        Raylib.DrawRectangle(x + 1, y + 1, (int)((width - 2) * progress), 2, Palette.StructureBuilding);
    }
}
