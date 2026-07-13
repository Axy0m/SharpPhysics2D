namespace SharpPhysics2D;

/// <summary>
/// Describes a detected contact between two bodies: the collision normal
/// (pointing from A to B) and how deeply they overlap.
/// </summary>
public readonly struct Manifold
{
    public readonly RigidBody A;
    public readonly RigidBody B;
    public readonly Vector2 Normal;
    public readonly float Penetration;

    public Manifold(RigidBody a, RigidBody b, Vector2 normal, float penetration)
    {
        A = a;
        B = b;
        Normal = normal;
        Penetration = penetration;
    }
}
