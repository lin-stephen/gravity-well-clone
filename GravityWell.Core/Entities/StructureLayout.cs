using System.Numerics;

namespace GravityWell.Core.Entities;

/// <summary>An axis-aligned box in world coordinates.</summary>
public readonly record struct Aabb(Vector2 Min, Vector2 Max);

/// <summary>
/// Where each structure sits on its planet and how much room its glyph takes. The renderer draws from this
/// and weapons hit-test against it, so what the player sees is what gets hit.
/// </summary>
public static class StructureLayout
{
    public static Vector2 GetAnchor(Planet planet, StructureType type) => type switch
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

    /// <summary>
    /// The box around the glyph the renderer draws for <paramref name="type"/>. It covers the glyph's nominal
    /// geometry, not the stroke width, so a highlighted dock's thick line can poke about a pixel outside it.
    /// </summary>
    public static Aabb GetBounds(Planet planet, StructureType type)
    {
        var anchor = GetAnchor(planet, type);
        var (min, max) = type switch
        {
            StructureType.Base => (new Vector2(-8f, -8f), new Vector2(8f, 8f)),
            StructureType.Colony => (new Vector2(-15f, -6f), new Vector2(15f, 6f)),
            StructureType.TechCenter => (new Vector2(-7f, -12f), new Vector2(7f, 6f)),
            StructureType.CommCenter => (new Vector2(-6f, -15f), new Vector2(9f, 8f)),
            StructureType.Starbase => (new Vector2(-25f, -7f), new Vector2(25f, 7f)),
            StructureType.SpaceDock => (new Vector2(-15f, -17f), new Vector2(-5f, 0f)),
            StructureType.SensorArray => (new Vector2(3f, -14f), new Vector2(17f, 0f)),
            _ => (Vector2.Zero, Vector2.Zero)
        };
        return new Aabb(anchor + min, anchor + max);
    }
}
