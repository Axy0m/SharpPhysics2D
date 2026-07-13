using System;
using SharpPhysics2D;

// A tiny console demo: drop a bouncing ball and a box onto a static floor,
// stepping at a fixed 60 Hz and drawing an ASCII frame a few times a second.

var world = new World(gravity: new Vector2(0f, -9.81f));

var floor = RigidBody.CreateBox(width: 24f, height: 1f, isStatic: true);
floor.Position = new Vector2(0f, 0.5f);
world.Add(floor);

var ball = RigidBody.CreateCircle(radius: 0.5f, mass: 1f);
ball.Position = new Vector2(-3f, 10f);
ball.Restitution = 0.7f;
world.Add(ball);

var box = RigidBody.CreateBox(width: 1f, height: 1f, mass: 2f);
box.Position = new Vector2(3f, 12f);
box.Restitution = 0.2f;
world.Add(box);

const float dt = 1f / 60f;
const int totalSteps = 360;   // 6 seconds
const int drawEvery = 20;     // ~3 frames per second

Console.WriteLine("SharpPhysics2D demo — dropping a ball (o) and a box (#) onto the floor.\n");

for (int step = 0; step <= totalSteps; step++)
{
    world.Step(dt);

    if (step % drawEvery == 0)
    {
        Console.WriteLine($"t = {step * dt,5:0.00}s   " +
                          $"ball y = {ball.Position.Y,6:0.00}   " +
                          $"box y = {box.Position.Y,6:0.00}");
        DrawFrame(ball, box);
    }
}

Console.WriteLine("Done. Both bodies have come to rest on the floor.");

// Renders a small side-on view (x horizontal, y vertical) of the two bodies.
static void DrawFrame(RigidBody ball, RigidBody box)
{
    const int width = 26;
    const int height = 14;
    const float worldWidth = 24f;   // maps [-12, 12] -> columns
    const float worldHeight = 14f;  // maps [0, 14]   -> rows

    var grid = new char[height, width];
    for (int r = 0; r < height; r++)
        for (int c = 0; c < width; c++)
            grid[r, c] = c == 0 || c == width - 1 ? '|' : ' ';

    // Ground line along the bottom.
    for (int c = 0; c < width; c++)
        grid[height - 1, c] = '=';

    Plot(grid, width, height, worldWidth, worldHeight, ball.Position, 'o');
    Plot(grid, width, height, worldWidth, worldHeight, box.Position, '#');

    for (int r = 0; r < height; r++)
        Console.WriteLine(new string(GetRow(grid, r, width)));
    Console.WriteLine();
}

static void Plot(char[,] grid, int width, int height, float worldWidth, float worldHeight, Vector2 pos, char glyph)
{
    int col = (int)((pos.X + worldWidth / 2f) / worldWidth * (width - 1));
    int row = (int)((worldHeight - pos.Y) / worldHeight * (height - 1));
    if (col >= 0 && col < width && row >= 0 && row < height)
        grid[row, col] = glyph;
}

static char[] GetRow(char[,] grid, int row, int width)
{
    var chars = new char[width];
    for (int c = 0; c < width; c++)
        chars[c] = grid[row, c];
    return chars;
}
