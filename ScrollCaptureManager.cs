using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace ScrollCapture
{
    public class ScrollCaptureManager : IDisposable
    {
        public bool IsCapturing { get; private set; }
        public Bitmap Result { get; private set; }

        private Bitmap lastScreenshot;
        private Bitmap previousScreenshot;
        private bool stopRequested;
        private Rectangle selectedRectangle;
        private IntPtr selectedWindow;
        private int bestMatchCount, bestMatchIndex, bestIgnoreBottomOffset;

        // Configuración
        private const int StartDelay = 300;
        private const int ScrollDelay = 300;
        private const int ScrollAmount = 2;
        private const bool AutoIgnoreBottomEdge = true;

        public ScrollCaptureManager()
        {
            // Crear directorio de cache si no existe
            string cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "ScrollCaptureCache");
            if (!Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }
        }

        public async void StartCapture()
        {
            if (IsCapturing) return;

            try
            {
                // Seleccionar región
                if (!SelectRegion())
                {
                    return;
                }

                IsCapturing = true;
                stopRequested = false;
                Reset();

                // Activar ventana seleccionada
                if (selectedWindow != IntPtr.Zero)
                {
                    NativeMethods.SetForegroundWindow(selectedWindow);
                    await Task.Delay(StartDelay);
                }

                // Iniciar captura
                await PerformCapture();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en captura: {ex.Message}");
            }
            finally
            {
                IsCapturing = false;
            }
        }

        public void StopCapture()
        {
            if (IsCapturing)
            {
                stopRequested = true;
            }
        }

        private bool SelectRegion()
        {
            // Crear formulario de selección de región
            using (var regionForm = new RegionSelectionForm())
            {
                if (regionForm.ShowDialog() == DialogResult.OK)
                {
                    selectedRectangle = regionForm.SelectedRectangle;
                    selectedWindow = regionForm.SelectedWindow;
                    return !selectedRectangle.IsEmpty;
                }
            }
            return false;
        }

        private async Task PerformCapture()
        {
            using (var screenshot = new Screenshot())
            {
                while (!stopRequested)
                {
                    // Capturar screenshot actual
                    lastScreenshot = screenshot.CaptureRectangle(selectedRectangle);

                    // Verificar si llegamos al final
                    if (CompareLastTwoImages())
                    {
                        break;
                    }

                    // Realizar scroll
                    PerformScroll();

                    // Combinar imágenes
                    if (lastScreenshot != null)
                    {
                        var newResult = await CombineImagesAsync(Result, lastScreenshot);
                        if (newResult != null)
                        {
                            Result?.Dispose();
                            Result = newResult;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (stopRequested) break;

                    // Preparar para siguiente iteración
                    if (lastScreenshot != null)
                    {
                        previousScreenshot?.Dispose();
                        previousScreenshot = lastScreenshot;
                        lastScreenshot = null;
                    }

                    // Delay entre capturas
                    await Task.Delay(ScrollDelay);
                }
            }

            // Guardar resultado
            if (Result != null)
            {
                SaveResult();
            }
        }

        private void PerformScroll()
        {
            // Usar rueda del mouse para scroll
            NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_WHEEL, 0, 0, -120 * ScrollAmount, 0);
        }

        private bool CompareLastTwoImages()
        {
            if (lastScreenshot != null && previousScreenshot != null)
            {
                return CompareImages(lastScreenshot, previousScreenshot);
            }
            return false;
        }

        private bool CompareImages(Bitmap bmp1, Bitmap bmp2)
        {
            if (bmp1 == null || bmp2 == null || bmp1.Width != bmp2.Width || bmp1.Height != bmp2.Height)
            {
                return false;
            }

            var bd1 = bmp1.LockBits(new Rectangle(0, 0, bmp1.Width, bmp1.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var bd2 = bmp2.LockBits(new Rectangle(0, 0, bmp2.Width, bmp2.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            try
            {
                return NativeMethods.memcmp(bd1.Scan0, bd2.Scan0, bd1.Stride * bmp1.Height) == 0;
            }
            finally
            {
                bmp1.UnlockBits(bd1);
                bmp2.UnlockBits(bd2);
            }
        }

        private async Task<Bitmap> CombineImagesAsync(Bitmap result, Bitmap currentImage)
        {
            return await Task.Run(() => CombineImages(result, currentImage));
        }

        private Bitmap CombineImages(Bitmap result, Bitmap currentImage)
        {
            if (result == null)
            {
                return (Bitmap)currentImage.Clone();
            }

            int matchCount = 0;
            int matchIndex = 0;
            int matchLimit = currentImage.Height / 2;

            // Ignorar bordes laterales
            int ignoreSideOffset = Math.Max(50, currentImage.Width / 20);
            ignoreSideOffset = Math.Min(ignoreSideOffset, currentImage.Width / 3);

            var rect = new Rectangle(ignoreSideOffset, result.Height - currentImage.Height, 
                currentImage.Width - ignoreSideOffset * 2, currentImage.Height);

            var bdResult = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), 
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var bdCurrentImage = currentImage.LockBits(new Rectangle(0, 0, currentImage.Width, currentImage.Height), 
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            int stride = bdResult.Stride;
            int pixelSize = stride / result.Width;
            IntPtr resultScan0 = bdResult.Scan0 + pixelSize * ignoreSideOffset;
            IntPtr currentImageScan0 = bdCurrentImage.Scan0 + pixelSize * ignoreSideOffset;
            int compareLength = pixelSize * rect.Width;

            // Ignorar parte inferior
            int ignoreBottomOffsetMax = currentImage.Height / 3;
            int ignoreBottomOffset = Math.Max(50, currentImage.Height / 10);

            if (AutoIgnoreBottomEdge)
            {
                IntPtr resultScan0Last = resultScan0 + (result.Height - 1) * stride;
                IntPtr currentImageScan0Last = currentImageScan0 + (currentImage.Height - 1) * stride;

                for (int i = 0; i <= ignoreBottomOffsetMax; i++)
                {
                    if (NativeMethods.memcmp(resultScan0Last - i * stride, currentImageScan0Last - i * stride, compareLength) != 0)
                    {
                        ignoreBottomOffset += i;
                        break;
                    }
                }

                ignoreBottomOffset = Math.Max(ignoreBottomOffset, bestIgnoreBottomOffset);
            }

            ignoreBottomOffset = Math.Min(ignoreBottomOffset, ignoreBottomOffsetMax);
            int rectBottom = rect.Bottom - ignoreBottomOffset - 1;

            // Buscar mejor coincidencia
            for (int currentImageY = currentImage.Height - 1; currentImageY >= 0 && matchCount < matchLimit; currentImageY--)
            {
                int currentMatchCount = 0;

                for (int y = 0; currentImageY - y >= 0 && currentMatchCount < matchLimit; y++)
                {
                    if (NativeMethods.memcmp(resultScan0 + ((rectBottom - y) * stride), 
                        currentImageScan0 + ((currentImageY - y) * stride), compareLength) == 0)
                    {
                        currentMatchCount++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (currentMatchCount > matchCount)
                {
                    matchCount = currentMatchCount;
                    matchIndex = currentImageY;
                }
            }

            result.UnlockBits(bdResult);
            currentImage.UnlockBits(bdCurrentImage);

            // Usar mejor estimación si no hay coincidencia
            if (matchCount == 0 && bestMatchCount > 0)
            {
                matchCount = bestMatchCount;
                matchIndex = bestMatchIndex;
                ignoreBottomOffset = bestIgnoreBottomOffset;
            }

            if (matchCount > 0)
            {
                int matchHeight = currentImage.Height - matchIndex - 1;

                if (matchHeight > 0)
                {
                    if (matchCount > bestMatchCount)
                    {
                        bestMatchCount = matchCount;
                        bestMatchIndex = matchIndex;
                        bestIgnoreBottomOffset = ignoreBottomOffset;
                    }

                    var newResult = new Bitmap(result.Width, result.Height - ignoreBottomOffset + matchHeight);

                    using (var g = Graphics.FromImage(newResult))
                    {
                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;

                        g.DrawImage(result, new Rectangle(0, 0, result.Width, result.Height - ignoreBottomOffset),
                            new Rectangle(0, 0, result.Width, result.Height - ignoreBottomOffset), GraphicsUnit.Pixel);
                        g.DrawImage(currentImage, new Rectangle(0, result.Height - ignoreBottomOffset, currentImage.Width, matchHeight),
                            new Rectangle(0, matchIndex + 1, currentImage.Width, matchHeight), GraphicsUnit.Pixel);
                    }

                    return newResult;
                }
            }

            return null;
        }

        private void SaveResult()
        {
            if (Result == null) return;

            try
            {
                string timestamp = DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss");
                string fileName = $"{timestamp}.png";
                string cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "ScrollCaptureCache");
                string filePath = Path.Combine(cacheDir, fileName);

                Result.Save(filePath, ImageFormat.Png);

                // Reproducir sonido de beep
                Console.Beep();

                // Abrir carpeta de destino
                Process.Start("explorer.exe", cacheDir);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error guardando imagen: {ex.Message}");
            }
        }

        private void Reset(bool keepResult = false)
        {
            lastScreenshot?.Dispose();
            lastScreenshot = null;

            previousScreenshot?.Dispose();
            previousScreenshot = null;

            if (!keepResult)
            {
                Result?.Dispose();
                Result = null;
            }

            bestMatchCount = 0;
            bestMatchIndex = 0;
            bestIgnoreBottomOffset = 0;
        }

        public void Dispose()
        {
            Reset();
        }
    }
}
