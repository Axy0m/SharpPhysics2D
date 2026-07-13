using System;
using System.Collections.Generic;

namespace SharpPhysics2D;

/// <summary>
/// The simulation container. Holds the bodies and advances them one fixed
/// time step at a time: integrate, detect collisions (broad then narrow),
/// then resolve them.
/// </summary>
public sealed class World
{
    private readonly List<RigidBody> _bodies = new();

    public Vector2 Gravity { get; set; }

    /// <summary>Collision resolution passes per step; more passes settle stacks.</summary>
    public int Iterations { get; set; } = 4;

    public IReadOnlyList<RigidBody> Bodies => _bodies;

    public World(Vector2 gravity)
    {
        Gravity = gravity;
    }

    public World() : this(new Vector2(0f, -9.81f)) { }

    public RigidBody Add(RigidBody body)
    {
        ArgumentNullException.ThrowIfNull(body);
        _bodies.Add(body);
        return body;
    }

    public bool Remove(RigidBody body) => _bodies.Remove(body);

    /// <summary>Advances the whole simulation by <paramref name="dt"/> seconds.</summary>
    public void Step(float dt)
    {
        if (dt <= 0f)
            throw new ArgumentOutOfRangeException(nameof(dt), "Time step must be positive.");

        foreach (RigidBody body in _bodies)
            body.Integrate(Gravity, dt);

        // Resolve several times so a body pressed between others settles.
        for (int iteration = 0; iteration < Iterations; iteration++)
        {
            foreach (Manifold manifold in FindCollisions())
                Resolver.Resolve(manifold);
        }
    }

    /// <summary>
    /// Broad phase (AABB reject) followed by narrow phase, yielding a manifold
    /// for every overlapping pair of bodies. Static-static pairs are skipped.
    /// </summary>
    private IEnumerable<Manifold> FindCollisions()
    {
        for (int i = 0; i < _bodies.Count; i++)
        {
            RigidBody a = _bodies[i];
            AABB boundsA = a.GetBounds();

            for (int j = i + 1; j < _bodies.Count; j++)
            {
                RigidBody b = _bodies[j];
                if (a.IsStatic && b.IsStatic)
                    continue;

                if (!boundsA.Overlaps(b.GetBounds()))
                    continue;

                if (Collision.TryCollide(a, b, out Manifold manifold))
                    yield return manifold;
            }
        }
    }
}
