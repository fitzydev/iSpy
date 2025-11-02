using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Drawing.Imaging;

namespace iSpyApplication.Vision
{
    public static class Tools
    {
        // NO CHANGES NEEDED HERE
        // With Accord.Imaging removed, 'PixelFormat' is no longer ambiguous
        // and correctly resolves to System.Drawing.Imaging.PixelFormat.
        public static int BytesPerPixel(PixelFormat pixelFormat)
        {
            int bytesPerPixel;

            // calculate bytes per pixel
            switch (pixelFormat)
            {
                case PixelFormat.Format8bppIndexed:
                    bytesPerPixel = 1;
                    break;
                case PixelFormat.Format16bppGrayScale:
                    bytesPerPixel = 2;
                    break;
                case PixelFormat.Format24bppRgb:
                    bytesPerPixel = 3;
                    break;
                case PixelFormat.Format32bppRgb:
                case PixelFormat.Format32bppArgb:
                case PixelFormat.Format32bppPArgb:
                    bytesPerPixel = 4;
                    break;
                case PixelFormat.Format48bppRgb:
                    bytesPerPixel = 6;
                    break;
                case PixelFormat.Format64bppArgb:
                case PixelFormat.Format64bppPArgb:
                    bytesPerPixel = 8;
                    break;
                default:
                    // You may need to create or find this exception class,
                    // or replace it with a standard System.Exception
                    throw new System.Exception("Can not create image with specified pixel format.");
            }
            return bytesPerPixel;
        }

        // --- THIS METHOD IS MIGRATED TO EMGU.CV ---
        // The method signature changes from UnmanagedImage to Mat
        public static void ConvertToGrayscale(Emgu.CV.Mat source, Emgu.CV.Mat destination)
        {
            // In Emgu.CV, we check the NumberOfChannels. 1 = Grayscale.
            if (source.NumberOfChannels != 1)
            {
                // The 'Apply' filter pattern is replaced by a static CvInvoke call.
                // We assume BGR source, which is OpenCV's default.
                // If your source might be BGRA, you can use ColorConversion.Bgra2Gray
                CvInvoke.CvtColor(source, destination, ColorConversion.Bgr2Gray);
            }
            else
            {
                // The copy operation is very similar
                source.CopyTo(destination);
            }
        }
    }
}