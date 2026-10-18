using System.Numerics;
using GravityWell.Entities;
using Raylib_cs;

namespace GravityWell.Rendering;

public sealed class FreighterRenderer
{
    public void Draw(Freighter freighter)
    {
        if (freighter.State == FreighterState.Consumed)
            return;

        var forward = freighter.Heading;
        var right = new Vector2(-forward.Y, forward.X);
        var nose = freighter.Position + forward * 15f;
        var tail = freighter.Position - forward * 15f;
        var color = freighter.State == FreighterState.Building
            ? Palette.StructureBuilding
            : Palette.PlayerShip;

        Raylib.DrawLineV(nose, tail, color);
        Raylib.DrawLineV(nose, freighter.Position + right * 7f, color);
        Raylib.DrawLineV(nose, freighter.Position - right * 7f, color);
        Raylib.DrawRectangleLines(
            (int)(tail.X - 4f), (int)(tail.Y - 4f), 8, 8, color);
    }
}
