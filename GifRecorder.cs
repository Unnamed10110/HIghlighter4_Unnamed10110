using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace Highlighter4
{
    /// <summary>
    /// Manages GIF recording using FFmpeg
    /// </summary>
    public class GifRecorder
    {
        private Process ffmpegProcess;
        private Rectangle recordingRegion;
        private string outputFilePath;
        private bool isRecording = false;
        private DateTime recordingStartTime;
        private RecordingBorderOverlay borderOverlay;
        
        public bool IsRecording => isRecording;
        
        public event EventHandler<string> RecordingStarted;
        public event EventHandler<string> RecordingStopped;
        public event EventHandler<string> RecordingError;
        public event EventHandler<int> RenderingProgress; // Progress in percentage (0-100)
        
        /// <summary>
        /// Starts recording a GIF in the specified region
        /// </summary>
        public async Task<bool> StartRecordingAsync(Rectangle region)
        {
            if (isRecording)
            {
                Debug.WriteLine("Recording is already in progress");
                return false;
            }
            
            // Check if FFmpeg is installed
            if (!FFmpegManager.IsFFmpegInstalled())
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    System.Windows.MessageBox.Show(
                        "FFmpeg is not installed.\n\n" +
                        "Please install FFmpeg from Settings → Advanced to use GIF recording.",
                        "FFmpeg Required",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                });
                return false;
            }
            
            try
            {
                recordingRegion = region;
                recordingStartTime = DateTime.Now;
                
                // Create output file path
                string timestamp = recordingStartTime.ToString("dd-MM-yyyy_HH-mm-ss");
                string dateFolder = recordingStartTime.ToString("dd-MM-yyyy");
                string basePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads",
                    "Highlighter4",
                    dateFolder
                );
                
                if (!Directory.Exists(basePath))
                {
                    Directory.CreateDirectory(basePath);
                }
                
                outputFilePath = Path.Combine(basePath, $"{timestamp}.gif");
                
                // Build FFmpeg command with improved settings to avoid black screen issues
                // Using gdigrab to capture Windows screen with better compatibility
                // Capture a slightly smaller region to avoid overlay interference
                int captureX = region.X + 2;  // Offset to avoid border
                int captureY = region.Y + 2;
                int captureWidth = region.Width - 4;  // Reduce width to avoid border
                int captureHeight = region.Height - 4; // Reduce height to avoid border
                
                string ffmpegArgs = $"-f gdigrab " +
                    $"-framerate 20 " +  // Higher framerate for better cursor capture
                    $"-offset_x {captureX} " +
                    $"-offset_y {captureY} " +
                    $"-video_size {captureWidth}x{captureHeight} " +
                    $"-draw_mouse 1 " +  // Enable cursor capture
                    $"-show_region 0 " + // Don't show region border
                    $"-i desktop " +
                    $"-vf \"fps=20,scale={captureWidth}:-1:flags=lanczos,eq=contrast=1.0:brightness=0.0:saturation=1.0,split[s0][s1];[s0]palettegen=max_colors=256[p];[s1][p]paletteuse=dither=bayer:bayer_scale=5\" " +
                    $"-c:v gif " +  // Use GIF codec directly
                    $"-pix_fmt rgb24 " +  // Use RGB24 pixel format for better cursor rendering
                    $"-loop 0 " +
                    $"\"{outputFilePath}\"";
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = ffmpegArgs,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true
                };
                
                ffmpegProcess = new Process { StartInfo = startInfo };
                
                // Subscribe to error output for debugging
                ffmpegProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Debug.WriteLine($"FFmpeg: {e.Data}");
                    }
                };
                
                ffmpegProcess.Start();
                ffmpegProcess.BeginErrorReadLine();
                
                isRecording = true;
                
                // Show overlay with advanced technique to avoid screen capture interference
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    borderOverlay = new RecordingBorderOverlay(recordingRegion);
                    borderOverlay.Show();
                });
                
                Debug.WriteLine($"GIF recording started: {outputFilePath}");
                Debug.WriteLine($"Region: X={region.X}, Y={region.Y}, W={region.Width}, H={region.Height}");
                
                RecordingStarted?.Invoke(this, outputFilePath);
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error starting GIF recording: {ex.Message}");
                RecordingError?.Invoke(this, ex.Message);
                
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    System.Windows.MessageBox.Show(
                        $"Failed to start GIF recording:\n\n{ex.Message}\n\n" +
                        "Make sure FFmpeg is properly installed.",
                        "Recording Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
                
                return false;
            }
        }
        
        /// <summary>
        /// Stops the current recording
        /// </summary>
        public async Task<string> StopRecordingAsync()
        {
            if (!isRecording || ffmpegProcess == null)
            {
                Debug.WriteLine("No recording in progress");
                return null;
            }
            
            try
            {
                Debug.WriteLine("Stopping GIF recording...");
                
                // Hide border overlay
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    borderOverlay?.Close();
                    borderOverlay = null;
                });
                
                // Send 'q' to FFmpeg to stop gracefully
                if (ffmpegProcess != null && !ffmpegProcess.HasExited)
                {
                    try
                    {
                        await ffmpegProcess.StandardInput.WriteLineAsync("q");
                        await ffmpegProcess.StandardInput.FlushAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error sending stop command: {ex.Message}");
                    }
                    
                    // Monitor FFmpeg progress while encoding
                    var progressTask = Task.Run(async () =>
                    {
                        int progress = 0;
                        while (!ffmpegProcess.HasExited && progress < 100)
                        {
                            await Task.Delay(100);
                            progress += 5; // Simulate progress (FFmpeg doesn't provide real-time progress for GIF)
                            if (progress > 95) progress = 95; // Cap at 95% until finished
                            
                            RenderingProgress?.Invoke(this, progress);
                            Debug.WriteLine($"Rendering progress: {progress}%");
                        }
                    });
                    
                    // Wait for FFmpeg to finish encoding (max 10 seconds)
                    bool exited = ffmpegProcess.WaitForExit(10000);
                    
                    if (!exited)
                    {
                        Debug.WriteLine("FFmpeg didn't exit gracefully, killing process");
                        ffmpegProcess.Kill();
                        await Task.Delay(1000); // Wait for file to be released
                    }
                    
                    // Report 100% completion
                    RenderingProgress?.Invoke(this, 100);
                    await Task.Delay(200); // Show 100% briefly
                }
                
                string savedPath = outputFilePath;
                
                ffmpegProcess?.Dispose();
                ffmpegProcess = null;
                isRecording = false;
                
                // Wait a bit for file to be fully written
                await Task.Delay(500);
                
                // Verify file exists
                if (File.Exists(savedPath))
                {
                    var fileInfo = new FileInfo(savedPath);
                    Debug.WriteLine($"GIF recording saved: {savedPath} ({fileInfo.Length} bytes)");
                    
                    RecordingStopped?.Invoke(this, savedPath);
                    
                    // Show notification
                    await ShowRecordingNotificationAsync(savedPath);
                    
                    return savedPath;
                }
                else
                {
                    Debug.WriteLine($"GIF file not found: {savedPath}");
                    RecordingError?.Invoke(this, "Output file was not created");
                    
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        System.Windows.MessageBox.Show(
                            "GIF recording completed but file was not created.\n\n" +
                            "This may happen if the recording was too short.",
                            "Recording Warning",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    });
                    
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error stopping GIF recording: {ex.Message}");
                RecordingError?.Invoke(this, ex.Message);
                
                isRecording = false;
                ffmpegProcess?.Dispose();
                ffmpegProcess = null;
                
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    System.Windows.MessageBox.Show(
                        $"Error stopping recording:\n\n{ex.Message}",
                        "Recording Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
                
                return null;
            }
        }
        
        /// <summary>
        /// Shows a notification with the recorded GIF
        /// </summary>
        private async Task ShowRecordingNotificationAsync(string gifPath)
        {
            try
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Load GIF as bitmap for thumbnail
                    using (var gifImage = System.Drawing.Image.FromFile(gifPath))
                    {
                        using (var bitmap = new Bitmap(gifImage))
                        {
                            // Show notification with thumbnail
                            Program.NotifyImageSaved("", "", bitmap, gifPath);
                        }
                    }
                    
                    Debug.WriteLine($"Notification shown for GIF: {gifPath}");
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing notification: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Cancels recording without saving
        /// </summary>
        public void CancelRecording()
        {
            // Hide border overlay
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                borderOverlay?.Close();
                borderOverlay = null;
            });
            
            if (ffmpegProcess != null && !ffmpegProcess.HasExited)
            {
                try
                {
                    ffmpegProcess.Kill();
                    ffmpegProcess.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error canceling recording: {ex.Message}");
                }
            }
            
            // Delete incomplete file
            if (!string.IsNullOrEmpty(outputFilePath) && File.Exists(outputFilePath))
            {
                try
                {
                    File.Delete(outputFilePath);
                    Debug.WriteLine($"Deleted incomplete GIF: {outputFilePath}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error deleting incomplete file: {ex.Message}");
                }
            }
            
            isRecording = false;
            ffmpegProcess = null;
        }
        
        /// <summary>
        /// Gets recording duration
        /// </summary>
        public TimeSpan GetRecordingDuration()
        {
            if (!isRecording)
                return TimeSpan.Zero;
                
            return DateTime.Now - recordingStartTime;
        }
    }
    
    /// <summary>
    /// Overlay window that shows a red border around the recording region
    /// </summary>
    public class RecordingBorderOverlay : System.Windows.Window
    {
        private System.Windows.Shapes.Rectangle borderRectangle;
        private System.Windows.Threading.DispatcherTimer animationTimer;
        private System.Windows.Threading.DispatcherTimer pulseTimer;
        private double dashOffset = 0;
        
        // P/Invoke declarations for creating a window that doesn't interfere with screen capture
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        
        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
        
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int WS_EX_TRANSPARENT = 0x20;
        private const uint LWA_COLORKEY = 0x1;
        private const uint LWA_ALPHA = 0x2;
        
        public RecordingBorderOverlay(Rectangle captureRect)
        {
            InitializeOverlay(captureRect);
        }
        
        private void InitializeOverlay(Rectangle captureRect)
        {
            // Window setup - Cover entire screen to show region border
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
            this.IsHitTestVisible = false; // Allow clicks to pass through
            
            this.Visibility = System.Windows.Visibility.Visible;
            
            var canvas = new System.Windows.Controls.Canvas
            {
                Background = System.Windows.Media.Brushes.Transparent,
                IsHitTestVisible = false
            };
            
            // Create animated border rectangle around the capture region
            borderRectangle = new System.Windows.Shapes.Rectangle
            {
                Width = captureRect.Width + 2,  // +2 to draw outside the region
                Height = captureRect.Height + 2,
                Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)), // White border
                StrokeThickness = 1, // 1px thickness
                StrokeDashArray = new System.Windows.Media.DoubleCollection { 6, 3 }, // Dashed line pattern
                Fill = System.Windows.Media.Brushes.Transparent,
                IsHitTestVisible = false
            };
            
            System.Windows.Controls.Canvas.SetLeft(borderRectangle, captureRect.X - 1);  // -1 to position outside
            System.Windows.Controls.Canvas.SetTop(borderRectangle, captureRect.Y - 1);
            canvas.Children.Add(borderRectangle);
            
            // Add timer display inside the capture region (bottom-left corner)
            var timerText = new System.Windows.Controls.TextBlock
            {
                Name = "TimerText",
                Text = "00:00.00",
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 16,
                FontWeight = System.Windows.FontWeights.Bold,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(220, 0, 0, 0)),
                Padding = new System.Windows.Thickness(8, 4, 8, 4),
                IsHitTestVisible = false
            };
            
            // Position timer inside the capture region, bottom-left corner
            System.Windows.Controls.Canvas.SetLeft(timerText, captureRect.X + 10);
            System.Windows.Controls.Canvas.SetTop(timerText, captureRect.Y + captureRect.Height - 35);
            canvas.Children.Add(timerText);
            
            // Add info text about cursor (bottom-right corner)
            var infoText = new System.Windows.Controls.TextBlock
            {
                Text = "Cursor enabled (advanced)",
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 10,
                FontWeight = System.Windows.FontWeights.Normal,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(180, 0, 0, 0)),
                Padding = new System.Windows.Thickness(4, 2, 4, 2),
                IsHitTestVisible = false
            };
            
            // Position info text in bottom-right corner
            System.Windows.Controls.Canvas.SetLeft(infoText, captureRect.X + captureRect.Width - 120);
            System.Windows.Controls.Canvas.SetTop(infoText, captureRect.Y + captureRect.Height - 20);
            canvas.Children.Add(infoText);
            
            this.Content = canvas;
            
            StartBorderAnimation();
            StartTimerAnimation(timerText);
        }
        
        
        private void StartBorderAnimation()
        {
            animationTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(30) // Faster animation for smoother movement
            };
            
            animationTimer.Tick += (s, e) =>
            {
                dashOffset += 0.5; // Slower increment for smoother movement
                if (dashOffset >= 9) dashOffset = 0; // Reset based on dash pattern (6+3=9)
                borderRectangle.StrokeDashOffset = dashOffset;
            };
            
            animationTimer.Start();
        }
        
        private void StartTimerAnimation(System.Windows.Controls.TextBlock timerText)
        {
            var startTime = DateTime.Now;
            
            pulseTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10) // Update every 10ms for smooth milliseconds
            };
            
            pulseTimer.Tick += (s, e) =>
            {
                var elapsed = DateTime.Now - startTime;
                
                // Format as mm:ss.ms (centiseconds - 2 digits)
                int centiseconds = elapsed.Milliseconds / 10; // Convert to centiseconds (00-99)
                string timeString = $"{elapsed.Minutes:D2}:{elapsed.Seconds:D2}.{centiseconds:D2}";
                
                timerText.Text = timeString;
                
                // Pulse effect on the text (subtle)
                if (elapsed.Milliseconds < 500)
                {
                    timerText.Foreground = System.Windows.Media.Brushes.White;
                }
                else
                {
                    timerText.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(200, 200, 200));
                }
            };
            
            pulseTimer.Start();
        }
        
        protected override void OnClosed(EventArgs e)
        {
            animationTimer?.Stop();
            animationTimer = null;
            pulseTimer?.Stop();
            pulseTimer = null;
            base.OnClosed(e);
        }
    }
}

