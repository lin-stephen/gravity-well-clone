using System.Numerics;
using GravityWell.Core.Entities;

namespace GravityWell.Core.Weapons;

/// <summary>
/// Tests a moving point's path over one frame, as the segment from <c>from</c> to <c>to</c> with parameter 0..1,
/// so a fast bolt cannot skip over a thin target between frames.
/// </summary>
public static class Sweep
{
    /// <summary>
    /// True when the segment overlaps the circle. <paramref name="enter"/> is the first parameter inside, at least 0
    /// (so 0 when the segment starts inside). <paramref name="exit"/> is where the line leaves the circle and is not
    /// clamped, so it exceeds 1 when the segment ends inside.
    /// </summary>
    public static bool SegmentCircle(Vector2 from, Vector2 to, Vector2 center, float radius,
        out float enter, out float exit)
    {
        var direction = to - from;
        var offset = from - center;
        var c = offset.LengthSquared() - radius * radius;
        var a = direction.LengthSquared();
        enter = 0f;
        exit = float.MaxValue;
        // NaN and infinite inputs compare false against everything, so reject them up front rather than report a hit.
        if (!float.IsFinite(a) || !float.IsFinite(c))
        {
            return false;
        }

        if (a <= 0f)
        {
            return c <= 0f;
        }

        var b = 2f * Vector2.Dot(offset, direction);
        var discriminant = b * b - 4f * a * c;
        if (!(discriminant >= 0f))
        {
            return false;
        }

        var root = MathF.Sqrt(discriminant);
        var first = (-b - root) / (2f * a);
        exit = (-b + root) / (2f * a);
        if (exit < 0f || first > 1f)
        {
            return false;
        }

        enter = MathF.Max(0f, first);
        return true;
    }

    /// <summary>True when the segment overlaps the box. <paramref name="enter"/> is the first parameter inside, at least 0.</summary>
    public static bool SegmentBox(Vector2 from, Vector2 to, Aabb box, out float enter)
    {
        var direction = to - from;
        var near = 0f;
        var far = 1f;
        enter = 0f;
        if (!ClipAxis(from.X, direction.X, box.Min.X, box.Max.X, ref near, ref far) ||
            !ClipAxis(from.Y, direction.Y, box.Min.Y, box.Max.Y, ref near, ref far))
        {
            return false;
        }

        enter = near;
        return true;
    }

    private static bool ClipAxis(float start, float delta, float min, float max, ref float near, ref float far)
    {
        if (delta == 0f)
        {
            return start >= min && start <= max;
        }

        var first = (min - start) / delta;
        var second = (max - start) / delta;
        if (first > second)
        {
            (first, second) = (second, first);
        }

        near = MathF.Max(near, first);
        far = MathF.Min(far, second);
        return near <= far;
    }
}
