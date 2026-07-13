using System;

namespace SharpPhysics2D;

/// <summary>
/// A physical body simulated by the <see cref="World"/>. Bodies are either a
/// circle or an axis-aligned box, and may be static (immovable) or dynamic.
/// </summary>
public sealed class RigidBody
{
    public Vector2 Position;
    public Vector2 Velocity;

    /// <summary>Accumulated force applied this step; cleared after integration.</summary>
    private Vector2 _force;

    public ShapeType Shape { get; }

    /// <summary>Circle radius. Only meaningful when <see cref="Shape"/> is Circle.</summary>
    public float Radius { get; }

    /// <summary>Box half-width and half-height. Only meaningful when Shape is Box.</summary>
    public Vector2 HalfSize { get; }

    /// <summary>Bounciness in [0, 1]; 0 is fully inelastic, 1 conserves speed.</summary>
    public float Restitution { get; set; } = 0.2f;

    /// <summary>Coulomb friction coefficient used during collision resolution.</summary>
    public float Friction { get; set; } = 0.4f;

    public bool IsStatic { get; }

    /// <summary>Body mass. Static bodies report 0 (treated as infinite mass).</summary>
    public float Mass { get; }

    /// <summary>Inverse mass; 0 for static bodies so impulses never move them.</summary>
    public float InverseMass { get; }

    private RigidBody(ShapeType shape, float radius, Vector2 halfSize, float mass, bool isStatic)
    {
        Shape = shape;
        Radius = radius;
        HalfSize = halfSize;
        IsStatic = isStatic;
        Mass = isStatic ? 0f : mass;
        InverseMass = isStatic || mass <= 0f ? 0f : 1f / mass;
    }

    public static RigidBody CreateCircle(float radius, float mass = 1f, bool isStatic = false)
    {
        if (radius <= 0f) throw new ArgumentOutOfRangeException(nameof(radius));
        return new RigidBody(ShapeType.Circle, radius, Vector2.Zero, mass, isStatic);
    }

    public static RigidBody CreateBox(float width, float height, float mass = 1f, bool isStatic = false)
    {
        if (width <= 0f) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0f) throw new ArgumentOutOfRangeException(nameof(height));
        return new RigidBody(ShapeType.Box, 0f, new Vector2(width * 0.5f, height * 0.5f), mass, isStatic);
    }

    /// <summary>Queues a force to be applied on the next integration step.</summary>
    public void ApplyForce(Vector2 force) => _force += force;

    /// <summary>Immediately changes velocity by an impulse (mass-scaled).</summary>
    public void ApplyImpulse(Vector2 impulse) => Velocity += impulse * InverseMass;

    /// <summary>The current world-space bounding box of this body.</summary>
    public AABB GetBounds() => Shape switch
    {
        ShapeType.Circle => AABB.FromCenter(Position, new Vector2(Radius, Radius)),
        ShapeType.Box => AABB.FromCenter(Position, HalfSize),
        _ => throw new InvalidOperationException($"Unknown shape {Shape}"),
    };

    /// <summary>
    /// Advances velocity and position using semi-implicit Euler integration and
    /// clears the accumulated force. Static bodies are left untouched.
    /// </summary>
    internal void Integrate(Vector2 gravity, float dt)
    {
        if (IsStatic)
        {
            _force = Vector2.Zero;
            return;
        }

        Vector2 acceleration = gravity + _force * InverseMass;
        Velocity += acceleration * dt;
        Position += Velocity * dt;
        _force = Vector2.Zero;
    }
}
