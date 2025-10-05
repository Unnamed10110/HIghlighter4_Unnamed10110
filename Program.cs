using System;
using System.Windows;
using System.Windows.Forms;
using Highlighter4;

namespace Highlighter4
{
    class Program
    {
        private static MainWindow mainWindow;
        private static ScrollCaptureSingleFile scrollCapture;
        private static NotificationManager notificationManager;
        
        // Método estático para notificaciones desde cualquier parte de la aplicación
        public static void NotifyImageSaved(string title, string message, System.Drawing.Bitmap bitmap)
        {
            notificationManager?.ShowImageNotification(title, message, bitmap, 6);
        }

        // Método estático para notificaciones con ruta del archivo
        public static void NotifyImageSaved(string title, string message, System.Drawing.Bitmap bitmap, string imagePath)
        {
            notificationManager?.ShowImageNotification(title, message, bitmap, imagePath, 6);
        }

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                // Inicializar NotificationManager
                notificationManager = new NotificationManager();
                
                // Inicializar ScrollCapture
                scrollCapture = new ScrollCaptureSingleFile();
                
                // Suscribirse a eventos de ScrollCapture
                scrollCapture.OnNotification += OnScrollCaptureNotification;
                scrollCapture.OnImageSaved += OnScrollCaptureImageSaved;
                scrollCapture.OnImageReadyForEditor += OnScrollCaptureImageReadyForEditor;
                

                // Crear y configurar la ventana principal
                mainWindow = new MainWindow();
                App.mainWindow = mainWindow; // Asignar referencia para HighlighterWindow
                
                // Suscribirse a eventos de la ventana principal
                mainWindow.ScrollCaptureRequested += OnScrollCaptureRequested;
                mainWindow.RegionCaptureRequested += OnRegionCaptureRequested;
                mainWindow.HotkeyPressed += OnHotkeyPressed;
                mainWindow.CaptureModeRequested += OnCaptureModeRequested;

                // Ejecutar la aplicación WPF
                var app = new System.Windows.Application();
                app.Run(mainWindow);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error: {ex.Message}", "Highlighter4 Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Limpiar recursos
                scrollCapture?.Dispose();
                notificationManager?.Dispose();
            }
        }

        private static void OnScrollCaptureRequested(object sender, EventArgs e)
        {
            try
            {
                if (scrollCapture.IsCapturing)
                {
                    scrollCapture.StopCapture();
                }
                else
                {
                    // Ejecutar en un hilo separado para no bloquear la UI
                    System.Threading.Tasks.Task.Run(async () =>
                    {
                        await scrollCapture.StartCaptureAsync();
                    });
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error en Scroll Capture: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void OnRegionCaptureRequested(object sender, EventArgs e)
        {
            try
            {
                // Crear y mostrar ventana de captura de región directamente
                var captureWindow = new CaptureWindow();
                
                // Suscribirse al evento de captura completada para abrir el editor
                captureWindow.CaptureCompleted += (s, bitmap) =>
                {
                    try
                    {
                        // Convertir Bitmap a BitmapSource
                        var bitmapSource = ConvertBitmapToBitmapSource(bitmap);
                        
                        // Abrir el editor de imágenes con la imagen capturada
                        var imageEditor = new ImageEditor(bitmapSource);
                        imageEditor.Show();
                    }
                    catch (Exception ex)
                    {
                        System.Windows.Forms.MessageBox.Show($"Error opening image editor: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                
                captureWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error en Region Capture: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private static void OnHotkeyPressed(object sender, EventArgs e)
        {
            // Crear y mostrar la ventana de resaltado
            var highlighterWindow = new HighlighterWindow();
            highlighterWindow.ToggleHighlight(); // Use ToggleHighlight instead of Show()
        }

        private static void OnCaptureModeRequested(object sender, EventArgs e)
        {
            // Crear y mostrar la ventana de resaltado para modo captura
            var highlighterWindow = new HighlighterWindow();
            highlighterWindow.ToggleHighlight(); // Use ToggleHighlight to ensure proper initialization
            mainWindow.TriggerCaptureModeWithOverlay(highlighterWindow);
        }

        private static void OnScrollCaptureNotification(string message)
        {
            // Mostrar notificación en el tray icon si es posible
            System.Diagnostics.Debug.WriteLine($"[ScrollCapture] {message}");
        }

        private static void OnScrollCaptureImageSaved(System.Drawing.Bitmap bitmap)
        {
            // Notificar que la imagen fue guardada (solo en debug, sin notificación visual)
            System.Diagnostics.Debug.WriteLine($"[ScrollCapture] Imagen guardada: {bitmap.Width}x{bitmap.Height} píxeles");
            
            // No mostrar notificación para ScrollCapture
            // Las notificaciones solo se muestran para el editor de imágenes
        }
        

        private static void OnScrollCaptureImageReadyForEditor(System.Drawing.Bitmap bitmap)
        {
            try
            {
                // Ejecutar en el hilo de UI para crear la ventana del editor
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    // Convertir System.Drawing.Bitmap a BitmapSource
                    var bitmapSource = ConvertBitmapToBitmapSource(bitmap);
                    
                    if (bitmapSource != null)
                    {
                        // Crear y mostrar el editor de imágenes
                        var editor = new ImageEditor(bitmapSource);
                        editor.Show();
                        
                        System.Diagnostics.Debug.WriteLine($"[ScrollCapture] Editor abierto con imagen: {bitmap.Width}x{bitmap.Height} píxeles");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ScrollCapture] Error abriendo editor: {ex.Message}");
            }
        }

        private static System.Windows.Media.Imaging.BitmapSource ConvertBitmapToBitmapSource(System.Drawing.Bitmap bitmap)
        {
            try
            {
                using (var stream = new System.IO.MemoryStream())
                {
                    bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    stream.Position = 0;

                    var bitmapImage = new System.Windows.Media.Imaging.BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = stream;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    return bitmapImage;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error convirtiendo bitmap: {ex.Message}");
                return null;
            }
        }
    }
}
