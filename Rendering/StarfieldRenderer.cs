using System.Numerics;
using GravityWell.Game;
using Raylib_cs;

namespace GravityWell.Rendering;

public sealed class StarfieldRenderer
{
    public void Draw(Vector2 playerPosition, float viewportWidth)
    {
        DrawLayer(playerPosition, viewportWidth, 48, 0.025f, 197, 89, 31, 17, 0.55f,
            new Color(58, 72, 86, 255));
        DrawLayer(playerPosition, viewportWidth, 26, 0.06f, 263, 137, 73, 41, 0.8f,
            new Color(107, 125, 141, 255));
        DrawLayer(playerPosition, viewportWidth, 12, 0.12f, 359, 211, 127, 83, 1.15f,
            new Color(184, 203, 216, 255));
    }

    private static void DrawLayer(
        Vector2 playerPosition,
        float viewportWidth,
        int baseCount,
        float parallax,
        int xStep,
        int yStep,
        int xSeed,
        int ySeed,
        float radius,
        Color color)
    {
        var count = Math.Max(1,
            (int)MathF.Ceiling(baseCount * viewportWidth / GameConstants.DesignWidth));

        for (var i = 0; i < count; i++)
        {
            var baseX = (i * xStep + xSeed) % viewportWidth;
            var baseY = (i * yStep + ySeed) % GameConstants.DesignHeight;
            var x = Wrap(baseX - playerPosition.X * parallax, viewportWidth);
            var y = Wrap(baseY - playerPosition.Y * parallax, GameConstants.DesignHeight);
            Raylib.DrawCircleV(new Vector2(x, y), radius, color);
        }
    }

    private static float Wrap(float value, float length) =>
        (value % length + length) % length;
}
