using System.Numerics;
using GravityWell.Core.Entities;
using Raylib_cs;

namespace GravityWell.Rendering;

public sealed class HudRenderer
{
    private const int FontAtlasSize = 64;
    private Font font;

    public void Initialize() =>
        font = Raylib.LoadFontEx("fonts/Orbitron-Medium.ttf", FontAtlasSize, null, 0);

    public void DrawDockingStatus(Fighter fighter)
    {
        if (fighter.DockingTarget is null)
        {
            return;
        }

        var target = fighter.DockingTarget is SpaceDockBody ? "SPACEDOCK" : "PLANET";
        var status = fighter.IsDocked ? "DOCKED" : "DOCKING";
        Raylib.DrawTextEx(font, $"{status}: {target}  |  UP TO DEPART",
            new Vector2(30, 166), 15, 0.5f, Color.Lime);
        if (!fighter.IsDocked)
        {
            return;
        }

        var repair = fighter.RepairStatus switch
        {
            RepairStatus.Repairing => "REPAIRING",
            RepairStatus.FullyRepaired => "FULLY REPAIRED",
            RepairStatus.AwaitingSupplies => "AWAITING SUPPLIES",
            RepairStatus.Unavailable => "REPAIRS UNAVAILABLE",
            _ => ""
        };
        var inventory = fighter.DockingTarget switch
        {
            Planet planet => planet.BaseResources,
            SpaceDockBody dock => dock.Planet.StarbaseResources,
            _ => null
        };
        var supplies = inventory is null ? "" :
            $"  |  RAW {inventory.RawMaterials}  FINISHED {inventory.FinishedMaterials}";
        Raylib.DrawTextEx(font, $"{repair}  {fighter.Health}/{fighter.HealthCapacity} HP{supplies}",
            new Vector2(30, 189), 15, 0.5f,
            fighter.RepairStatus == RepairStatus.Repairing || fighter.RepairStatus == RepairStatus.FullyRepaired
                ? Color.Lime : Color.Yellow);
    }

    public void Draw(float viewportWidth)
    {
        Raylib.DrawRectangle(16, 16, 260, 127, new Color(6, 18, 31, 220));
        Raylib.DrawRectangleLines(16, 16, 260, 127, new Color(55, 121, 170, 255));
        Raylib.DrawTextEx(font, "GRAVITY WELL", new Vector2(30, 26), 24, 1,
            new Color(106, 191, 255, 255));
        Raylib.DrawTextEx(font, "LEFT / RIGHT  ROTATE", new Vector2(30, 61), 15, 0.5f,
            Color.LightGray);
        Raylib.DrawTextEx(font, "UP  THRUST     HOME  RESET", new Vector2(30, 84), 15, 0.5f,
            Color.LightGray);
        Raylib.DrawTextEx(font, "?  HIDE HELP", new Vector2(30, 107), 15, 0.5f,
            Color.LightGray);
        Raylib.DrawTextEx(font, $"{Raylib.GetFPS()} FPS", new Vector2(viewportWidth - 92, 20),
            15, 0.5f, Color.Lime);
    }
}
