# SharpPhysics2D

A small, dependency-free **2D rigid-body physics engine** written in pure C# (.NET 10).

It is built for learning and for embedding in your own game loop — no Unity, no MonoGame, no native bindings. Just a class library you can reference and step forward in time.

## Features

- 2D vector math (`Vector2`)
- Rigid bodies with mass, velocity, restitution and friction
- Circle and box (oriented-less AABB) colliders
- Semi-implicit Euler integration with global gravity
- Collision detection (circle/circle, circle/box, box/box)
- Impulse-based collision resolution with positional correction
- Static and dynamic bodies

## Project layout

```
src/SharpPhysics2D        The engine (class library)
samples/SharpPhysics2D.Demo   A console demo that drops bodies under gravity
tests/SharpPhysics2D.Tests    xUnit tests
```

## Quick start

```csharp
using SharpPhysics2D;

var world = new World(gravity: new Vector2(0f, -9.81f));

var ground = RigidBody.CreateBox(width: 20f, height: 1f, isStatic: true);
ground.Position = new Vector2(0f, 0f);
world.Add(ground);

var ball = RigidBody.CreateCircle(radius: 0.5f, mass: 1f);
ball.Position = new Vector2(0f, 5f);
ball.Restitution = 0.6f;
world.Add(ball);

const float dt = 1f / 60f;
for (int i = 0; i < 300; i++)
    world.Step(dt);
```

## Build & test

```bash
dotnet build
dotnet test
dotnet run --project samples/SharpPhysics2D.Demo
```

## License

MIT
