using System.Numerics;
using GravityWell.Entities;
using GravityWell.Game;
using GravityWell.World;
using Raylib_cs;

namespace GravityWell.Rendering;

public sealed class RadarRenderer
{
    private const int FontAtlasSize = 64;
    private const float Radius = 72f;
    private const double CoordinateUpdateIntervalSeconds = 0.2d;
    private Font font;
    private Vector2 displayedPosition;
    private double nextCoordinateUpdateTime;
    private bool hasDisplayedPosition;

    public void Initialize() =>
        font = Raylib.LoadFontEx("fonts/Orbitron-Medium.ttf", FontAtlasSize, null, 0);

    public void Draw(GameWorld world, float viewportWidth)
    {
        var fighter = world.Player;
        var center = new Vector2(viewportWidth - 100f, GameConstants.DesignHeight - 100f);

        var currentTime = Raylib.GetTime();
        if (!hasDisplayedPosition || currentTime >= nextCoordinateUpdateTime)
        {
            displayedPosition = fighter.Position;
            nextCoordinateUpdateTime = currentTime + CoordinateUpdateIntervalSeconds;
            hasDisplayedPosition = true;
        }

        Raylib.DrawCircleV(center, Radius + 10f, new Color(6, 18, 31, 220));
        Raylib.DrawCircleLinesV(center, Radius, new Color(55, 121, 170, 255));
        Raylib.DrawCircleLinesV(center, Radius + 10f, new Color(24, 66, 91, 255));

        DrawObjectMarker(center, fighter.Position, GameWorld.StarPosition, 6f,
            new Color(255, 218, 91, 255));
        DrawObjectMarker(center, fighter.Position, GameWorld.PlanetPosition, 4f,
            new Color(92, 183, 126, 255));
        DrawFighter(center, fighter);

        var coordinates = $"X {displayedPosition.X:0}  Y {displayedPosition.Y:0}";
        var textSize = Raylib.MeasureTextEx(font, coordinates, 14, 0.5f);
        Raylib.DrawTextEx(font, coordinates,
            new Vector2(center.X - textSize.X / 2f, center.Y + Radius + 14f),
            14, 0.5f, Color.LightGray);
    }

    private static void DrawObjectMarker(
        Vector2 center,
        Vector2 fighterPosition,
        Vector2 objectPosition,
        float radius,
        Color color)
    {
        var offset = objectPosition - fighterPosition;
        if (offset.LengthSquared() == 0f)
            return;

        var markerPosition = center + Vector2.Normalize(offset) * Radius;
        Raylib.DrawCircleV(markerPosition, radius, color);
    }

    private static void DrawFighter(Vector2 center, Fighter fighter)
    {
        var radians = fighter.Rotation * MathF.PI / 180f;
        Vector2 Rotate(Vector2 point) => new(
            point.X * MathF.Cos(radians) - point.Y * MathF.Sin(radians),
            point.X * MathF.Sin(radians) + point.Y * MathF.Cos(radians));

        var color = fighter.Health switch
        {
            > Fighter.MaxHealth * 2 / 3 => Color.Lime,
            > Fighter.MaxHealth / 3 => Color.Yellow,
            _ => Color.Red
        };

        var nose = center + Rotate(new Vector2(0, -10));
        var left = center + Rotate(new Vector2(-7, 8));
        var right = center + Rotate(new Vector2(7, 8));
        Raylib.DrawTriangleLines(nose, left, right, color);
    }
}
