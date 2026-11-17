using System.Drawing;
using Microsoft.Win32;

namespace RecycloTray;

/// <summary>Paleta del menú según el tema de Windows (claro u oscuro).</summary>
internal sealed record Theme(bool IsLight)
{
    public static Theme Current()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        object? v = key?.GetValue("SystemUsesLightTheme");
        return new Theme(v is int i && i == 1);
    }

    public Color Surface => IsLight ? Color.FromArgb(0xFB, 0xFB, 0xFD) : Color.FromArgb(0x20, 0x21, 0x24);
    public Color Border => IsLight ? Color.FromArgb(0xE2, 0xE3, 0xE8) : Color.FromArgb(0x39, 0x3C, 0x41);
    public Color Text => IsLight ? Color.FromArgb(0x1A, 0x1B, 0x1E) : Color.FromArgb(0xF1, 0xF2, 0xF4);
    public Color TextMuted => IsLight ? Color.FromArgb(0x8A, 0x8F, 0x98) : Color.FromArgb(0x9A, 0xA0, 0xA9);
    public Color Hover => IsLight ? Color.FromArgb(0xEF, 0xF0, 0xF3) : Color.FromArgb(0x2C, 0x2F, 0x34);
    public Color Separator => IsLight ? Color.FromArgb(0xEC, 0xEC, 0xF0) : Color.FromArgb(0x33, 0x36, 0x3A);
    public Color Accent => Color.FromArgb(0xF2, 0xA3, 0x3C);
    public Color AccentText => Color.FromArgb(0x2A, 0x1C, 0x08);
}
