using System.Numerics;
using GravityWell.Core.Entities;
using Raylib_cs;

namespace GravityWell.Rendering;

/// <summary>Draws cannon bolts and the flash where one hit. The weapon system owns their state and timing.</summary>
public sealed class BoltRenderer
{
    private const float BoltLength = 12f;
    private readonly float impactDuration;

    public BoltRenderer(float impactDuration)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(impactDuration);
        this.impactDuration = impactDuration;
    }

    public void DrawBolt(Bolt bolt)
    {
        if (bolt.Velocity.LengthSquared() <= 0f)
        {
            return;
        }

        var tail = bolt.Position - Vector2.Normalize(bolt.Velocity) * BoltLength;
        Raylib.DrawLineEx(tail, bolt.Position, 2f, Palette.ForFaction(bolt.Faction));
    }

    /// <summary>A ring that grows and fades over the impact's life, around a bright core.</summary>
    public void DrawImpact(Impact impact)
    {
        var progress = Math.Clamp(impact.Age / impactDuration, 0f, 1f);
        var fade = Raylib.Fade(Palette.Impact, 1f - progress);
        Raylib.DrawCircleV(impact.Position, 3f * (1f - progress) + 1f, fade);
        Raylib.DrawCircleLinesV(impact.Position, 5f + 15f * progress, fade);
    }
}
