using System.Numerics;
using GravityWell.Core.Entities;
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
        var front = freighter.Position + forward * 5f;
        var rear = freighter.Position - forward * 13f;
        var frontLeft = front - right * 7f;
        var frontRight = front + right * 7f;
        var rearLeft = rear - right * 7f;
        var rearRight = rear + right * 7f;
        var color = freighter.State == FreighterState.Building
            ? Palette.StructureBuilding
            : Palette.ForFaction(freighter.Faction);

        // A rectangular cargo hull capped by a triangular forward section.
        // All vertices use the ship's axes so the whole outline turns together.
        Raylib.DrawLineV(rearLeft, frontLeft, color);
        Raylib.DrawLineV(frontLeft, frontRight, color);
        Raylib.DrawLineV(frontRight, rearRight, color);
        Raylib.DrawLineV(rearRight, rearLeft, color);
        Raylib.DrawLineV(frontLeft, nose, color);
        Raylib.DrawLineV(nose, frontRight, color);
    }
}
