namespace SharpPhysics2D;

/// <summary>
/// An axis-aligned bounding box, used for cheap broad-phase overlap tests.
/// </summary>
public readonly struct AABB
{
    public readonly Vector2 Min;
    public readonly Vector2 Max;

    public AABB(Vector2 min, Vector2 max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>Builds an AABB from a centre point and half-extents.</summary>
    public static AABB FromCenter(Vector2 center, Vector2 halfExtents) =>
        new(center - halfExtents, center + halfExtents);

    /// <summary>True if this box overlaps <paramref name="other"/> (touching counts).</summary>
    public bool Overlaps(in AABB other) =>
        Min.X <= other.Max.X && Max.X >= other.Min.X &&
        Min.Y <= other.Max.Y && Max.Y >= other.Min.Y;
}
