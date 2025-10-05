using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScrollCapture
{
    public class Screenshot : IDisposable
    {
        public Bitmap CaptureScreen()
        {
            var bounds = Screen.PrimaryScreen.Bounds;
            return CaptureRectangle(bounds);
        }

        public Bitmap CaptureRectangle(Rectangle bounds)
        {
            var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
            }

            return bitmap;
        }

        public void Dispose()
        {
            // No hay recursos que liberar en esta implementación simple
        }
    }
}
