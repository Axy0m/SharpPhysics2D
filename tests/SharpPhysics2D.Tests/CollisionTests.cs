using SharpPhysics2D;
using Xunit;

namespace SharpPhysics2D.Tests;

public class CollisionTests
{
    [Fact]
    public void OverlappingCircles_Collide_WithNormalFromAToB()
    {
        var a = RigidBody.CreateCircle(1f);
        a.Position = new Vector2(0f, 0f);
        var b = RigidBody.CreateCircle(1f);
        b.Position = new Vector2(1.5f, 0f);

        Assert.True(Collision.TryCollide(a, b, out var m));
        Assert.Equal(1f, m.Normal.X, 5);   // points from a towards b (+x)
        Assert.Equal(0.5f, m.Penetration, 5);
    }

    [Fact]
    public void SeparatedCircles_DoNotCollide()
    {
        var a = RigidBody.CreateCircle(1f);
        a.Position = new Vector2(0f, 0f);
        var b = RigidBody.CreateCircle(1f);
        b.Position = new Vector2(5f, 0f);

        Assert.False(Collision.TryCollide(a, b, out _));
    }

    [Fact]
    public void OverlappingBoxes_ResolveAlongLeastPenetrationAxis()
    {
        var a = RigidBody.CreateBox(2f, 2f);
        a.Position = new Vector2(0f, 0f);
        var b = RigidBody.CreateBox(2f, 2f);
        b.Position = new Vector2(0f, 1.5f); // stacked, small vertical overlap

        Assert.True(Collision.TryCollide(a, b, out var m));
        Assert.Equal(1f, m.Normal.Y, 5);   // normal is vertical
        Assert.Equal(0.5f, m.Penetration, 5);
    }

    [Fact]
    public void CircleBox_NormalPointsFromCircleToBox_WhenCircleIsFirst()
    {
        var circle = RigidBody.CreateCircle(1f);
        circle.Position = new Vector2(0f, 0f);
        var box = RigidBody.CreateBox(2f, 2f);
        box.Position = new Vector2(1.5f, 0f);

        Assert.True(Collision.TryCollide(circle, box, out var m));
        Assert.Equal(1f, m.Normal.X, 5);
    }
}
