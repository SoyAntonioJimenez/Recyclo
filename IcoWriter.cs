using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace RecycloTray;

/// <summary>
/// Empaqueta varios bitmaps en un .ico multi-tamaño (32 bpp con canal alfa),
/// para que Windows elija el tamaño nítido según el DPI.
/// </summary>
internal static class IcoWriter
{
    public static Icon Pack(IReadOnlyList<Bitmap> bitmaps)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);

        // ICONDIR
        w.Write((ushort)0);              // reservado
        w.Write((ushort)1);              // tipo: icono
        w.Write((ushort)bitmaps.Count);  // número de imágenes

        var payloads = new List<byte[]>(bitmaps.Count);
        foreach (var bmp in bitmaps)
            payloads.Add(ToDib(bmp));

        int offset = 6 + 16 * bitmaps.Count;
        for (int i = 0; i < bitmaps.Count; i++)
        {
            var bmp = bitmaps[i];
            w.Write((byte)(bmp.Width >= 256 ? 0 : bmp.Width));
            w.Write((byte)(bmp.Height >= 256 ? 0 : bmp.Height));
            w.Write((byte)0);            // colores de la paleta
            w.Write((byte)0);            // reservado
            w.Write((ushort)1);          // planos
            w.Write((ushort)32);         // bits por píxel
            w.Write(payloads[i].Length);
            w.Write(offset);
            offset += payloads[i].Length;
        }

        foreach (var p in payloads)
            w.Write(p);

        w.Flush();
        ms.Position = 0;
        return new Icon(ms);
    }

    /// <summary>BITMAPINFOHEADER + píxeles BGRA (de abajo arriba) + máscara AND.</summary>
    private static byte[] ToDib(Bitmap bmp)
    {
        int w = bmp.Width;
        int h = bmp.Height;
        int maskStride = ((w + 31) / 32) * 4;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // BITMAPINFOHEADER
        bw.Write(40);            // biSize
        bw.Write(w);             // biWidth
        bw.Write(h * 2);         // biHeight (XOR + AND)
        bw.Write((ushort)1);     // biPlanes
        bw.Write((ushort)32);    // biBitCount
        bw.Write(0);             // biCompression = BI_RGB
        bw.Write(w * h * 4);     // biSizeImage
        bw.Write(0);             // biXPelsPerMeter
        bw.Write(0);             // biYPelsPerMeter
        bw.Write(0);             // biClrUsed
        bw.Write(0);             // biClrImportant

        // Píxeles (BGRA, fila inferior primero)
        for (int y = h - 1; y >= 0; y--)
        {
            for (int x = 0; x < w; x++)
            {
                Color c = bmp.GetPixel(x, y);
                bw.Write(c.B);
                bw.Write(c.G);
                bw.Write(c.R);
                bw.Write(c.A);
            }
        }

        // Máscara AND: 1 = transparente. La rellenamos según el alfa real.
        var mask = new byte[maskStride * h];
        for (int y = 0; y < h; y++)
        {
            int srcY = h - 1 - y;
            for (int x = 0; x < w; x++)
            {
                if (bmp.GetPixel(x, srcY).A < 128)
                {
                    mask[y * maskStride + (x / 8)] |= (byte)(0x80 >> (x % 8));
                }
            }
        }
        bw.Write(mask);

        bw.Flush();
        return ms.ToArray();
    }
}
