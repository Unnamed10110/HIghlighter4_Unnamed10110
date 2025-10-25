using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Highlighter4
{
    /// <summary>
    /// Clase principal que contiene toda la funcionalidad de scroll capture
    /// en un solo archivo para fácil integración a otros proyectos.
    /// </summary>
    public class ScrollCaptureSingleFile : IDisposable
    {
        #region Configuración
        private const int StartDelay = 300;
        private const int ScrollDelay = 300;
        private const int ScrollAmount = 2;
        private const bool AutoIgnoreBottomEdge = true;
        #endregion

        #region Eventos
        public event Action<string> OnNotification;
        public event Action<Bitmap> OnImageSaved;
        public event Action<Bitmap> OnImageReadyForEditor;
        #endregion

        #region Propiedades
        public bool IsCapturing { get; private set; }
        public Bitmap Result { get; private set; }
        #endregion

        #region Campos Privados
        private Bitmap lastScreenshot;
        private Bitmap previousScreenshot;
        private bool stopRequested;
        private Rectangle selectedRectangle;
        private IntPtr selectedWindow;
        private int bestMatchCount, bestMatchIndex, bestIgnoreBottomOffset;
        private string cacheDirectory;
        private ScrollCaptureOverlay captureOverlay;
        #endregion

        #region Constructor
        public ScrollCaptureSingleFile(string cacheDir = null)
        {
            // Ya no necesitamos crear el directorio de cache
            // Las imágenes se guardan directamente en Downloads/Highlighter4/dd-MM-yyyy/
            cacheDirectory = null;
        }
        #endregion

        #region Métodos Públicos
        /// <summary>
        /// Inicia el proceso de captura con scroll
        /// </summary>
        public async Task StartCaptureAsync()
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
                
                ShowNotification("Iniciando captura...");

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
                ShowNotification($"Error en captura: {ex.Message}");
            }
            finally
            {
                IsCapturing = false;
            }
        }

        /// <summary>
        /// Detiene la captura actual
        /// </summary>
        public void StopCapture()
        {
            if (IsCapturing)
            {
                stopRequested = true;
                ShowNotification("Deteniendo captura...");
            }
        }

        /// <summary>
        /// Inicia captura de forma síncrona (para compatibilidad)
        /// </summary>
        public void StartCapture()
        {
            Task.Run(async () => await StartCaptureAsync());
        }
        #endregion

        #region Selección de Región
        private bool SelectRegion()
        {
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
        #endregion

        #region Captura Principal
        private async Task PerformCapture()
        {
            // Show capture overlay
            ShowCaptureOverlay();
            
            using (var screenshot = new Screenshot())
            {
                while (!stopRequested)
                {
                    // Capturar screenshot actual
                    lastScreenshot = screenshot.CaptureRectangle(selectedRectangle);

                    // Verificar si llegamos al final
                    if (CompareLastTwoImages())
                    {
                        ShowNotification("Fin del contenido detectado");
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

            // Hide capture overlay
            HideCaptureOverlay();
            
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
        #endregion

        #region Comparación de Imágenes
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
        #endregion

        #region Combinación de Imágenes
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
            int matchLimit = Math.Max(currentImage.Height / 2, 10); // Mínimo 10 píxeles para regiones pequeñas

            // Ignorar bordes laterales - ajustado para regiones pequeñas
            int ignoreSideOffset = Math.Max(Math.Min(10, currentImage.Width / 10), 0);
            ignoreSideOffset = Math.Min(ignoreSideOffset, currentImage.Width / 3);

            var rect = new Rectangle(ignoreSideOffset, result.Height - currentImage.Height, 
                Math.Max(currentImage.Width - ignoreSideOffset * 2, 1), currentImage.Height);
            
            // Asegurar que el rectángulo de comparación sea válido
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return null;
            }

            var bdResult = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), 
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var bdCurrentImage = currentImage.LockBits(new Rectangle(0, 0, currentImage.Width, currentImage.Height), 
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            int stride = bdResult.Stride;
            int pixelSize = stride / result.Width;
            IntPtr resultScan0 = bdResult.Scan0 + pixelSize * ignoreSideOffset;
            IntPtr currentImageScan0 = bdCurrentImage.Scan0 + pixelSize * ignoreSideOffset;
            int compareLength = pixelSize * rect.Width;

            // Ignorar parte inferior - ajustado para regiones pequeñas
            int ignoreBottomOffsetMax = Math.Max(currentImage.Height / 3, 5);
            int ignoreBottomOffset = Math.Max(Math.Min(10, currentImage.Height / 10), 0);

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
                        g.CompositingMode = CompositingMode.SourceCopy;
                        g.InterpolationMode = InterpolationMode.NearestNeighbor;

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
        #endregion

        #region Guardado de Resultado
        private void SaveResult()
        {
            if (Result == null) return;

            try
            {
                // Usar la ruta especificada: Downloads/Highlighter4/dd-MM-yyyy/dd-MM-yyyy.png
                var downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                downloadsPath = Path.Combine(downloadsPath, "Downloads");
                
                var now = DateTime.Now;
                var dateFolder = now.ToString("dd-MM-yyyy");
                var fileName = now.ToString("dd-MM-yyyy") + ".png";
                
                var basePath = Path.Combine(downloadsPath, "Highlighter4", dateFolder);
                
                // Crear directorio si no existe
                Directory.CreateDirectory(basePath);
                
                var fullPath = Path.Combine(basePath, fileName);

                Result.Save(fullPath, ImageFormat.Png);

                // Reproducir sonido de beep más fuerte (múltiples beeps)
                Console.Beep(800, 200);
                Console.Beep(1000, 200);
                
                // Copiar imagen al clipboard
                try
                {
                    // Limpiar el clipboard primero
                    System.Windows.Forms.Clipboard.Clear();
                    
                    // Crear una copia de la imagen para el clipboard
                    using (var clipboardImage = new System.Drawing.Bitmap(Result))
                    {
                        // Usar DataObject para mejor compatibilidad
                        var dataObject = new System.Windows.Forms.DataObject();
                        dataObject.SetData(System.Windows.Forms.DataFormats.Bitmap, clipboardImage);
                        
                        // Agregar datos en formato DIB (Device Independent Bitmap) para mayor compatibilidad
                        var ms = new MemoryStream();
                        clipboardImage.Save(ms, ImageFormat.Bmp);
                        var dibBytes = ConvertBmpToDib(ms.ToArray());
                        dataObject.SetData(System.Windows.Forms.DataFormats.Dib, dibBytes);
                        
                        // Agregar tambien en formato PNG para soporte en mas aplicaciones
                        using (var pngStream = new MemoryStream())
                        {
                            Result.Save(pngStream, ImageFormat.Png);
                            pngStream.Position = 0;
                            var pngData = pngStream.ToArray();
                            dataObject.SetData("PNG", new MemoryStream(pngData));
                        }
                        
                        // Intentar copiar al clipboard con múltiples intentos si falla
                        for (int attempt = 0; attempt < 3; attempt++)
                        {
                            try
                            {
                                System.Windows.Forms.Clipboard.SetDataObject(dataObject, true);
                                System.Diagnostics.Debug.WriteLine($"Imagen copiada al clipboard en el intento {attempt + 1}. Tamaño: {clipboardImage.Width}x{clipboardImage.Height}");
                                break;
                            }
                            catch (ExternalException ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Intento {attempt + 1} fallido: {ex.Message}");
                                if (attempt == 2) throw; // Si falla en todos los intentos, lanzar la excepción
                                System.Threading.Thread.Sleep(100); // Esperar un poco antes del siguiente intento
                            }
                        }
                    }
                    
                    // También copiar al clipboard usando WPF para mayor compatibilidad
                    try
                    {
                        // Convertir System.Drawing.Bitmap a WPF BitmapSource
                        var bitmapSource = ConvertBitmapToBitmapSource(Result);
                        if (bitmapSource != null)
                        {
                            for (int attempt = 0; attempt < 3; attempt++)
                            {
                                try
                                {
                                    System.Windows.Clipboard.SetImage(bitmapSource);
                                    System.Diagnostics.Debug.WriteLine("Imagen copiada al clipboard usando WPF exitosamente.");
                                    break;
                                }
                                catch (ExternalException ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Intento WPF {attempt + 1} fallido: {ex.Message}");
                                    if (attempt == 2) throw;
                                    System.Threading.Thread.Sleep(100);
                                }
                            }
                        }
                    }
                    catch (Exception wpfEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error copiando al clipboard con WPF: {wpfEx.Message}");
                    }
                }
                catch (Exception clipboardEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error copying to clipboard: {clipboardEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {clipboardEx.StackTrace}");
                }

                ShowNotification($"Imagen guardada: {fileName}");
                OnImageSaved?.Invoke(Result);
                
                // Abrir imagen en el editor automáticamente
                OnImageReadyForEditor?.Invoke(Result);
            }
            catch (Exception ex)
            {
                ShowNotification($"Error guardando imagen: {ex.Message}");
            }
        }
        #endregion

        #region Utilidades
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

        private void ShowNotification(string message)
        {
            OnNotification?.Invoke(message);
        }

        private System.Windows.Media.Imaging.BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
        {
            if (bitmap == null) return null;
            
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            
            try
            {
                var bitmapSource = System.Windows.Media.Imaging.BitmapSource.Create(
                    bitmap.Width, bitmap.Height, 96, 96,
                    System.Windows.Media.PixelFormats.Bgra32,
                    null,
                    bitmapData.Scan0,
                    bitmapData.Stride * bitmapData.Height,
                    bitmapData.Stride);
                
                return bitmapSource;
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }

        private byte[] ConvertBmpToDib(byte[] bmpBytes)
        {
            // Convertir BMP a DIB (Device Independent Bitmap)
            // El formato DIB es similar al BMP pero sin la cabecera BITMAPFILEHEADER de 14 bytes
            
            if (bmpBytes == null || bmpBytes.Length < 14)
                return bmpBytes;
            
            // Crear nuevo arreglo sin los primeros 14 bytes (BITMAPFILEHEADER)
            var dibBytes = new byte[bmpBytes.Length - 14];
            Array.Copy(bmpBytes, 14, dibBytes, 0, dibBytes.Length);
            
            return dibBytes;
        }

        public void Dispose()
        {
            Reset();
        }
        
        private void ShowCaptureOverlay()
        {
            try
            {
                // Create and show overlay on UI thread
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    captureOverlay = new ScrollCaptureOverlay(selectedRectangle);
                    captureOverlay.Show();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing capture overlay: {ex.Message}");
            }
        }
        
        private void HideCaptureOverlay()
        {
            try
            {
                // Hide overlay on UI thread
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    captureOverlay?.Close();
                    captureOverlay = null;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error hiding capture overlay: {ex.Message}");
            }
        }
        #endregion
    }
    
    #region ScrollCaptureOverlay
    
    /// <summary>
    /// Overlay window that shows the capture region border during scroll capture
    /// </summary>
    public class ScrollCaptureOverlay : System.Windows.Window
    {
        private System.Windows.Shapes.Rectangle borderRectangle;
        private System.Windows.Threading.DispatcherTimer animationTimer;
        private double dashOffset = 0;
        
        public ScrollCaptureOverlay(System.Drawing.Rectangle captureRect)
        {
            InitializeOverlay(captureRect);
        }
        
        private void InitializeOverlay(System.Drawing.Rectangle captureRect)
        {
            // Window configuration
            this.WindowStyle = System.Windows.WindowStyle.None;
            this.AllowsTransparency = true;
            this.Background = System.Windows.Media.Brushes.Transparent;
            this.Topmost = true;
            this.ShowInTaskbar = false;
            this.ResizeMode = System.Windows.ResizeMode.NoResize;
            this.WindowState = System.Windows.WindowState.Maximized;
            this.Left = 0;
            this.Top = 0;
            this.Width = System.Windows.SystemParameters.PrimaryScreenWidth;
            this.Height = System.Windows.SystemParameters.PrimaryScreenHeight;
            
            // Create canvas
            var canvas = new System.Windows.Controls.Canvas
            {
                Background = System.Windows.Media.Brushes.Transparent,
                IsHitTestVisible = false
            };
            
            // Create animated border rectangle
            borderRectangle = new System.Windows.Shapes.Rectangle
            {
                Width = captureRect.Width,
                Height = captureRect.Height,
                Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 68)), // Green #00ff44
                StrokeThickness = 1,
                StrokeDashArray = new System.Windows.Media.DoubleCollection { 8, 4 }, // Dashed line
                Fill = System.Windows.Media.Brushes.Transparent,
                IsHitTestVisible = false
            };
            
            System.Windows.Controls.Canvas.SetLeft(borderRectangle, captureRect.X);
            System.Windows.Controls.Canvas.SetTop(borderRectangle, captureRect.Y);
            canvas.Children.Add(borderRectangle);
            
            this.Content = canvas;
            
            // Start animation
            StartBorderAnimation();
        }
        
        private void StartBorderAnimation()
        {
            animationTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            
            animationTimer.Tick += (s, e) =>
            {
                dashOffset += 1;
                if (dashOffset >= 12) dashOffset = 0; // Reset at dash pattern length
                borderRectangle.StrokeDashOffset = dashOffset;
            };
            
            animationTimer.Start();
        }
        
        protected override void OnClosed(EventArgs e)
        {
            animationTimer?.Stop();
            animationTimer = null;
            base.OnClosed(e);
        }
    }
    
    #endregion

    #region Clases Auxiliares

    /// <summary>
    /// Formulario para selección de región con hook de teclado global
    /// </summary>
    public class RegionSelectionForm : Form
    {
        public Rectangle SelectedRectangle { get; private set; }
        public IntPtr SelectedWindow { get; private set; }

        private System.Drawing.Point startPoint;
        private System.Drawing.Point endPoint;
        private bool isSelecting = false;
        private Bitmap backgroundImage;
        private Graphics backgroundGraphics;
        private IntPtr hookID = IntPtr.Zero;
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        
        // Variables para animación del borde
        private Timer animationTimer;
        private float dashOffset = 0f;

        public RegionSelectionForm()
        {
            InitializeForm();
            CaptureBackground();
            InitializeAnimationTimer();
            
            // Instalar hook de teclado global
            InstallKeyboardHook();
            
            // Asegurar que el formulario pueda recibir eventos de teclado
            this.Focus();
            this.BringToFront();
        }

        private void InitializeForm()
        {
            // Configurar formulario para captura de pantalla completa
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            TopMost = true;
            ShowInTaskbar = false;
            Cursor = Cursors.Cross;
            BackColor = Color.Black;
            Opacity = 0.3;
            KeyPreview = true; // Permitir que el formulario reciba eventos de teclado
        }

        private void CaptureBackground()
        {
            // Capturar pantalla completa
            var screenBounds = Screen.PrimaryScreen.Bounds;
            backgroundImage = new Bitmap(screenBounds.Width, screenBounds.Height);
            backgroundGraphics = Graphics.FromImage(backgroundImage);
            backgroundGraphics.CopyFromScreen(screenBounds.X, screenBounds.Y, 0, 0, screenBounds.Size);
        }

        private void InitializeAnimationTimer()
        {
            animationTimer = new Timer();
            animationTimer.Interval = 50; // 20 FPS para animación suave
            animationTimer.Tick += (sender, e) => {
                dashOffset += 2f; // Velocidad de la animación
                if (dashOffset > 20f) dashOffset = 0f; // Reset cuando llega al final del patrón
                if (isSelecting)
                {
                    // Forzar redibujado del formulario
                    this.Invalidate();
                    this.Update();
                }
            };
            animationTimer.Start();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                startPoint = e.Location;
                endPoint = e.Location;
                isSelecting = true;
                
                // Reiniciar la animación
                dashOffset = 0f;
                
                // Asegurar que el timer esté funcionando
                if (!animationTimer.Enabled)
                {
                    animationTimer.Start();
                }
                
                Invalidate();
            }
            else if (e.Button == MouseButtons.Right)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (isSelecting)
            {
                endPoint = e.Location;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && isSelecting)
            {
                endPoint = e.Location;
                isSelecting = false;

                // Calcular rectángulo seleccionado
                var rect = new Rectangle(
                    Math.Min(startPoint.X, endPoint.X),
                    Math.Min(startPoint.Y, endPoint.Y),
                    Math.Abs(endPoint.X - startPoint.X),
                    Math.Abs(endPoint.Y - startPoint.Y)
                );

                if (rect.Width > 0 && rect.Height > 0)
                {
                    SelectedRectangle = rect;
                    SelectedWindow = GetWindowAtPoint(rect.Location);
                    DialogResult = DialogResult.OK;
                }
                else
                {
                    DialogResult = DialogResult.Cancel;
                }

                Close();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            
            if (e.KeyCode == Keys.Escape)
            {
                e.Handled = true;
                DialogResult = DialogResult.Cancel;
                Close();
            }
            else if (e.KeyCode == Keys.Enter && isSelecting)
            {
                e.Handled = true;
                // Confirmar selección con Enter
                OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, endPoint.X, endPoint.Y, 0));
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Dibujar fondo
            e.Graphics.DrawImage(backgroundImage, 0, 0);

            if (isSelecting)
            {
                // Dibujar rectángulo de selección
                var rect = new Rectangle(
                    Math.Min(startPoint.X, endPoint.X),
                    Math.Min(startPoint.Y, endPoint.Y),
                    Math.Abs(endPoint.X - startPoint.X),
                    Math.Abs(endPoint.Y - startPoint.Y)
                );

                // Dibujar fondo semi-transparente (muy transparente para no interferir)
                using (var brush = new SolidBrush(Color.FromArgb(5, Color.White)))
                {
                    e.Graphics.FillRectangle(brush, rect);
                }

                // Dibujar contorno verde entrecortado animado (100% opaco, sin transparencia)
                using (var pen = new Pen(Color.FromArgb(0, 255, 68), 2)) // Verde #00ff44, 2px de grosor
                {
                    // Configurar patrón de línea entrecortada
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Custom;
                    pen.DashPattern = new float[] { 6f, 3f }; // Líneas de 6px, espacios de 3px
                    pen.DashOffset = dashOffset; // Offset animado
                    
                    // Configurar para máxima opacidad
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None; // Sin suavizado para líneas más nítidas
                    e.Graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                    e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                    
                    // Dibujar el rectángulo múltiples veces para máxima opacidad
                    e.Graphics.DrawRectangle(pen, rect);
                    e.Graphics.DrawRectangle(pen, rect);
                    e.Graphics.DrawRectangle(pen, rect); // Dibujar tres veces para opacidad total
                }

                // Mostrar dimensiones
                string sizeText = $"{rect.Width} x {rect.Height}";
                using (var font = new Font("Arial", 12, System.Drawing.FontStyle.Bold))
                using (var brush = new SolidBrush(Color.FromArgb(0, 255, 68))) // Verde #00ff44
                {
                    e.Graphics.DrawString(sizeText, font, brush, rect.X, rect.Y - 25);
                }
            }
            else
            {
                // Mostrar instrucciones cuando no se está seleccionando
                string instructions = "Drag to select region | ESC to cancel | Enter to confirm";
                using (var font = new Font("Arial", 14, System.Drawing.FontStyle.Bold))
                using (var brush = new SolidBrush(Color.Yellow))
                using (var outlineBrush = new SolidBrush(Color.Black))
                {
                    var textSize = e.Graphics.MeasureString(instructions, font);
                    var x = (Width - textSize.Width) / 2;
                    var y = Height / 2 - 50;
                    
                    // Dibujar contorno negro
                    for (int i = -2; i <= 2; i++)
                    {
                        for (int j = -2; j <= 2; j++)
                        {
                            if (i != 0 || j != 0)
                            {
                                e.Graphics.DrawString(instructions, font, outlineBrush, x + i, y + j);
                            }
                        }
                    }
                    
                    // Dibujar texto amarillo
                    e.Graphics.DrawString(instructions, font, brush, x, y);
                }
            }
        }

        private IntPtr GetWindowAtPoint(System.Drawing.Point point)
        {
            return NativeMethods.WindowFromPoint(point);
        }

        // Declaraciones de Windows API para el hook de teclado
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private void InstallKeyboardHook()
        {
            if (hookID == IntPtr.Zero)
            {
                hookID = SetWindowsHookEx(WH_KEYBOARD_LL, KeyboardHookProc, GetModuleHandle(null), 0);
            }
        }

        private void UninstallKeyboardHook()
        {
            if (hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(hookID);
                hookID = IntPtr.Zero;
            }
        }

        private IntPtr KeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (vkCode == (int)Keys.Escape)
                {
                    // Cerrar el formulario desde el hook
                    this.Invoke(new Action(() => {
                        DialogResult = DialogResult.Cancel;
                        Close();
                    }));
                    return (IntPtr)1; // Consumir el evento
                }
            }
            return CallNextHookEx(hookID, nCode, wParam, lParam);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Detener y limpiar timer de animación
            animationTimer?.Stop();
            animationTimer?.Dispose();
            
            // Desinstalar hook de teclado
            UninstallKeyboardHook();
            
            // Limpiar recursos
            backgroundGraphics?.Dispose();
            backgroundImage?.Dispose();
            base.OnFormClosed(e);
        }
    }

    /// <summary>
    /// Clase para captura de pantalla
    /// </summary>
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

    /// <summary>
    /// Métodos nativos de Windows
    /// </summary>
    public static class NativeMethods
    {
        // Constantes para mouse events
        public const int MOUSEEVENTF_WHEEL = 0x0800;

        // Constantes para ventanas
        public const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(System.Drawing.Point point);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, int dwData, uint dwExtraInfo);

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int memcmp(IntPtr ptr1, IntPtr ptr2, int count);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out Rectangle lpRect);

        [DllImport("user32.dll")]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        // Constantes para SetWindowPos
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_SHOWWINDOW = 0x0040;
    }

    #endregion
}