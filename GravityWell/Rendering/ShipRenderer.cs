using System.Numerics;
using GravityWell.Core.Entities;
using Raylib_cs;

namespace GravityWell.Rendering;

public sealed class ShipRenderer
{
    /// <param name="throttle">0 to 1. The flame grows with it, so a ship that only trims against gravity barely shows one.</param>
    public void Draw(Fighter fighter, float throttle)
    {
        var radians = fighter.Rotation * MathF.PI / 180f;
        Vector2 Rotate(Vector2 point) => new(
            point.X * MathF.Cos(radians) - point.Y * MathF.Sin(radians),
            point.X * MathF.Sin(radians) + point.Y * MathF.Cos(radians));

        var nose = fighter.Position + Rotate(new Vector2(0, -13));
        var left = fighter.Position + Rotate(new Vector2(-9, 10));
        var right = fighter.Position + Rotate(new Vector2(9, 10));
        Raylib.DrawTriangleLines(nose, left, right, Palette.ForFaction(fighter.Faction));

        if (throttle <= 0f) return;

        // The flame tip starts at the hull's base (10) and reaches 20 at full throttle.
        var flame = fighter.Position + Rotate(new Vector2(0, 10f + 10f * Math.Min(throttle, 1f)));
        Raylib.DrawLineV(left, flame, Color.Orange);
        Raylib.DrawLineV(right, flame, Color.Orange);
    }
}
