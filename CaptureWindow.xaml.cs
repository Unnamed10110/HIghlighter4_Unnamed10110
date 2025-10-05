using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Highlighter4
{
    public partial class CaptureWindow : Window
    {
        private bool _isCapturing = false;
        private System.Windows.Point _startPoint;
        private System.Windows.Point _endPoint;
        private readonly DispatcherTimer _updateTimer;
        private readonly DispatcherTimer _marchingAntsTimer;
        private double _dashOffset = 0;
        private HighlighterWindow? overlayWindow;

        public event EventHandler<Bitmap>? CaptureCompleted;

        public CaptureWindow()
        {
            InitializeComponent();
            
            // Timer for updating selection rectangle
            _updateTimer = new DispatcherTimer();
            _updateTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            _updateTimer.Tick += UpdateSelectionRectangle;
            
            // Timer for marching ants animation
            _marchingAntsTimer = new DispatcherTimer();
            _marchingAntsTimer.Interval = TimeSpan.FromMilliseconds(50); // 20 FPS for smooth animation
            _marchingAntsTimer.Tick += UpdateMarchingAnts;
        }
        
        public CaptureWindow(HighlighterWindow overlayWindow) : this()
        {
            this.overlayWindow = overlayWindow;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _startPoint = this.PointToScreen(e.GetPosition(this));
                _isCapturing = true;
                SelectionRectangle.Visibility = Visibility.Visible;
                _updateTimer.Start();
                _marchingAntsTimer.Start();
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isCapturing && e.LeftButton == MouseButtonState.Pressed)
            {
                _endPoint = this.PointToScreen(e.GetPosition(this));
                UpdateSelectionRectangle(sender, null);
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isCapturing && e.LeftButton == MouseButtonState.Released)
            {
                _isCapturing = false;
                _updateTimer.Stop();
                _marchingAntsTimer.Stop();
                
                // Capture the selected region
                CaptureSelectedRegion();
                
                // Close the capture window
                this.Close();
            }
        }

        private void UpdateSelectionRectangle(object? sender, EventArgs? e)
        {
            if (!_isCapturing) return;

            // Convert screen coordinates back to window coordinates for the selection rectangle
            var startWindowPoint = this.PointFromScreen(_startPoint);
            var endWindowPoint = this.PointFromScreen(_endPoint);

            var left = Math.Min(startWindowPoint.X, endWindowPoint.X);
            var top = Math.Min(startWindowPoint.Y, endWindowPoint.Y);
            var width = Math.Abs(endWindowPoint.X - startWindowPoint.X);
            var height = Math.Abs(endWindowPoint.Y - startWindowPoint.Y);

            Canvas.SetLeft(SelectionRectangle, left);
            Canvas.SetTop(SelectionRectangle, top);
            SelectionRectangle.Width = width;
            SelectionRectangle.Height = height;
        }
        
        private void UpdateMarchingAnts(object? sender, EventArgs? e)
        {
            if (!_isCapturing) return;
            
            // Update dash offset for marching ants effect
            _dashOffset += 1;
            if (_dashOffset >= 10) _dashOffset = 0;
            
            SelectionRectangle.StrokeDashOffset = _dashOffset;
        }

        private void CaptureSelectedRegion()
        {
            try
            {
                var left = (int)Math.Min(_startPoint.X, _endPoint.X);
                var top = (int)Math.Min(_startPoint.Y, _endPoint.Y);
                var width = (int)Math.Abs(_endPoint.X - _startPoint.X);
                var height = (int)Math.Abs(_endPoint.Y - _startPoint.Y);

                // Check if selection is valid
                if (width < 10 || height < 10)
                {
                    MessageBox.Show("Selection too small. Please select a larger area.", "Invalid Selection", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Since the window is maximized, mouse coordinates are already screen coordinates
                // Capture the screen region
                var bitmap = CaptureScreenRegion(left, top, width, height);
                
                if (bitmap != null)
                {
                    // Copy to clipboard
                    System.Windows.Clipboard.SetImage(ConvertBitmapToBitmapSource(bitmap));
                    
                    // Save to file
                    SaveCaptureToFile(bitmap);
                    
                    // Hide the overlay window if it exists
                    if (overlayWindow != null)
                    {
                        overlayWindow.HideHighlight();
                    }
                    
                    CaptureCompleted?.Invoke(this, bitmap);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error capturing region: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Bitmap? CaptureScreenRegion(int x, int y, int width, int height)
        {
            try
            {
                var bitmap = new Bitmap(width, height);
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    // First capture the screen content
                    graphics.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height));
                    
                    // If we have an overlay window, render its content on top
                    if (overlayWindow != null)
                    {
                        RenderOverlayContent(graphics, x, y, width, height);
                    }
                }
                return bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error capturing screen: {ex.Message}");
                return null;
            }
        }
        
        private void RenderOverlayContent(Graphics graphics, int x, int y, int width, int height)
        {
            try
            {
                // Hide the capture window temporarily to avoid capturing it
                this.Hide();
                
                // Force the overlay window to update its layout
                overlayWindow.UpdateLayout();
                
                // Get the actual canvas size
                var canvasWidth = (int)overlayWindow.MainCanvas.ActualWidth;
                var canvasHeight = (int)overlayWindow.MainCanvas.ActualHeight;
                
                System.Diagnostics.Debug.WriteLine($"Canvas size: {canvasWidth}x{canvasHeight}");
                System.Diagnostics.Debug.WriteLine($"Canvas children count: {overlayWindow.MainCanvas.Children.Count}");
                
                // If canvas size is 0, use screen dimensions
                if (canvasWidth == 0 || canvasHeight == 0)
                {
                    canvasWidth = (int)SystemParameters.PrimaryScreenWidth;
                    canvasHeight = (int)SystemParameters.PrimaryScreenHeight;
                    System.Diagnostics.Debug.WriteLine($"Using screen size: {canvasWidth}x{canvasHeight}");
                }
                
                // Render the overlay window content
                var overlayBitmap = RenderVisualToBitmap(overlayWindow.MainCanvas, canvasWidth, canvasHeight);
                
                if (overlayBitmap != null)
                {
                    // Calculate the offset to align overlay content with the captured region
                    // The overlay window is maximized and covers the entire screen
                    // We need to draw only the portion that corresponds to the captured region
                    var offsetX = -x; // Negative because we want to offset the overlay to match the capture region
                    var offsetY = -y;
                    
                    // Draw the overlay content on the captured bitmap
                    graphics.DrawImage(overlayBitmap, (float)offsetX, (float)offsetY);
                    overlayBitmap.Dispose();
                }
                
                // Show the capture window again
                this.Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error rendering overlay content: {ex.Message}");
                this.Show(); // Make sure to show the window even if there's an error
            }
        }
        
        private Bitmap? RenderVisualToBitmap(System.Windows.Controls.Canvas canvas, int width, int height)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Rendering canvas to bitmap: {width}x{height}");
                
                // Create a RenderTargetBitmap
                var renderTarget = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                
                // Render the canvas
                renderTarget.Render(canvas);
                
                // Convert to Bitmap
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));
                
                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    stream.Position = 0;
                    var bitmap = new Bitmap(stream);
                    System.Diagnostics.Debug.WriteLine($"Created bitmap: {bitmap.Width}x{bitmap.Height}");
                    return bitmap;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error rendering visual to bitmap: {ex.Message}");
                return null;
            }
        }

        private BitmapSource ConvertBitmapToBitmapSource(Bitmap bitmap)
        {
            using (var stream = new System.IO.MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);
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

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _isCapturing = false;
                _updateTimer.Stop();
                this.Close();
            }
            base.OnKeyDown(e);
        }

        private void SaveCaptureToFile(Bitmap bitmap)
        {
            try
            {
                // Get the Downloads folder
                var downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";
                
                // Create the directory structure: Downloads/Highlighter4/dd-MM-yyyy/
                var now = DateTime.Now;
                var dateFolder = now.ToString("dd-MM-yyyy");
                var hilighter4Path = Path.Combine(downloadsPath, "Highlighter4", dateFolder);
                
                // Create directory if it doesn't exist
                Directory.CreateDirectory(hilighter4Path);
                
                // Create filename: dd-MM-yyyy_HH-mm-ss.png
                var fileName = now.ToString("dd-MM-yyyy_HH-mm-ss") + ".png";
                var fullPath = Path.Combine(hilighter4Path, fileName);
                
                // Save the bitmap
                bitmap.Save(fullPath, System.Drawing.Imaging.ImageFormat.Png);
                
                // Mostrar notificación con miniatura
                Program.NotifyImageSaved("", "", bitmap, fullPath);
                
                // Reproducir sonido de beep
                Console.Beep();
                
                System.Diagnostics.Debug.WriteLine($"Capture saved to: {fullPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving capture: {ex.Message}");
                MessageBox.Show($"Error saving capture: {ex.Message}", "Save Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
