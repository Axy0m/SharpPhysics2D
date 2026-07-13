using SharpPhysics2D;
using Xunit;

namespace SharpPhysics2D.Tests;

public class Vector2Tests
{
    [Fact]
    public void Arithmetic_Works()
    {
        var a = new Vector2(1f, 2f);
        var b = new Vector2(3f, 4f);

        Assert.Equal(new Vector2(4f, 6f), a + b);
        Assert.Equal(new Vector2(-2f, -2f), a - b);
        Assert.Equal(new Vector2(2f, 4f), a * 2f);
    }

    [Fact]
    public void DotAndCross_AreCorrect()
    {
        var a = new Vector2(1f, 0f);
        var b = new Vector2(0f, 1f);

        Assert.Equal(0f, Vector2.Dot(a, b));
        Assert.Equal(1f, Vector2.Cross(a, b));
    }

    [Fact]
    public void Normalized_ReturnsUnitLength()
    {
        var v = new Vector2(3f, 4f);
        Assert.Equal(1f, v.Normalized().Length, 5);
    }

    [Fact]
    public void Normalized_OfZero_IsZero()
    {
        Assert.Equal(Vector2.Zero, Vector2.Zero.Normalized());
    }
}
