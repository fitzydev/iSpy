using System.Runtime.InteropServices;
using Emgu.CV;

namespace iSpyApplication.Controls
{
    public static class GraphicsHelper
    {
        // Suppress the thread-safety warning for this performance-critical flag
        #pragma warning disable CA2211
        public static volatile bool UseManaged = true;
        #pragma warning restore CA2211

        // FIX: Changed parameter type from 'IInputArray' to 'Mat'.
        // This is the correct base class that has the .ToBitmap() method
        // and accepts both Mat and Image<,> objects.
        public static void GdiDrawImage(this Graphics graphics, Mat image, Rectangle r)
        {
            if (image == null || image.IsEmpty)
                return;

            try
            {
                // 'image' is now a Mat object, which has .ToBitmap()
                using (Bitmap bmp = image.ToBitmap())
                {
                    graphics.GdiDrawImage(bmp, r);
                }
            }
            catch (Exception)
            {
                // Can happen if the underlying Mat is disposed by another thread
            }
        }

        // FIX: Changed parameter type from 'IInputArray' to 'Mat'
        public static void GdiDrawImage(this Graphics graphics, Mat image, int x, int y, int w, int h)
        {
            graphics.GdiDrawImage(image, new Rectangle(x, y, w, h));
        }

        // This high-performance worker method is unchanged
        public static void GdiDrawImage(this Graphics graphics, Bitmap image, Rectangle r)
        {
            if (UseManaged)
            {
                try
                {
                    graphics.DrawImage(image, r);
                }
                catch
                {
                    // GDI+ can be flaky on high-load
                }
                return;
            }

            IntPtr hdc = IntPtr.Zero;
            IntPtr memdc = IntPtr.Zero;
            IntPtr bmp = IntPtr.Zero;

            try
            {
                hdc = graphics.GetHdc();
                memdc = GdiInterop.CreateCompatibleDC(hdc);
                bmp = image.GetHbitmap();

                GdiInterop.SelectObject(memdc, bmp);
                GdiInterop.SetStretchBltMode(hdc, 0x04);

                GdiInterop.StretchBlt(hdc, r.Left, r.Top, r.Width, r.Height, memdc, 0, 0, image.Width, image.Height, GdiInterop.TernaryRasterOperations.SRCCOPY);
            }
            finally
            {
                if (bmp != IntPtr.Zero)
                {
                    GdiInterop.DeleteObject(bmp);
                }
                if (memdc != IntPtr.Zero)
                {
                    GdiInterop.DeleteDC(memdc);
                }
                if (hdc != IntPtr.Zero)
                {
                    graphics.ReleaseHdc(hdc);
                }
            }
        }
    }

    // This class is unchanged (still internal)
    internal class GdiInterop
    {
        public enum TernaryRasterOperations
        {
            SRCCOPY = 0x00CC0020,
            SRCPaint = 0x00EE0086,
            SRCAND = 0x008800C6,
            SRCINVERT = 0x00660046,
            SRCERASE = 0x00440328,
            NOTSRCCOPY = 0x00330008,
            NOTSRCERASE = 0x001100A6,
            MERGECOPY = 0x00C000CA,
            MERGEPAINT = 0x00BB0226,
            PATCOPY = 0x00F00021,
            PATPAINT = 0x00FB0A09,
            PATINVERT = 0x005A0049,
            DSTINVERT = 0x00550009,
            BLACKNESS = 0x00000042,
            WHITENESS = 0x00FF0062,
            CAPTUREBLT = 0x40000000
        };

        public enum Bool
        {
            False = 0,
            True
        };

        [DllImport("gdi32.dll")]
        public static extern int SetBkColor(IntPtr hdc, int crColor);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern Bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern Bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hObject, int width, int height);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern Bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hObjSource, int nXSrc, int nYSrc, TernaryRasterOperations dwRop);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern Bool StretchBlt(IntPtr hObject, int nXOriginDest, int nYOriginDest, int nWidthDest, int nHeightDest, IntPtr hObjSource, int nXOriginSrc, int nYOriginSrc, int nWidthSrc, int nHeightSrc, TernaryRasterOperations dwRop);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern Bool SetStretchBltMode(IntPtr hObject, int nStretchMode);
    }
}