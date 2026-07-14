using System.Drawing.Drawing2D;
using SharpPhysics2D;

namespace SharpPhysics2D.App;

/// <summary>
/// A real-time window that steps a <see cref="World"/> at ~60 Hz and paints the
/// bodies with GDI+. World space is y-up; screen space is y-down, so every
/// draw call runs through <see cref="WorldToScreen"/>. One body is a
/// player-controlled box driven by the arrow keys / WASD.
/// </summary>
public sealed class SimulationForm : Form
{
    // Pixels per world unit. Larger = more zoomed in.
    private const float PixelsPerUnit = 40f;
    private const float FixedDt = 1f / 60f;
    private const float WallThickness = 1f;
    private const float PlayerMoveSpeed = 6f;
    private const float PlayerJumpSpeed = 9f;

    private readonly World _world = new(gravity: new Vector2(0f, -18f));
    private readonly Dictionary<RigidBody, Color> _colors = new();
    private readonly List<RigidBody> _boundaries = new();
    private readonly HashSet<Keys> _pressedKeys = new();
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Random _rng = new();

    private readonly Vector2 _gravity = new(0f, -18f);
    private bool _gravityEnabled = true;

    private RigidBody? _player;
    private Vector2 _playerSpawn;

    public SimulationForm()
    {
        Text = "SharpPhysics2D — arrows/WASD to move, click to drop bodies";
        ClientSize = new Size(960, 640);
        BackColor = Color.FromArgb(24, 26, 32);
        DoubleBuffered = true;
        KeyPreview = true;
        StartPosition = FormStartPosition.CenterScreen;

        _timer = new System.Windows.Forms.Timer { Interval = 16 };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        BuildBoundaries();
        _playerSpawn = new Vector2(2f, WorldHeight - 2f);
        _player = SpawnPlayer(_playerSpawn);
        // Seed the scene with a small pile so the window isn't empty on launch.
        for (int i = 0; i < 6; i++)
            SpawnRandom(new Vector2(WorldWidth * 0.65f, WorldHeight - 1f - i));
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (IsHandleCreated)
            BuildBoundaries();
    }

    private float WorldWidth => ClientSize.Width / PixelsPerUnit;
    private float WorldHeight => ClientSize.Height / PixelsPerUnit;

    private void Tick()
    {
        UpdatePlayer();
        _world.Step(FixedDt);
        CullFallenBodies();
        if (_player is not null && _player.Position.Y < -5f)
            RespawnPlayer();
        Invalidate();
    }

    /// <summary>(Re)builds the floor and side walls to match the client size.</summary>
    private void BuildBoundaries()
    {
        foreach (RigidBody wall in _boundaries)
            _world.Remove(wall);
        _boundaries.Clear();

        float w = WorldWidth;
        float h = WorldHeight;
        float t = WallThickness;

        RigidBody floor = RigidBody.CreateBox(w, t, isStatic: true);
        floor.Position = new Vector2(w * 0.5f, t * 0.5f);

        RigidBody left = RigidBody.CreateBox(t, h, isStatic: true);
        left.Position = new Vector2(t * 0.5f, h * 0.5f);

        RigidBody right = RigidBody.CreateBox(t, h, isStatic: true);
        right.Position = new Vector2(w - t * 0.5f, h * 0.5f);

        foreach (RigidBody wall in new[] { floor, left, right })
        {
            _world.Add(wall);
            _boundaries.Add(wall);
            _colors[wall] = Color.FromArgb(60, 64, 74);
        }
    }

    /// <summary>Removes dynamic bodies (other than the player) that have escaped below the view.</summary>
    private void CullFallenBodies()
    {
        for (int i = _world.Bodies.Count - 1; i >= 0; i--)
        {
            RigidBody body = _world.Bodies[i];
            if (body == _player)
                continue;

            if (!body.IsStatic && body.Position.Y < -5f)
            {
                _colors.Remove(body);
                _world.Remove(body);
            }
        }
    }

    private RigidBody SpawnPlayer(Vector2 position)
    {
        RigidBody body = RigidBody.CreateBox(1f, 1f, mass: 1f);
        body.Position = position;
        body.Restitution = 0f;
        body.Friction = 0.5f;

        _world.Add(body);
        _colors[body] = Color.FromArgb(255, 221, 51);
        return body;
    }

    private RigidBody SpawnRandom(Vector2 position)
    {
        bool circle = _rng.Next(2) == 0;
        RigidBody body = circle
            ? RigidBody.CreateCircle(0.35f + (float)_rng.NextDouble() * 0.5f, mass: 1f)
            : RigidBody.CreateBox(
                0.7f + (float)_rng.NextDouble() * 0.8f,
                0.7f + (float)_rng.NextDouble() * 0.8f,
                mass: 1.5f);

        body.Position = position;
        body.Restitution = 0.35f + (float)_rng.NextDouble() * 0.35f;
        body.Friction = 0.4f;

        _world.Add(body);
        _colors[body] = FromHsv(_rng.Next(360), 0.55f, 0.95f);
        return body;
    }

    // --- input -----------------------------------------------------------

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left)
            SpawnRandom(ScreenToWorld(e.Location));
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        _pressedKeys.Add(e.KeyCode);

        switch (e.KeyCode)
        {
            case Keys.Space:
                // A burst of bodies raining from the top edge.
                for (int i = 0; i < 12; i++)
                {
                    float x = 1f + (float)_rng.NextDouble() * (WorldWidth - 2f);
                    SpawnRandom(new Vector2(x, WorldHeight - 1f));
                }
                e.Handled = true;
                break;

            case Keys.C:
                ClearDynamicBodies();
                break;

            case Keys.G:
                _gravityEnabled = !_gravityEnabled;
                _world.Gravity = _gravityEnabled ? _gravity : Vector2.Zero;
                break;

            case Keys.R:
                RespawnPlayer();
                break;
        }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        _pressedKeys.Remove(e.KeyCode);
    }

    /// <summary>Drives the player from held movement keys; called once per tick before stepping the world.</summary>
    private void UpdatePlayer()
    {
        if (_player is null)
            return;

        float direction = 0f;
        if (_pressedKeys.Contains(Keys.Left) || _pressedKeys.Contains(Keys.A))
            direction -= 1f;
        if (_pressedKeys.Contains(Keys.Right) || _pressedKeys.Contains(Keys.D))
            direction += 1f;
        _player.Velocity = new Vector2(direction * PlayerMoveSpeed, _player.Velocity.Y);

        bool jumpHeld = _pressedKeys.Contains(Keys.Up) || _pressedKeys.Contains(Keys.W);
        if (jumpHeld && IsGrounded(_player))
            _player.Velocity = new Vector2(_player.Velocity.X, PlayerJumpSpeed);
    }

    /// <summary>True if something sits directly beneath the body, within a thin skin distance.</summary>
    private bool IsGrounded(RigidBody body, float skin = 0.05f)
    {
        AABB bounds = body.GetBounds();
        var probe = new AABB(
            new Vector2(bounds.Min.X, bounds.Min.Y - skin),
            new Vector2(bounds.Max.X, bounds.Min.Y));

        foreach (RigidBody other in _world.Bodies)
        {
            if (other == body)
                continue;
            if (probe.Overlaps(other.GetBounds()))
                return true;
        }
        return false;
    }

    private void RespawnPlayer()
    {
        if (_player is null)
            return;

        _player.Position = _playerSpawn;
        _player.Velocity = Vector2.Zero;
    }

    private void ClearDynamicBodies()
    {
        for (int i = _world.Bodies.Count - 1; i >= 0; i--)
        {
            RigidBody body = _world.Bodies[i];
            if (body == _player)
                continue;

            if (!body.IsStatic)
            {
                _colors.Remove(body);
                _world.Remove(body);
            }
        }
    }

    // --- rendering -------------------------------------------------------

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        foreach (RigidBody body in _world.Bodies)
            DrawBody(g, body);

        DrawHud(g);
    }

    private void DrawBody(Graphics g, RigidBody body)
    {
        Color color = _colors.TryGetValue(body, out Color c) ? c : Color.Gainsboro;
        using var fill = new SolidBrush(color);
        using var outline = new Pen(Color.FromArgb(200, Color.Black), 1.5f);

        if (body.Shape == ShapeType.Circle)
        {
            PointF center = WorldToScreen(body.Position);
            float r = body.Radius * PixelsPerUnit;
            var rect = new RectangleF(center.X - r, center.Y - r, r * 2f, r * 2f);
            g.FillEllipse(fill, rect);
            g.DrawEllipse(outline, rect);
        }
        else
        {
            float hw = body.HalfSize.X * PixelsPerUnit;
            float hh = body.HalfSize.Y * PixelsPerUnit;
            PointF center = WorldToScreen(body.Position);
            var rect = new RectangleF(center.X - hw, center.Y - hh, hw * 2f, hh * 2f);
            g.FillRectangle(fill, rect);
            g.DrawRectangle(outline, rect.X, rect.Y, rect.Width, rect.Height);
        }
    }

    private void DrawHud(Graphics g)
    {
        int dynamicCount = _world.Bodies.Count - _boundaries.Count - (_player is not null ? 1 : 0);
        string text =
            $"bodies: {dynamicCount}    gravity: {(_gravityEnabled ? "on" : "off")}\n" +
            "left click: drop a body\n" +
            "arrows/WASD: move + jump player   R: respawn player\n" +
            "space: burst   C: clear   G: toggle gravity";
        using var brush = new SolidBrush(Color.FromArgb(210, Color.White));
        using var font = new Font("Consolas", 10f);
        g.DrawString(text, font, brush, new PointF(10f, 8f));
    }

    /// <summary>Converts a y-up world point to a y-down screen point.</summary>
    private PointF WorldToScreen(Vector2 p) =>
        new(p.X * PixelsPerUnit, ClientSize.Height - p.Y * PixelsPerUnit);

    /// <summary>Converts a y-down screen point back to a y-up world point.</summary>
    private Vector2 ScreenToWorld(Point p) =>
        new(p.X / PixelsPerUnit, (ClientSize.Height - p.Y) / PixelsPerUnit);

    private static Color FromHsv(float hue, float sat, float val)
    {
        int hi = (int)(hue / 60f) % 6;
        float f = hue / 60f - (int)(hue / 60f);
        float p = val * (1f - sat);
        float q = val * (1f - f * sat);
        float t = val * (1f - (1f - f) * sat);
        (float r, float g, float b) = hi switch
        {
            0 => (val, t, p),
            1 => (q, val, p),
            2 => (p, val, t),
            3 => (p, q, val),
            4 => (t, p, val),
            _ => (val, p, q),
        };
        return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
    }
}
