using System.Drawing.Drawing2D;
using SharpPhysics2D;

namespace SharpPhysics2D.App;

/// <summary>
/// A real-time window that steps a <see cref="World"/> at ~60 Hz and paints the
/// bodies with GDI+. World space is y-up; screen space is y-down, so every
/// draw call runs through <see cref="WorldToScreen"/>.
/// </summary>
public sealed class SimulationForm : Form
{
    // Pixels per world unit. Larger = more zoomed in.
    private const float PixelsPerUnit = 40f;
    private const float FixedDt = 1f / 60f;
    private const float WallThickness = 1f;

    private readonly World _world = new(gravity: new Vector2(0f, -18f));
    private readonly Dictionary<RigidBody, Color> _colors = new();
    private readonly List<RigidBody> _boundaries = new();
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Random _rng = new();

    public SimulationForm()
    {
        Text = "SharpPhysics2D — click to drop bodies";
        ClientSize = new Size(960, 640);
        BackColor = Color.FromArgb(24, 26, 32);
        DoubleBuffered = true;
        StartPosition = FormStartPosition.CenterScreen;

        _timer = new System.Windows.Forms.Timer { Interval = 16 };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        BuildBoundaries();
        // Seed the scene with a small pile so the window isn't empty on launch.
        for (int i = 0; i < 6; i++)
            SpawnRandom(new Vector2(WorldWidth * 0.5f, WorldHeight - 1f - i));
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
        _world.Step(FixedDt);
        CullFallenBodies();
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

    /// <summary>Removes dynamic bodies that have escaped below the view.</summary>
    private void CullFallenBodies()
    {
        for (int i = _world.Bodies.Count - 1; i >= 0; i--)
        {
            RigidBody body = _world.Bodies[i];
            if (!body.IsStatic && body.Position.Y < -5f)
            {
                _colors.Remove(body);
                _world.Remove(body);
            }
        }
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
        int dynamicCount = _world.Bodies.Count - _boundaries.Count;
        string text =
            $"bodies: {dynamicCount}\n" +
            "left click: drop a body\n" +
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
