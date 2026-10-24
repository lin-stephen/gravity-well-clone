using GravityWell.Entities;
using GravityWell.Game;
using GravityWell.Input;
using GravityWell.World;
using Raylib_cs;

namespace GravityWell.Rendering;

public sealed class GameRenderer
{
    private readonly Viewport viewport = new();
    private readonly StarfieldRenderer starfield = new();
    private readonly ShipRenderer ships = new();
    private readonly FreighterRenderer freighters = new();
    private readonly PlanetRenderer planets = new();
    private readonly HudRenderer hud = new();
    private readonly RadarRenderer radar = new();

    public void Initialize()
    {
        hud.Initialize();
        radar.Initialize();
    }
    public void Resize(int width, int height) => viewport.Resize(width, height);

    public void Draw(GameWorld world, PlayerControls controls, bool showHelp)
    {
        var screenCamera = viewport.CreateScreenCamera();
        var worldCamera = viewport.CreateWorldCamera(world.Player.Position);

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);

        Raylib.BeginMode2D(screenCamera);
        DrawBackground(world);
        Raylib.EndMode2D();

        Raylib.BeginMode2D(worldCamera);
        DrawWorld(world, controls);
        Raylib.EndMode2D();

        Raylib.BeginMode2D(screenCamera);
        if (showHelp)
            hud.Draw(viewport.LogicalWidth);
        hud.DrawDockingStatus(world.Player);
        radar.Draw(world, viewport.LogicalWidth);
        Raylib.EndMode2D();

        Raylib.EndDrawing();
    }

    private void DrawBackground(GameWorld world)
    {
        Raylib.DrawRectangle(0, 0, (int)MathF.Ceiling(viewport.LogicalWidth),
            GameConstants.DesignHeight, new Color(3, 7, 14, 255));
        starfield.Draw(world.Player.Position, viewport.LogicalWidth);
    }

    private void DrawWorld(GameWorld world, PlayerControls controls)
    {
        foreach (var planet in world.Planets)
            planets.Draw(planet, world.Player);
        foreach (var freighter in world.Freighters)
            freighters.Draw(freighter);
        ships.Draw(world.Player, controls.Thrust);
    }
}
