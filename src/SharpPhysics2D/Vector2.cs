using System;

namespace SharpPhysics2D;

/// <summary>
/// An immutable 2D vector with single-precision components.
/// </summary>
public readonly struct Vector2 : IEquatable<Vector2>
{
    public readonly float X;
    public readonly float Y;

    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    public static Vector2 Zero => new(0f, 0f);
    public static Vector2 UnitX => new(1f, 0f);
    public static Vector2 UnitY => new(0f, 1f);

    public float LengthSquared => X * X + Y * Y;
    public float Length => MathF.Sqrt(LengthSquared);

    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2 operator -(Vector2 a) => new(-a.X, -a.Y);
    public static Vector2 operator *(Vector2 a, float s) => new(a.X * s, a.Y * s);
    public static Vector2 operator *(float s, Vector2 a) => new(a.X * s, a.Y * s);
    public static Vector2 operator /(Vector2 a, float s) => new(a.X / s, a.Y / s);

    public static float Dot(Vector2 a, Vector2 b) => a.X * b.X + a.Y * b.Y;

    /// <summary>The 2D scalar cross product (z-component of the 3D cross).</summary>
    public static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    public float Distance(Vector2 other) => (this - other).Length;

    /// <summary>Returns a unit vector in the same direction, or zero if this is the zero vector.</summary>
    public Vector2 Normalized()
    {
        float len = Length;
        return len > 1e-8f ? this / len : Zero;
    }

    /// <summary>Component-wise clamp between two vectors.</summary>
    public static Vector2 Clamp(Vector2 value, Vector2 min, Vector2 max) =>
        new(Math.Clamp(value.X, min.X, max.X), Math.Clamp(value.Y, min.Y, max.Y));

    public bool Equals(Vector2 other) => X.Equals(other.X) && Y.Equals(other.Y);
    public override bool Equals(object? obj) => obj is Vector2 v && Equals(v);
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override string ToString() => $"({X:0.###}, {Y:0.###})";
}
