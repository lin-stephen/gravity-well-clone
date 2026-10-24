using System.Numerics;
using GravityWell.Game;
using Raylib_cs;

namespace GravityWell.Rendering;

public sealed class Viewport
{
    public int FramebufferWidth { get; private set; } = GameConstants.DesignWidth;
    public int FramebufferHeight { get; private set; } = GameConstants.DesignHeight;
    public float LogicalWidth { get; private set; } = GameConstants.DesignWidth;
    public float Scale { get; private set; } = 1f;

    public void Resize(int width, int height)
    {
        FramebufferWidth = Math.Max(width, 1);
        FramebufferHeight = Math.Max(height, 1);
        Raylib.SetWindowSize(FramebufferWidth, FramebufferHeight);
        UpdateDimensions();
    }

    public Camera2D CreateScreenCamera()
    {
        UpdateDimensions();
        return new Camera2D
        {
            Offset = Vector2.Zero,
            Target = Vector2.Zero,
            Rotation = 0,
            Zoom = Scale
        };
    }

    public Camera2D CreateWorldCamera(Vector2 target)
    {
        UpdateDimensions();
        return new Camera2D
        {
            Offset = new Vector2(FramebufferWidth / 2f, FramebufferHeight / 2f),
            Target = target,
            Rotation = 0,
            Zoom = Scale
        };
    }

    private void UpdateDimensions()
    {
        Scale = FramebufferHeight / (float)GameConstants.DesignHeight;
        LogicalWidth = FramebufferWidth / Scale;
    }
}
