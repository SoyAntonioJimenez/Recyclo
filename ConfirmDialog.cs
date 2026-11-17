using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RecycloTray;

/// <summary>Botón redondeado, con variante sólida (acento) o fantasma.</summary>
internal sealed class RoundedButton : Button
{
    private readonly Theme _theme;
    private readonly bool _primary;

    public RoundedButton(Theme theme, string text, bool primary)
    {
        _theme = theme;
        _primary = primary;
        Text = text;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseOverBackColor = Color.Transparent;
        FlatAppearance.MouseDownBackColor = Color.Transparent;
        BackColor = Color.Transparent;
        ForeColor = primary ? theme.AccentText : theme.Text;
        Font = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point);
        Cursor = Cursors.Hand;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? _theme.Surface);

        var r = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = MaterialMenu.Rounded(r, 8);

        if (_primary)
        {
            using var brush = new SolidBrush(_theme.Accent);
            g.FillPath(brush, path);
        }
        else
        {
            using var pen = new Pen(_theme.Border);
            g.DrawPath(pen, path);
        }

        TextRenderer.DrawText(g, Text, Font, r, ForeColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}

/// <summary>Diálogo de confirmación con el mismo lenguaje visual que el menú.</summary>
internal sealed class ConfirmDialog : Form
{
    private readonly Theme _theme;
    private Point _dragStart;
    private bool _dragging;

    private ConfirmDialog(Theme theme, string title, string message, string acceptText)
    {
        _theme = theme;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        BackColor = theme.Surface;
        ForeColor = theme.Text;
        ClientSize = new Size(400, 168);
        DoubleBuffered = true;
        KeyPreview = true;

        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = theme.Text,
            AutoSize = false,
            Bounds = new Rectangle(24, 22, 352, 26),
        };

        var lblMessage = new Label
        {
            Text = message,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = theme.TextMuted,
            AutoSize = false,
            Bounds = new Rectangle(24, 54, 352, 44),
        };

        var cancel = new RoundedButton(theme, "Cancel", primary: false)
        {
            Bounds = new Rectangle(ClientSize.Width - 24 - 92 - 8 - 104, ClientSize.Height - 24 - 36, 92, 36),
            DialogResult = DialogResult.Cancel,
        };

        var accept = new RoundedButton(theme, acceptText, primary: true)
        {
            Bounds = new Rectangle(ClientSize.Width - 24 - 104, ClientSize.Height - 24 - 36, 104, 36),
            DialogResult = DialogResult.OK,
        };

        Controls.AddRange(new Control[] { lblTitle, lblMessage, cancel, accept });
        CancelButton = cancel;
        AcceptButton = accept;

        MouseDown += (_, e) => { _dragStart = e.Location; _dragging = true; };
        MouseMove += (_, e) =>
        {
            if (_dragging) Location = new Point(Location.X + e.X - _dragStart.X, Location.Y + e.Y - _dragStart.Y);
        };
        MouseUp += (_, _) => _dragging = false;

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape) DialogResult = DialogResult.Cancel;
        };
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        using var path = MaterialMenu.Rounded(ClientRectangle, 12);
        Region = new Region(path);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var pen = new Pen(_theme.Border);
        using var path = MaterialMenu.Rounded(new Rectangle(0, 0, Width - 1, Height - 1), 12);
        e.Graphics.DrawPath(pen, path);
    }

    /// <summary>Muestra el diálogo y devuelve true si el usuario confirma.</summary>
    public static bool Ask(Theme theme, string title, string message, string acceptText)
    {
        using var dialog = new ConfirmDialog(theme, title, message, acceptText);
        return dialog.ShowDialog() == DialogResult.OK;
    }
}
