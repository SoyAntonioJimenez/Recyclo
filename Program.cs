using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace RecycloTray;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // Debug mode: write the recycle bin state as JSON and exit.
        if (args.Length > 0 && args[0].Equals("--dump", StringComparison.OrdinalIgnoreCase))
        {
            BinState s = RecycleBin.Query();
            string json = JsonSerializer.Serialize(new
            {
                items = s.Items,
                bytes = s.Bytes,
                isEmpty = s.IsEmpty,
            });
            string path = args.Length > 1 ? args[1] : Path.Combine(Path.GetTempPath(), "recyclotray-state.json");
            File.WriteAllText(path, json);
            Console.WriteLine(json);
            return;
        }

        // Utility mode: empty the recycle bin and exit.
        if (args.Length > 0 && args[0].Equals("--empty", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(RecycleBin.Empty() ? "ok" : "fail");
            return;
        }

        // Docs mode: render the context menu on a neutral backdrop.
        if (args.Length > 0 && args[0].Equals("--menu", StringComparison.OrdinalIgnoreCase))
        {
            string path = args.Length > 1 ? args[1] : Path.Combine(Path.GetTempPath(), "menu.png");
            bool? forceLight = args.Length > 2
                ? args[2].Equals("light", StringComparison.OrdinalIgnoreCase)
                : null;
            CaptureMenu(path, forceLight);
            Console.WriteLine(path);
            return;
        }

        // Docs mode: contact sheet with every icon state and size.
        if (args.Length > 0 && args[0].Equals("--preview", StringComparison.OrdinalIgnoreCase))
        {
            string path = args.Length > 1 ? args[1] : Path.Combine(Path.GetTempPath(), "recyclotray-preview.png");
            RenderPreview(path);
            Console.WriteLine(path);
            return;
        }

        // Utility mode: regenerate the application .ico.
        if (args.Length > 0 && args[0].Equals("--make-icon", StringComparison.OrdinalIgnoreCase))
        {
            string path = args.Length > 1 ? args[1] : "app.ico";
            var sizes = new[] { 16, 24, 32, 48, 64, 128, 256 };
            var bitmaps = new List<Bitmap>();
            foreach (int s in sizes) bitmaps.Add(IconFactory.DrawAppIcon(s));
            using (var icon = IcoWriter.Pack(bitmaps))
            using (var fs = File.Create(path))
            {
                icon.Save(fs);
            }
            foreach (var b in bitmaps) b.Dispose();
            Console.WriteLine(path);
            return;
        }

        using var mutex = new Mutex(initiallyOwned: true, name: @"Local\RecycloTray_SingleInstance", out bool created);
        if (!created)
            return;   // already running

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApp());
    }

    /// <summary>Shows the menu over a neutral backdrop and saves a screenshot (for the docs).</summary>
    private static void CaptureMenu(string path, bool? forceLight)
    {
        ApplicationConfiguration.Initialize();

        Theme theme = forceLight is null ? Theme.Current() : new Theme(forceLight.Value);
        var menu = new MaterialMenu(theme);

        void Add(string text, char glyph, Color? color = null)
        {
            var item = new ToolStripMenuItem(text)
            {
                Padding = new Padding(12, 7, 12, 7),
                ForeColor = theme.Text,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point),
            };
            item.Image = MaterialMenu.Glyph(glyph, color ?? theme.Text);
            menu.Items.Add(item);
        }

        Add("Open recycle bin", '\uE838');
        Add("Empty recycle bin  (3)", '\uE74D');
        menu.Items.Add(new ToolStripSeparator());
        Add("Refresh", '\uE72C');
        Add("Start with Windows", '\uE7E8', theme.Accent);
        menu.Items.Add(new ToolStripSeparator());
        Add("Exit", '\uE711');

        var backdropColor = theme.IsLight ? Color.FromArgb(0xE8, 0xEA, 0xEE) : Color.FromArgb(0x0E, 0x10, 0x13);

        var host = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.CenterScreen,
            ClientSize = new Size(460, 320),
            BackColor = backdropColor,
            ShowInTaskbar = false,
            TopMost = true,
        };

        var timer = new System.Windows.Forms.Timer { Interval = 800 };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            var r = host.Bounds;
            using var bmp = new Bitmap(r.Width, r.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(r.Left, r.Top, 0, 0, r.Size);
            }
            bmp.Save(path, ImageFormat.Png);
            menu.Close();
            host.Close();
        };

        host.Shown += (_, _) =>
        {
            var pt = new Point(host.Left + 40, host.Top + 40);
            menu.Show(pt);
            timer.Start();
        };

        Application.Run(host);
    }

    /// <summary>Contact sheet: 4 combinations (state × theme) at 4 sizes.</summary>
    private static void RenderPreview(string path)
    {
        var combos = new (bool full, bool light, string label)[]
        {
            (false, false, "empty / dark"),
            (true,  false, "full / dark"),
            (false, true,  "empty / light"),
            (true,  true,  "full / light"),
        };

        int[] sizes = { 16, 24, 32, 64 };
        int cellW = 96, cellH = 84, pad = 14;
        int width = pad * 2 + cellW * sizes.Length;
        int height = pad * 2 + cellH * combos.Length;

        using var sheet = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(sheet))
        {
            g.Clear(Color.FromArgb(0x20, 0x20, 0x20));
            g.SmoothingMode = SmoothingMode.AntiAlias;

            for (int r = 0; r < combos.Length; r++)
            {
                var (full, light, label) = combos[r];
                var bg = light ? Color.FromArgb(0xF1, 0xF1, 0xF1) : Color.FromArgb(0x20, 0x20, 0x20);
                var fg = light ? Color.FromArgb(0x55, 0x55, 0x55) : Color.FromArgb(0xAA, 0xAA, 0xAA);

                using (var brush = new SolidBrush(bg))
                    g.FillRectangle(brush, pad, pad + r * cellH, width - pad * 2, cellH - 6);

                using (var brush = new SolidBrush(fg))
                using (var font = new Font("Segoe UI", 7.5f))
                    g.DrawString(label, font, brush, pad + 4, pad + r * cellH + 4);

                Color ink = light ? IconFactory.InkLight : IconFactory.InkDark;
                for (int c = 0; c < sizes.Length; c++)
                {
                    int s = sizes[c];
                    using var icon = IconFactory.Draw(s, full, ink);
                    float x = pad + c * cellW + (cellW - s) / 2f;
                    float y = pad + r * cellH + (cellH - s) / 2f + 6;
                    g.DrawImage(icon, x, y, s, s);
                }
            }
        }

        sheet.Save(path, ImageFormat.Png);
    }
}
