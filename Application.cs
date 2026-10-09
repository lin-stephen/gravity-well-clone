using System.Numerics;
using System.Runtime.InteropServices.JavaScript;
using Raylib_cs;

public partial class Application
{
    private const int ScreenWidth = 1280;
    private const int ScreenHeight = 720;
    private const int FontAtlasSize = 64;
    private const float Gravity = 1_350_000f;
    private const float Thrust = 230f;
    private const float TurnSpeed = 2.8f;

    private static readonly Vector2 Star = new(760, 360);
    private static readonly Vector2 Planet = new(430, 360);
    private static Vector2 shipPosition = new(430, 270);
    private static Vector2 shipVelocity = new(105, 0);
    private static float shipRotation = 90f;
    private static Font hudFont;
    private static int framebufferWidth = ScreenWidth;
    private static int framebufferHeight = ScreenHeight;

    public static void Main()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.VSyncHint);
        Raylib.InitWindow(ScreenWidth, ScreenHeight, "Gravity Well");
        Raylib.SetTargetFPS(60);
        hudFont = Raylib.LoadFontEx("fonts/Orbitron-Medium.ttf", FontAtlasSize, null, 0);
    }

    [JSExport]
    public static void ResizeCanvas(int width, int height)
    {
        framebufferWidth = Math.Max(width, 1);
        framebufferHeight = Math.Max(height, 1);
        Raylib.SetWindowSize(framebufferWidth, framebufferHeight);
    }

    [JSExport]
    public static void UpdateFrame()
    {
        var dt = MathF.Min(Raylib.GetFrameTime(), 1f / 30f);
        Update(dt);
        Draw();
    }

    private static void Update(float dt)
    {
        if (Raylib.IsKeyDown(KeyboardKey.Left)) shipRotation -= TurnSpeed * dt * 60f;
        if (Raylib.IsKeyDown(KeyboardKey.Right)) shipRotation += TurnSpeed * dt * 60f;

        var radians = shipRotation * MathF.PI / 180f;
        var facing = new Vector2(MathF.Sin(radians), -MathF.Cos(radians));
        if (Raylib.IsKeyDown(KeyboardKey.Up)) shipVelocity += facing * Thrust * dt;

        ApplyGravity(Star, Gravity, dt);
        ApplyGravity(Planet, Gravity * 0.08f, dt);
        shipPosition += shipVelocity * dt;

        if (Raylib.IsKeyPressed(KeyboardKey.Home))
        {
            shipPosition = new Vector2(430, 270);
            shipVelocity = new Vector2(105, 0);
            shipRotation = 90f;
        }
    }

    private static void ApplyGravity(Vector2 body, float strength, float dt)
    {
        var offset = body - shipPosition;
        var distanceSquared = MathF.Max(offset.LengthSquared(), 2_500f);
        shipVelocity += Vector2.Normalize(offset) * (strength / distanceSquared) * dt;
    }

    private static void Draw()
    {
        var scale = MathF.Min(
            framebufferWidth / (float)ScreenWidth,
            framebufferHeight / (float)ScreenHeight);
        var viewportWidth = ScreenWidth * scale;
        var viewportHeight = ScreenHeight * scale;
        var camera = new Camera2D
        {
            Offset = new Vector2(
                (framebufferWidth - viewportWidth) / 2f,
                (framebufferHeight - viewportHeight) / 2f),
            Target = Vector2.Zero,
            Rotation = 0,
            Zoom = scale
        };

        Raylib.BeginDrawing();
        Raylib.ClearBackground(Color.Black);
        Raylib.BeginMode2D(camera);

        Raylib.DrawRectangle(0, 0, ScreenWidth, ScreenHeight, new Color(3, 7, 14, 255));

        DrawStars();
        Raylib.DrawCircleLines((int)Star.X, (int)Star.Y, 330, new Color(24, 66, 91, 255));
        Raylib.DrawCircleV(Star, 34, new Color(255, 218, 91, 255));
        Raylib.DrawCircleV(Planet, 22, new Color(92, 183, 126, 255));
        Raylib.DrawCircleLines((int)Planet.X, (int)Planet.Y, 26, new Color(88, 162, 255, 255));

        DrawShip();
        DrawHud();

        Raylib.EndMode2D();
        Raylib.EndDrawing();
    }

    private static void DrawStars()
    {
        for (var i = 0; i < 80; i++)
        {
            var x = (i * 197 + 31) % ScreenWidth;
            var y = (i * 89 + 17) % ScreenHeight;
            Raylib.DrawPixel(x, y, i % 5 == 0 ? Color.LightGray : Color.DarkGray);
        }
    }

    private static void DrawShip()
    {
        var radians = shipRotation * MathF.PI / 180f;
        Vector2 Rotate(Vector2 point) => new(
            point.X * MathF.Cos(radians) - point.Y * MathF.Sin(radians),
            point.X * MathF.Sin(radians) + point.Y * MathF.Cos(radians));

        var nose = shipPosition + Rotate(new Vector2(0, -13));
        var left = shipPosition + Rotate(new Vector2(-9, 10));
        var right = shipPosition + Rotate(new Vector2(9, 10));
        Raylib.DrawTriangleLines(nose, left, right, new Color(92, 170, 255, 255));

        if (Raylib.IsKeyDown(KeyboardKey.Up))
        {
            var flame = shipPosition + Rotate(new Vector2(0, 20));
            Raylib.DrawLineV(left, flame, Color.Orange);
            Raylib.DrawLineV(right, flame, Color.Orange);
        }
    }

    private static void DrawHud()
    {
        Raylib.DrawRectangle(16, 16, 260, 104, new Color(6, 18, 31, 220));
        Raylib.DrawRectangleLines(16, 16, 260, 104, new Color(55, 121, 170, 255));
        Raylib.DrawTextEx(hudFont, "GRAVITY WELL", new Vector2(30, 26), 24, 1,
            new Color(106, 191, 255, 255));
        Raylib.DrawTextEx(hudFont, "LEFT / RIGHT  ROTATE", new Vector2(30, 61), 15, 0.5f,
            Color.LightGray);
        Raylib.DrawTextEx(hudFont, "UP  THRUST     HOME  RESET", new Vector2(30, 84), 15, 0.5f,
            Color.LightGray);
        Raylib.DrawTextEx(hudFont, $"{Raylib.GetFPS()} FPS", new Vector2(ScreenWidth - 92, 20),
            15, 0.5f, Color.Lime);
    }
}
