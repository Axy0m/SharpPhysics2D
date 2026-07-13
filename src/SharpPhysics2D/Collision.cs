using System;

namespace SharpPhysics2D;

/// <summary>
/// Narrow-phase collision detection. Each method returns <c>true</c> and fills
/// a <see cref="Manifold"/> when the two bodies overlap. The normal always
/// points from <paramref name="a"/> towards <paramref name="b"/>.
/// </summary>
public static class Collision
{
    /// <summary>Dispatches to the correct test based on the two shapes.</summary>
    public static bool TryCollide(RigidBody a, RigidBody b, out Manifold manifold)
    {
        return (a.Shape, b.Shape) switch
        {
            (ShapeType.Circle, ShapeType.Circle) => CircleCircle(a, b, out manifold),
            (ShapeType.Box, ShapeType.Box) => BoxBox(a, b, out manifold),
            (ShapeType.Circle, ShapeType.Box) => CircleBox(a, b, flipped: false, out manifold),
            (ShapeType.Box, ShapeType.Circle) => CircleBox(b, a, flipped: true, out manifold),
            _ => Miss(out manifold),
        };
    }

    private static bool CircleCircle(RigidBody a, RigidBody b, out Manifold manifold)
    {
        Vector2 delta = b.Position - a.Position;
        float radiusSum = a.Radius + b.Radius;
        float distSq = delta.LengthSquared;

        if (distSq >= radiusSum * radiusSum)
            return Miss(out manifold);

        float dist = MathF.Sqrt(distSq);
        // When the centres coincide, pick an arbitrary but consistent normal.
        Vector2 normal = dist > 1e-8f ? delta / dist : Vector2.UnitY;
        manifold = new Manifold(a, b, normal, radiusSum - dist);
        return true;
    }

    private static bool BoxBox(RigidBody a, RigidBody b, out Manifold manifold)
    {
        Vector2 delta = b.Position - a.Position;
        float overlapX = a.HalfSize.X + b.HalfSize.X - MathF.Abs(delta.X);
        if (overlapX <= 0f) return Miss(out manifold);

        float overlapY = a.HalfSize.Y + b.HalfSize.Y - MathF.Abs(delta.Y);
        if (overlapY <= 0f) return Miss(out manifold);

        // Resolve along the axis of least penetration.
        if (overlapX < overlapY)
        {
            Vector2 normal = new(delta.X < 0f ? -1f : 1f, 0f);
            manifold = new Manifold(a, b, normal, overlapX);
        }
        else
        {
            Vector2 normal = new(0f, delta.Y < 0f ? -1f : 1f);
            manifold = new Manifold(a, b, normal, overlapY);
        }
        return true;
    }

    /// <summary>
    /// Circle vs box. <paramref name="circle"/> and <paramref name="box"/> are
    /// the actual shapes; <paramref name="flipped"/> tells us the original call
    /// order so the emitted normal still points from the first body to the second.
    /// </summary>
    private static bool CircleBox(RigidBody circle, RigidBody box, bool flipped, out Manifold manifold)
    {
        Vector2 delta = circle.Position - box.Position;
        Vector2 closest = Vector2.Clamp(delta, -box.HalfSize, box.HalfSize);

        bool inside = false;
        // If the circle centre is inside the box, clamp it to the nearest face.
        if (delta.Equals(closest))
        {
            inside = true;
            if (MathF.Abs(delta.X) * box.HalfSize.Y > MathF.Abs(delta.Y) * box.HalfSize.X)
                closest = new Vector2(box.HalfSize.X * MathF.Sign(delta.X == 0f ? 1f : delta.X), closest.Y);
            else
                closest = new Vector2(closest.X, box.HalfSize.Y * MathF.Sign(delta.Y == 0f ? 1f : delta.Y));
        }

        Vector2 toCircle = delta - closest;
        float distSq = toCircle.LengthSquared;
        if (!inside && distSq >= circle.Radius * circle.Radius)
            return Miss(out manifold);

        float dist = MathF.Sqrt(distSq);
        Vector2 normalBoxToCircle = dist > 1e-8f
            ? toCircle / dist
            : Vector2.UnitY;
        if (inside) normalBoxToCircle = -normalBoxToCircle;

        float penetration = circle.Radius - dist;
        if (inside) penetration = circle.Radius + dist;

        // normalBoxToCircle points box -> circle. Emit it in original A -> B order.
        Vector2 normal = flipped ? normalBoxToCircle : -normalBoxToCircle;
        RigidBody a = flipped ? box : circle;
        RigidBody b = flipped ? circle : box;
        manifold = new Manifold(a, b, normal, penetration);
        return true;
    }

    private static bool Miss(out Manifold manifold)
    {
        manifold = default;
        return false;
    }
}
