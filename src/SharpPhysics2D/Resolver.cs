using System;

namespace SharpPhysics2D;

/// <summary>
/// Resolves a detected contact by applying an impulse that removes the
/// approaching velocity (plus restitution and friction), then nudges the
/// bodies apart to correct residual penetration.
/// </summary>
public static class Resolver
{
    // Fraction of penetration corrected per step and the slop we allow to
    // remain, which keeps resting stacks from jittering.
    private const float CorrectionPercent = 0.8f;
    private const float PenetrationSlop = 0.005f;

    public static void Resolve(in Manifold m)
    {
        RigidBody a = m.A;
        RigidBody b = m.B;
        float invMassSum = a.InverseMass + b.InverseMass;

        // Two static bodies (or zero total inverse mass) cannot be resolved.
        if (invMassSum <= 0f)
            return;

        Vector2 normal = m.Normal;
        Vector2 relativeVelocity = b.Velocity - a.Velocity;
        float velAlongNormal = Vector2.Dot(relativeVelocity, normal);

        // Already separating along the normal: nothing to do.
        if (velAlongNormal > 0f)
        {
            PositionalCorrection(m, invMassSum);
            return;
        }

        float restitution = MathF.Min(a.Restitution, b.Restitution);
        float j = -(1f + restitution) * velAlongNormal / invMassSum;
        Vector2 normalImpulse = normal * j;

        a.ApplyImpulse(-normalImpulse);
        b.ApplyImpulse(normalImpulse);

        ApplyFriction(a, b, normal, j, invMassSum);
        PositionalCorrection(m, invMassSum);
    }

    private static void ApplyFriction(RigidBody a, RigidBody b, Vector2 normal, float j, float invMassSum)
    {
        Vector2 relativeVelocity = b.Velocity - a.Velocity;
        // Velocity component tangent to the collision normal.
        Vector2 tangent = (relativeVelocity - normal * Vector2.Dot(relativeVelocity, normal)).Normalized();
        if (tangent.Equals(Vector2.Zero))
            return;

        float jt = -Vector2.Dot(relativeVelocity, tangent) / invMassSum;
        // Combined friction coefficient; clamp the friction impulse to the
        // Coulomb cone (|jt| <= mu * j) so friction never adds energy.
        float mu = MathF.Sqrt(a.Friction * b.Friction);
        float maxFriction = j * mu;
        jt = Math.Clamp(jt, -maxFriction, maxFriction);

        Vector2 frictionImpulse = tangent * jt;
        a.ApplyImpulse(-frictionImpulse);
        b.ApplyImpulse(frictionImpulse);
    }

    private static void PositionalCorrection(in Manifold m, float invMassSum)
    {
        float depth = MathF.Max(m.Penetration - PenetrationSlop, 0f);
        if (depth <= 0f)
            return;

        Vector2 correction = m.Normal * (depth / invMassSum * CorrectionPercent);
        m.A.Position -= correction * m.A.InverseMass;
        m.B.Position += correction * m.B.InverseMass;
    }
}
