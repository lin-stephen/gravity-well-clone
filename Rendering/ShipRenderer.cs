using System.Numerics;
using GravityWell.Entities;
using Raylib_cs;

namespace GravityWell.Rendering;

public sealed class ShipRenderer
{
    public void Draw(Fighter fighter, bool isThrusting)
    {
        var radians = fighter.Rotation * MathF.PI / 180f;
        Vector2 Rotate(Vector2 point) => new(
            point.X * MathF.Cos(radians) - point.Y * MathF.Sin(radians),
            point.X * MathF.Sin(radians) + point.Y * MathF.Cos(radians));

        var nose = fighter.Position + Rotate(new Vector2(0, -13));
        var left = fighter.Position + Rotate(new Vector2(-9, 10));
        var right = fighter.Position + Rotate(new Vector2(9, 10));
        Raylib.DrawTriangleLines(nose, left, right, new Color(92, 170, 255, 255));

        if (!isThrusting) return;

        var flame = fighter.Position + Rotate(new Vector2(0, 20));
        Raylib.DrawLineV(left, flame, Color.Orange);
        Raylib.DrawLineV(right, flame, Color.Orange);
    }
}
