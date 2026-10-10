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
    private readonly HudRenderer hud = new();

    public void Initialize() => hud.Initialize();
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

        if (showHelp)
        {
            Raylib.BeginMode2D(screenCamera);
            hud.Draw(viewport.LogicalWidth);
            Raylib.EndMode2D();
        }

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
        Raylib.DrawCircleLines((int)GameWorld.StarPosition.X, (int)GameWorld.StarPosition.Y, 330,
            new Color(24, 66, 91, 255));
        Raylib.DrawCircleV(GameWorld.StarPosition, 34, new Color(255, 218, 91, 255));
        Raylib.DrawCircleV(GameWorld.PlanetPosition, 22, new Color(92, 183, 126, 255));
        Raylib.DrawCircleLines((int)GameWorld.PlanetPosition.X, (int)GameWorld.PlanetPosition.Y, 26,
            new Color(88, 162, 255, 255));
        ships.Draw(world.Player, controls.Thrust);
    }
}
