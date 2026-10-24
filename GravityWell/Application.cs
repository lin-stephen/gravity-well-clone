using System.Runtime.InteropServices.JavaScript;
using GravityWell.Game;
using Raylib_cs;

public partial class Application
{
    private static readonly GravityWellGame Game = new();

    public static void Main()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.VSyncHint);
        Raylib.InitWindow(GameConstants.DesignWidth, GameConstants.DesignHeight, "Gravity Well");
        Raylib.SetTargetFPS(60);
        Game.Initialize();
    }

    [JSExport]
    public static void ResizeCanvas(int width, int height) => Game.Resize(width, height);

    [JSExport]
    public static void UpdateFrame()
    {
        var deltaTime = MathF.Min(Raylib.GetFrameTime(), 1f / 30f);
        Game.Tick(deltaTime);
    }
}
