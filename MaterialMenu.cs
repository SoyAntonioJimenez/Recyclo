using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RecycloTray;

/// <summary>Menú contextual con aspecto Material: esquinas redondeadas, hover suave y tema.</summary>
internal sealed class MaterialMenu : ContextMenuStrip
{
    private const int CS_DROPSHADOW = 0x00020000;

    private readonly Theme _theme;

    public MaterialMenu(Theme theme)
        : base()
    {
        _theme = theme;
        Renderer = new MaterialRenderer(theme);
        BackColor = theme.Surface;
        ForeColor = theme.Text;
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
        ShowImageMargin = true;
        ShowCheckMargin = false;
        ImageScalingSize = new Size(16, 16);
        Padding = new Padding(6);
        MinimumSize = new Size(210, 0);
        DropShadowEnabled = true;
    }

    /// <summary>Dibuja un glifo de Segoe Fluent Icons como imagen del elemento.</summary>
    internal static Image Glyph(char code, Color color, int size = 16)
    {
        var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        using var font = new Font("Segoe Fluent Icons", size - 1f, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(color);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };
        g.DrawString(code.ToString(), font, brush, new RectangleF(0, 0, size, size), format);
        return bmp;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= CS_DROPSHADOW;
            return cp;
        }
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        ApplyRoundedRegion();
    }

    private void ApplyRoundedRegion()
    {
        if (Width <= 0 || Height <= 0)
            return;

        using var path = Rounded(ClientRectangle, 10);
        var previous = Region;
        Region = new Region(path);
        previous?.Dispose();
    }

    internal static GraphicsPath Rounded(Rectangle r, float radius)
    {
        float d = radius * 2f;
        var path = new GraphicsPath();
        path.AddArc(r.Left, r.Top, d, d, 180, 90);
        path.AddArc(r.Right - d - 1, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d - 1, r.Bottom - d - 1, d, d, 0, 90);
        path.AddArc(r.Left, r.Bottom - d - 1, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}

/// <summary>Dibuja los elementos del menú con la paleta del tema.</summary>
internal sealed class MaterialRenderer : ToolStripRenderer
{
    private readonly Theme _theme;

    public MaterialRenderer(Theme theme) => _theme = theme;

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        e.Graphics.Clear(_theme.Surface);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var pen = new Pen(_theme.Border);
        using var path = MaterialMenu.Rounded(e.ToolStrip.ClientRectangle, 10);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.DrawPath(pen, path);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        // Sin banda lateral: el margen de iconos usa el mismo fondo que el menú
        using var brush = new SolidBrush(_theme.Surface);
        e.Graphics.FillRectangle(brush, e.AffectedBounds);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        if (e.Item.Selected && e.Item.Enabled)
        {
            var r = new Rectangle(6, e.Item.Bounds.Top, e.Item.Width - 12, e.Item.Height);
            using var path = MaterialMenu.Rounded(r, 6);
            using var brush = new SolidBrush(_theme.Hover);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillPath(brush, path);
        }
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.TextColor = e.Item.Enabled ? _theme.Text : _theme.TextMuted;
        base.OnRenderItemText(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        if (e.Vertical)
        {
            base.OnRenderSeparator(e);
            return;
        }

        int y = e.Item.Height / 2;
        using var pen = new Pen(_theme.Separator);
        e.Graphics.DrawLine(pen, 10, y, e.Item.Width - 10, y);
    }

    protected override void OnRenderItemBackground(ToolStripItemRenderEventArgs e)
    {
        // Sin fondo por defecto: lo pinta OnRenderMenuItemBackground
    }
}
