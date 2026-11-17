using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace RecycloTray;

/// <summary>
/// Icono plano (2D) con silueta en V: cuerpo acampanado, sin tapa ni asa.
/// Contorno con una costilla cuando está vacía; silueta maciza cuando está llena.
/// </summary>
internal static class IconFactory
{
    public static readonly Color Amber = Color.FromArgb(0xF2, 0xA3, 0x3C);
    public static readonly Color InkDark = Color.FromArgb(0xF5, 0xF5, 0xF5);
    public static readonly Color InkLight = Color.FromArgb(0x1A, 0x1A, 0x1A);

    private static readonly int[] Sizes = { 16, 20, 24, 32, 48 };

    public static Icon Build(bool full, bool lightTheme)
    {
        Color ink = lightTheme ? InkLight : InkDark;
        var bitmaps = new List<Bitmap>(Sizes.Length);
        try
        {
            foreach (int s in Sizes)
                bitmaps.Add(Draw(s, full, ink));

            using var icon = IcoWriter.Pack(bitmaps);
            Size system = SystemInformation.SmallIconSize;
            using var ms = new MemoryStream();
            icon.Save(ms);
            ms.Position = 0;
            return new Icon(ms, new Size(system.Width, system.Height));
        }
        finally
        {
            foreach (var b in bitmaps) b.Dispose();
        }
    }

    private sealed record Geo(
        PointF[] Body,
        float Rib1X, float Rib2X, float RibTop, float RibBottom);

    private static Geo BuildGeo(float u) => new(
        // V marcada: 10.8u arriba, 5.0u abajo
        Body: new[]
        {
            new PointF(2.6f * u, 1.6f * u),
            new PointF(13.4f * u, 1.6f * u),
            new PointF(10.5f * u, 14.4f * u),
            new PointF(5.5f * u, 14.4f * u),
        },
        Rib1X: 6.2f * u,
        Rib2X: 9.8f * u,
        RibTop: 4.4f * u,
        RibBottom: 11.4f * u);

    private static float Snap(float v, bool forStroke) =>
        forStroke ? MathF.Round(v - 0.5f) + 0.5f : MathF.Round(v);

    private static Geo SnapGeo(Geo geo, bool forStroke)
    {
        float S(float v) => Snap(v, forStroke);

        return geo with
        {
            Body = geo.Body.Select(p => new PointF(S(p.X), S(p.Y))).ToArray(),
            Rib1X = S(geo.Rib1X),
            Rib2X = S(geo.Rib2X),
            RibTop = S(geo.RibTop),
            RibBottom = S(geo.RibBottom),
        };
    }

    public static Bitmap Draw(int size, bool full, Color ink)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.Clear(Color.Transparent);

        float u = size / 16f;
        bool tiny = size <= 20;
        float stroke = tiny ? 1f : 1.2f * u;
        float radius = tiny ? 0.9f : 0.9f * u;

        var geo = BuildGeo(u);
        if (tiny)
            geo = SnapGeo(geo, forStroke: !full);

        using var pen = new Pen(ink, stroke) { LineJoin = LineJoin.Round };
        using var brush = new SolidBrush(ink);
        using var bodyPath = RoundedPolygon(geo.Body, radius);

        if (full)
            g.FillPath(brush, bodyPath);

        g.DrawPath(pen, bodyPath);

        // Una costilla a 16/20 px; dos en tamaños mayores
        if (size >= 24)
        {
            using var ribPen = new Pen(ink, stroke * 0.9f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(ribPen, geo.Rib1X, geo.RibTop, geo.Rib1X, geo.RibBottom);
            g.DrawLine(ribPen, geo.Rib2X, geo.RibTop, geo.Rib2X, geo.RibBottom);
        }
        else if (!full)
        {
            float cx = Snap(8.0f * u, forStroke: true);
            using var ribPen = new Pen(ink, 1f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
            g.DrawLine(ribPen, cx, geo.RibTop, cx, geo.RibBottom);
        }

        return bmp;
    }

    private static GraphicsPath RoundedPolygon(PointF[] pts, float radius)
    {
        int n = pts.Length;
        var path = new GraphicsPath();
        var firstA = PointF.Empty;
        var lastB = PointF.Empty;

        for (int i = 0; i < n; i++)
        {
            PointF p = pts[i];
            PointF prev = pts[(i - 1 + n) % n];
            PointF next = pts[(i + 1) % n];

            PointF a = PointTowards(p, prev, radius);
            PointF b = PointTowards(p, next, radius);

            if (i == 0)
            {
                path.StartFigure();
                firstA = a;
            }
            else
            {
                path.AddLine(lastB, a);
            }

            var c1 = new PointF(a.X + 2f / 3f * (p.X - a.X), a.Y + 2f / 3f * (p.Y - a.Y));
            var c2 = new PointF(b.X + 2f / 3f * (p.X - b.X), b.Y + 2f / 3f * (p.Y - b.Y));
            path.AddBezier(a, c1, c2, b);
            lastB = b;
        }

        path.AddLine(lastB, firstA);
        path.CloseFigure();
        return path;
    }

    private static PointF PointTowards(PointF from, PointF to, float distance)
    {
        float dx = to.X - from.X;
        float dy = to.Y - from.Y;
        float len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 0.0001f) return from;
        return new PointF(from.X + dx * distance / len, from.Y + dy * distance / len);
    }

    /// <summary>Icono del ejecutable: la misma V en ámbar.</summary>
    public static Bitmap DrawAppIcon(int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.Clear(Color.Transparent);

        float u = size / 16f;
        var geo = BuildGeo(u);
        var espresso = Color.FromArgb(0x4A, 0x32, 0x14);

        using var amberBrush = new SolidBrush(Amber);
        using var pen = new Pen(espresso, MathF.Max(1f, 0.9f * u)) { LineJoin = LineJoin.Round };
        using var bodyPath = RoundedPolygon(geo.Body, 0.9f * u);

        g.FillPath(amberBrush, bodyPath);
        g.DrawPath(pen, bodyPath);

        if (size >= 32)
        {
            using var ribPen = new Pen(espresso, MathF.Max(1f, 0.6f * u))
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
            };
            g.DrawLine(ribPen, geo.Rib1X, geo.RibTop, geo.Rib1X, geo.RibBottom);
            g.DrawLine(ribPen, geo.Rib2X, geo.RibTop, geo.Rib2X, geo.RibBottom);
        }

        return bmp;
    }
}
