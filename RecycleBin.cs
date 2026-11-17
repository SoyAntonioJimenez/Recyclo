using System.Runtime.InteropServices;

namespace RecycloTray;

internal readonly record struct BinState(int Items, long Bytes)
{
    public bool IsEmpty => Items <= 0;
}

/// <summary>Consulta y vacía la papelera de reciclaje mediante la API del shell.</summary>
internal static class RecycleBin
{
    // Ojo: en Windows 11 la API espera el tamaño con alineación natural (24 bytes).
    // Con Pack = 4 (20 bytes) devuelve E_INVALIDARG.
    [StructLayout(LayoutKind.Sequential)]
    private struct SHQUERYRBINFO
    {
        public int cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    private const uint SHERB_NOCONFIRMATION = 0x00000001;
    private const uint SHERB_NOPROGRESSUI = 0x00000002;
    private const uint SHERB_NOSOUND = 0x00000004;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

    /// <summary>Estado agregado de todas las papeleras de todas las unidades.</summary>
    public static BinState Query()
    {
        var info = new SHQUERYRBINFO { cbSize = Marshal.SizeOf<SHQUERYRBINFO>() };
        int hr = SHQueryRecycleBin(null, ref info);
        if (hr != 0)
            return new BinState(0, 0);

        return new BinState((int)info.i64NumItems, info.i64Size);
    }

    /// <summary>Vacía todas las papeleras sin sonido ni progreso (la confirmación la pone la app).</summary>
    public static bool Empty() =>
        SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND) == 0;
}
