using System.Numerics;
using Raylib_cs;

namespace GravityWell.Rendering;

public sealed class HudRenderer
{
    private const int FontAtlasSize = 64;
    private Font font;

    public void Initialize() =>
        font = Raylib.LoadFontEx("fonts/Orbitron-Medium.ttf", FontAtlasSize, null, 0);

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
