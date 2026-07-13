using SharpPhysics2D;
using Xunit;

namespace SharpPhysics2D.Tests;

public class WorldTests
{
    [Fact]
    public void Gravity_AcceleratesADynamicBody()
    {
        var world = new World(new Vector2(0f, -10f));
        var body = world.Add(RigidBody.CreateCircle(0.5f));
        body.Position = new Vector2(0f, 100f);

        world.Step(1f);

        // v = g*t = -10, so after one 1s step the body has moved down 10 units.
        Assert.Equal(-10f, body.Velocity.Y, 4);
        Assert.Equal(90f, body.Position.Y, 4);
    }

    [Fact]
    public void StaticBody_NeverMoves()
    {
        var world = new World(new Vector2(0f, -10f));
        var floor = world.Add(RigidBody.CreateBox(10f, 1f, isStatic: true));
        floor.Position = new Vector2(0f, 0f);

        for (int i = 0; i < 60; i++)
            world.Step(1f / 60f);

        Assert.Equal(Vector2.Zero, floor.Position);
    }

    [Fact]
    public void FallingCircle_ComesToRestOnFloor()
    {
        var world = new World(new Vector2(0f, -9.81f));

        var floor = world.Add(RigidBody.CreateBox(20f, 1f, isStatic: true));
        floor.Position = new Vector2(0f, 0.5f); // top face at y = 1

        var ball = world.Add(RigidBody.CreateCircle(0.5f));
        ball.Position = new Vector2(0f, 8f);
        ball.Restitution = 0.3f;

        for (int i = 0; i < 600; i++)
            world.Step(1f / 60f);

        // Floor top is y = 1, radius 0.5, so the centre rests near y = 1.5.
        Assert.InRange(ball.Position.Y, 1.45f, 1.55f);
        Assert.True(System.MathF.Abs(ball.Velocity.Y) < 0.1f, "ball should be nearly at rest");
    }
}
