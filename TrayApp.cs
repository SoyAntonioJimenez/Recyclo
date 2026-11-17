using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace RecycloTray;

/// <summary>Tray app: recycle bin icon with state, Material menu and emptying.</summary>
internal sealed class TrayApp : ApplicationContext
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValue = "RecycloTray";
    private const string AppKey = @"Software\RecycloTray";

    private readonly NotifyIcon _icon;
    private readonly System.Windows.Forms.Timer _timer;

    private Theme _theme;
    private MaterialMenu _menu = null!;
    private ToolStripMenuItem _emptyItem = null!;
    private ToolStripMenuItem _autoStartItem = null!;

    private Icon? _iconEmpty;
    private Icon? _iconFull;
    private BinState _state = new(0, 0);

    public TrayApp()
    {
        _theme = Theme.Current();
        _iconEmpty = IconFactory.Build(false, _theme.IsLight);
        _iconFull = IconFactory.Build(true, _theme.IsLight);

        // First run: register to start with Windows.
        EnsureConfigured();

        BuildMenu();

        _icon = new NotifyIcon
        {
            ContextMenuStrip = _menu,
            Visible = true,
            Text = "Recycle bin",
            Icon = _iconEmpty,
        };
        _icon.DoubleClick += (_, _) => OpenBin();

        _timer = new System.Windows.Forms.Timer { Interval = 1500 };
        _timer.Tick += (_, _) => Refresh(force: false);
        _timer.Start();

        Refresh(force: true);
    }

    private void BuildMenu()
    {
        var menu = new MaterialMenu(_theme);

        var open = Item("Open recycle bin", '\uE838', (_, _) => OpenBin());
        _emptyItem = Item("Empty recycle bin", '\uE74D', (_, _) => EmptyBin(), _theme.TextMuted);
        var refresh = Item("Refresh", '\uE72C', (_, _) => Refresh(force: true));
        _autoStartItem = Item("Start with Windows", '\uE7E8', (_, _) => ToggleAutoStart());
        var exit = Item("Exit", '\uE711', (_, _) => ExitApp());

        menu.Items.Add(open);
        menu.Items.Add(_emptyItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(refresh);
        menu.Items.Add(_autoStartItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exit);

        var previous = _menu;
        _menu = menu;
        if (_icon is not null)
            _icon.ContextMenuStrip = menu;
        previous?.Dispose();

        UpdateAutoStartVisual();
    }

    private ToolStripMenuItem Item(string text, char glyph, EventHandler onClick, Color? glyphColor = null)
    {
        var item = new ToolStripMenuItem(text, null, onClick)
        {
            Padding = new Padding(12, 7, 12, 7),
            ForeColor = _theme.Text,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular, GraphicsUnit.Point),
        };
        item.Image = MaterialMenu.Glyph(glyph, glyphColor ?? _theme.Text);
        return item;
    }

    private void UpdateAutoStartVisual()
    {
        if (_autoStartItem is null)
            return;

        bool enabled = IsAutoStartEnabled();
        _autoStartItem.Image?.Dispose();
        _autoStartItem.Image = MaterialMenu.Glyph('\uE7E8', enabled ? _theme.Accent : _theme.TextMuted);
    }

    private void Refresh(bool force)
    {
        BinState now = RecycleBin.Query();

        // Windows theme can change at any time (light <-> dark).
        Theme theme = Theme.Current();
        if (theme.IsLight != _theme.IsLight)
        {
            _theme = theme;
            RebuildVisuals();
            force = true;
        }

        bool changed = now != _state;
        _state = now;

        if (!force && !changed)
            return;

        _icon.Icon = _state.IsEmpty ? _iconEmpty : _iconFull;
        _icon.Text = BuildTooltip(_state);
        _emptyItem.Enabled = !_state.IsEmpty;
        _emptyItem.ForeColor = _state.IsEmpty ? _theme.TextMuted : _theme.Text;
        _emptyItem.Text = _state.IsEmpty ? "Empty recycle bin" : $"Empty recycle bin  ({_state.Items})";

        var glyphColor = _state.IsEmpty ? _theme.TextMuted : _theme.Text;
        var old = _emptyItem.Image;
        _emptyItem.Image = MaterialMenu.Glyph('\uE74D', glyphColor);
        old?.Dispose();
    }

    private void RebuildVisuals()
    {
        _iconEmpty?.Dispose();
        _iconFull?.Dispose();
        _iconEmpty = IconFactory.Build(false, _theme.IsLight);
        _iconFull = IconFactory.Build(true, _theme.IsLight);
        _icon.Icon = null;
        BuildMenu();
    }

    private static string BuildTooltip(BinState s)
    {
        if (s.IsEmpty)
            return "Recycle bin is empty";

        string items = s.Items == 1 ? "1 item" : $"{s.Items} items";
        string text = $"Recycle bin: {items} \u00b7 {FormatSize(s.Bytes)}";
        return text.Length > 63 ? text[..63] : text;
    }

    private static string FormatSize(long bytes)
    {
        double b = bytes;
        if (b < 1024) return $"{b:0} B";
        if (b < 1024 * 1024) return $"{b / 1024:0.0} KB";
        if (b < 1024L * 1024 * 1024) return $"{b / (1024.0 * 1024):0.0} MB";
        return $"{b / (1024.0 * 1024 * 1024):0.0} GB";
    }

    private void OpenBin()
    {
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", "shell:RecycleBinFolder")
            {
                UseShellExecute = true,
            });
        }
        catch
        {
            // nothing to do
        }
    }

    private void EmptyBin()
    {
        if (_state.IsEmpty)
            return;

        string items = _state.Items == 1 ? "1 item" : $"{_state.Items} items";
        string message = $"{items} ({FormatSize(_state.Bytes)}) will be permanently deleted.";

        if (!ConfirmDialog.Ask(_theme, "Empty the recycle bin?", message, "Empty"))
            return;

        Cursor.Current = Cursors.WaitCursor;
        try
        {
            RecycleBin.Empty();
        }
        finally
        {
            Cursor.Current = Cursors.Default;
        }

        Refresh(force: true);
    }

    private static bool IsAutoStartEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(RunValue) is string;
    }

    private static void EnsureConfigured()
    {
        using var key = Registry.CurrentUser.CreateSubKey(AppKey);
        bool configured = key?.GetValue("Configured") is int;

        if (!configured)
        {
            key?.SetValue("Configured", 1, RegistryValueKind.DWord);
            key?.SetValue("AutoStart", 1, RegistryValueKind.DWord);
            SetAutoStart(true);
            return;
        }

        bool autoStart = key?.GetValue("AutoStart") is int v && v == 1;
        SetAutoStart(autoStart);
    }

    private static void SetAutoStart(bool enabled)
    {
        using var run = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (run is null) return;

        if (enabled)
            run.SetValue(RunValue, $"\"{Application.ExecutablePath}\"");
        else
            run.DeleteValue(RunValue, throwOnMissingValue: false);
    }

    private void ToggleAutoStart()
    {
        bool enabled = !IsAutoStartEnabled();

        using (var key = Registry.CurrentUser.CreateSubKey(AppKey))
            key?.SetValue("AutoStart", enabled ? 1 : 0, RegistryValueKind.DWord);

        SetAutoStart(enabled);
        UpdateAutoStartVisual();
    }

    private void ExitApp()
    {
        _timer.Stop();
        _icon.Visible = false;
        _icon.Dispose();
        ExitThread();
    }
}
