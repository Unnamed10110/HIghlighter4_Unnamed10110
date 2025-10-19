using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Drawing.Drawing2D;
using System.Windows.Controls;
using Microsoft.Win32;

namespace Highlighter4
{
    public partial class MainWindow : Window
    {
        private const int WM_HOTKEY = 0x0312;
        private const int WM_TRAYICON = 0x8000;
        private const int HOTKEY_ID = 9002;
        private const int HOTKEY_HIGHLIGHT_ID = 9003;
        private const int HOTKEY_SCROLL_CAPTURE_ID = 9004;
        private const int HOTKEY_REGION_CAPTURE_ID = 9005;
        
        // Tray icon constants
        private const int NIM_ADD = 0x00000000;
        private const int NIM_DELETE = 0x00000002;
        private const int NIM_MODIFY = 0x00000001;
        private const int NIF_ICON = 0x00000002;
        private const int NIF_MESSAGE = 0x00000001;
        private const int NIF_TIP = 0x00000004;
        private const int NIF_STATE = 0x00000008;
        private const int NIF_INFO = 0x00000010;
        private const int WM_LBUTTONDBLCLK = 0x0203;
        private const int WM_RBUTTONUP = 0x0205;
        
        private IntPtr _trayIconHandle;
        private bool _trayIconVisible = false;
        private const string STARTUP_REGISTRY_KEY = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string APP_NAME = "Highlighter4";

        public event EventHandler? HotkeyPressed;
        public event EventHandler? CaptureModeRequested;
        public event EventHandler? ScrollCaptureRequested;
        public event EventHandler? RegionCaptureRequested;
        public event EventHandler? GifRecordingRequested;
        
        public void TriggerCaptureModeWithOverlay(HighlighterWindow overlayWindow)
        {
            // Create capture window with overlay content
            var captureWindow = new CaptureWindow(overlayWindow);
            captureWindow.Show();
        }

        public MainWindow()
        {
            InitializeComponent();
            
            // Create icon file for the executable
            CreateIconFile();
            
            // Check if this is the first run and add to startup
            CheckAndAddToStartup();
            
            // Hide the main window - we only need the tray icon
            this.WindowState = WindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visibility = Visibility.Hidden;
        }

        private void InitializeTrayIcon()
        {
            try
            {
                var windowInteropHelper = new System.Windows.Interop.WindowInteropHelper(this);
                IntPtr hWnd = windowInteropHelper.Handle;
                
                System.Diagnostics.Debug.WriteLine($"Window handle: {hWnd}");
                
                // Create system warning icon (yellow)
                Icon customIcon = CreateRedCircleIcon();
                System.Diagnostics.Debug.WriteLine($"Icon created, handle: {customIcon.Handle}");
                
                // Create tray icon data structure
                var iconData = new NOTIFYICONDATA
                {
                    cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                    hWnd = hWnd,
                    uID = 1,
                    uFlags = NIF_ICON | NIF_MESSAGE | NIF_TIP,
                    uCallbackMessage = WM_TRAYICON,
                    hIcon = customIcon.Handle,
                    szTip = "Highlighter4 - Alt+X=Highlight, Ctrl+NumpadDot=GIF Recording\0"
                };
                
                System.Diagnostics.Debug.WriteLine($"About to call Shell_NotifyIcon with flags: {iconData.uFlags}");
                
                // Add tray icon
                bool result = Shell_NotifyIcon(NIM_ADD, ref iconData);
                System.Diagnostics.Debug.WriteLine($"Shell_NotifyIcon result: {result}");
                
                if (result)
                {
                    _trayIconVisible = true;
                    _trayIconHandle = customIcon.Handle;
                    System.Diagnostics.Debug.WriteLine("Tray icon successfully added");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Failed to add tray icon");
                    // Try again after a short delay
                    System.Threading.Tasks.Task.Delay(1000).ContinueWith(_ => EnsureTrayIconVisible());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Could not create tray icon: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                // Try again after a short delay
                System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ => EnsureTrayIconVisible());
            }
        }

        private void EnsureTrayIconVisible()
        {
            if (!_trayIconVisible)
            {
                System.Diagnostics.Debug.WriteLine("Tray icon not visible, attempting to reinitialize...");
                InitializeTrayIcon();
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // Initialize tray icon now that window handle is available
            InitializeTrayIcon();
            
            // Register hotkeys
            var windowInteropHelper = new System.Windows.Interop.WindowInteropHelper(this);
            if (!RegisterHotKey(windowInteropHelper.Handle, HOTKEY_ID, 1 | 4, 0x58)) // 1=MOD_ALT, 4=MOD_SHIFT, 0x58=VK_X
            {
                MessageBox.Show("Unable to register Shift+Alt+X hotkey. The program might already be running.", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            
            if (!RegisterHotKey(windowInteropHelper.Handle, HOTKEY_HIGHLIGHT_ID, 1, 0x58)) // 1=MOD_ALT, 0x58=VK_X
            {
                MessageBox.Show("Unable to register Alt+X hotkey.", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            
            if (!RegisterHotKey(windowInteropHelper.Handle, HOTKEY_SCROLL_CAPTURE_ID, 2, 0x60)) // 2=MOD_CONTROL, 0x60=VK_NUMPAD0
            {
                MessageBox.Show("Unable to register Ctrl+Numpad0 hotkey.", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            
            if (!RegisterHotKey(windowInteropHelper.Handle, HOTKEY_REGION_CAPTURE_ID, 2, 0x6E)) // 2=MOD_CONTROL, 0x6E=VK_DECIMAL (NumpadDot)
            {
                MessageBox.Show("Unable to register Ctrl+NumpadDot hotkey.", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            
            // Set up message hook
            var source = System.Windows.Interop.HwndSource.FromHwnd(windowInteropHelper.Handle);
            source?.AddHook(HwndHook);
            
            // Start periodic tray icon check
            StartTrayIconMonitor();
        }

        private void StartTrayIconMonitor()
        {
            // Check every 30 seconds to ensure tray icon is visible
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(30);
            timer.Tick += (s, e) => EnsureTrayIconVisible();
            timer.Start();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            
            // Keep the window hidden and not in taskbar
            if (this.WindowState != WindowState.Minimized)
            {
                this.WindowState = WindowState.Minimized;
            }
            
            if (this.ShowInTaskbar)
            {
                this.ShowInTaskbar = false;
            }
            
            if (this.Visibility == Visibility.Visible)
            {
                this.Visibility = Visibility.Hidden;
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                // Check if Shift and Alt are still held down
                var isShiftDown = (GetKeyState(0x10) & 0x8000) != 0;
                var isAltDown = (GetKeyState(0x12) & 0x8000) != 0;
                
                if (isShiftDown && isAltDown)
                {
                    // Shift+Alt+X: Toggle capture mode when overlay is active, otherwise show highlight
                    HotkeyPressed?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    // Alt+X: Show highlight
                    HotkeyPressed?.Invoke(this, EventArgs.Empty);
                }
                handled = true;
            }
            else if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_HIGHLIGHT_ID)
            {
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
                handled = true;
            }
            else if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_SCROLL_CAPTURE_ID)
            {
                ScrollCaptureRequested?.Invoke(this, EventArgs.Empty);
                handled = true;
            }
            else if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_REGION_CAPTURE_ID)
            {
                // Trigger GIF recording (start/stop toggle)
                GifRecordingRequested?.Invoke(this, EventArgs.Empty);
                handled = true;
            }
            else if (msg == WM_TRAYICON)
            {
                if (lParam.ToInt32() == WM_LBUTTONDBLCLK)
                {
                    CaptureModeRequested?.Invoke(this, EventArgs.Empty);
                    handled = true;
                }
                else if (lParam.ToInt32() == WM_RBUTTONUP)
                {
                    ShowContextMenu();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        private void ShowContextMenu()
        {
            // Make this window visible momentarily to host the ContextMenu
            this.Show();
            this.WindowState = WindowState.Normal;
            this.ShowInTaskbar = false;
            this.Opacity = 0; // Invisible but can host ContextMenu
            this.Width = 1;
            this.Height = 1;
            this.Topmost = true; // Ensure it's on top
            
            // Position window near the cursor
            var cursorPos = System.Windows.Forms.Cursor.Position;
            this.Left = cursorPos.X;
            this.Top = cursorPos.Y;
            
            // Activate the window to ensure it has focus
            this.Activate();
            this.Focus();
            
            var contextMenu = new System.Windows.Controls.ContextMenu();
            
            // Close menu when it loses focus
            contextMenu.StaysOpen = false;
            contextMenu.PlacementTarget = this;
            contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
            
            // Close and hide window when menu closes
            contextMenu.Closed += (s, e) => {
                contextMenu.IsOpen = false;
                this.Hide();
            };
            
            // Close menu when Escape is pressed
            contextMenu.PreviewKeyDown += (s, e) => {
                if (e.Key == System.Windows.Input.Key.Escape)
                {
                    contextMenu.IsOpen = false;
                    this.Hide();
                    e.Handled = true;
                }
            };
            
            var highlightItem = new System.Windows.Controls.MenuItem();
            highlightItem.Header = "Show Highlight (Alt+X)";
            highlightItem.Click += (s, e) => HotkeyPressed?.Invoke(this, EventArgs.Empty);
            contextMenu.Items.Add(highlightItem);
            
            var captureItem = new System.Windows.Controls.MenuItem();
            captureItem.Header = "Capture Region (C when overlay active)";
            captureItem.Click += (s, e) => {
                // First show highlight, then user can press C to capture
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
            };
            contextMenu.Items.Add(captureItem);
            
            var scrollCaptureItem = new System.Windows.Controls.MenuItem();
            scrollCaptureItem.Header = "Scroll Capture (Ctrl+Numpad0)";
            scrollCaptureItem.Click += (s, e) => {
                ScrollCaptureRequested?.Invoke(this, EventArgs.Empty);
            };
            contextMenu.Items.Add(scrollCaptureItem);
            
            contextMenu.Items.Add(new System.Windows.Controls.Separator());
            
            // Drawing Tools
            var drawingToolsItem = new System.Windows.Controls.MenuItem();
            drawingToolsItem.Header = "🎨 Drawing Tools";
            contextMenu.Items.Add(drawingToolsItem);
            
            // Arrow Tool
            var arrowToolItem = new System.Windows.Controls.MenuItem();
            arrowToolItem.Header = "▶ Arrow Tool (F1)";
            arrowToolItem.ToolTip = "Select and move elements";
            arrowToolItem.Click += (s, e) => {
                // TODO: Implement arrow tool functionality
            };
            drawingToolsItem.Items.Add(arrowToolItem);
            
            // Line Tool
            var lineToolItem = new System.Windows.Controls.MenuItem();
            lineToolItem.Header = "➖ Line Tool (F2)";
            lineToolItem.ToolTip = "Draw straight lines";
            lineToolItem.Click += (s, e) => {
                // TODO: Implement line tool functionality
            };
            drawingToolsItem.Items.Add(lineToolItem);
            
            // Rectangle Tool
            var rectangleToolItem = new System.Windows.Controls.MenuItem();
            rectangleToolItem.Header = "⬜ Rectangle Tool (F3)";
            rectangleToolItem.ToolTip = "Draw rectangles and squares";
            rectangleToolItem.Click += (s, e) => {
                // TODO: Implement rectangle tool functionality
            };
            drawingToolsItem.Items.Add(rectangleToolItem);
            
            // Highlighter Tool
            var highlighterToolItem = new System.Windows.Controls.MenuItem();
            highlighterToolItem.Header = "🖍 Highlighter Tool (F4)";
            highlighterToolItem.ToolTip = "Highlight text and areas";
            highlighterToolItem.Click += (s, e) => {
                // TODO: Implement highlighter tool functionality
            };
            drawingToolsItem.Items.Add(highlighterToolItem);
            
            // Blur Tool
            var blurToolItem = new System.Windows.Controls.MenuItem();
            blurToolItem.Header = "🌫 Blur Tool (F5)";
            blurToolItem.ToolTip = "Blur sensitive information";
            blurToolItem.Click += (s, e) => {
                // TODO: Implement blur tool functionality
            };
            drawingToolsItem.Items.Add(blurToolItem);
            
            // Speech Balloon Tool
            var speechBalloonToolItem = new System.Windows.Controls.MenuItem();
            speechBalloonToolItem.Header = "💬 Speech Balloon Tool (F6)";
            speechBalloonToolItem.ToolTip = "Add speech bubbles and annotations";
            speechBalloonToolItem.Click += (s, e) => {
                // TODO: Implement speech balloon tool functionality
            };
            drawingToolsItem.Items.Add(speechBalloonToolItem);
            
            // Crop Tool
            var cropToolItem = new System.Windows.Controls.MenuItem();
            cropToolItem.Header = "✂️ Crop Tool (F7)";
            cropToolItem.ToolTip = "Crop and resize images";
            cropToolItem.Click += (s, e) => {
                // TODO: Implement crop tool functionality
            };
            drawingToolsItem.Items.Add(cropToolItem);
            
            contextMenu.Items.Add(new System.Windows.Controls.Separator());
            
            // Settings
            var settingsItem = new System.Windows.Controls.MenuItem();
            settingsItem.Header = "⚙️ Settings";
            settingsItem.Click += (s, e) => {
                var settingsWindow = new SettingsWindow();
                settingsWindow.Show();
            };
            contextMenu.Items.Add(settingsItem);
            
            // Startup options
            var startupItem = new System.Windows.Controls.MenuItem();
            startupItem.Header = "Startup Options";
            contextMenu.Items.Add(startupItem);
            
            var enableStartupItem = new System.Windows.Controls.MenuItem();
            enableStartupItem.Header = "Start with Windows";
            enableStartupItem.IsCheckable = true;
            enableStartupItem.IsChecked = IsStartupEnabled();
            enableStartupItem.Click += (s, e) => {
                if (IsStartupEnabled())
                {
                    RemoveFromStartup();
                }
                else
                {
                    AddToStartup();
                }
            };
            startupItem.Items.Add(enableStartupItem);
            
            contextMenu.Items.Add(new System.Windows.Controls.Separator());
            
            var exitItem = new System.Windows.Controls.MenuItem();
            exitItem.Header = "Exit";
            exitItem.Click += (s, e) => Application.Current.Shutdown();
            contextMenu.Items.Add(exitItem);
            
            contextMenu.IsOpen = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            var windowInteropHelper = new System.Windows.Interop.WindowInteropHelper(this);
            UnregisterHotKey(windowInteropHelper.Handle, HOTKEY_ID);
            UnregisterHotKey(windowInteropHelper.Handle, HOTKEY_HIGHLIGHT_ID);
            UnregisterHotKey(windowInteropHelper.Handle, HOTKEY_SCROLL_CAPTURE_ID);
            UnregisterHotKey(windowInteropHelper.Handle, HOTKEY_REGION_CAPTURE_ID);
            
            // Remove tray icon
            if (_trayIconVisible)
            {
                var iconData = new NOTIFYICONDATA
                {
                    cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                    hWnd = windowInteropHelper.Handle,
                    uID = 1
                };
                Shell_NotifyIcon(NIM_DELETE, ref iconData);
                _trayIconVisible = false;
            }
            
            base.OnClosed(e);
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            // Hide the window immediately if it becomes activated
            this.Hide();
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("shell32.dll")]
        private static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);
        
        private void CreateIconFile()
        {
            try
            {
                // No need to create icon file - using system icon
                System.Diagnostics.Debug.WriteLine("Using system icon");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Could not access icon: {ex.Message}");
            }
        }
        
        private Icon CreateRedCircleIcon()
        {
            try
            {
                // Try to load Lambda.ico
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lambda.ico");
                
                if (System.IO.File.Exists(iconPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Loading Lambda.ico from: {iconPath}");
                    return new Icon(iconPath, 16, 16);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Lambda.ico not found at: {iconPath}");
                    return SystemIcons.Application;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Could not load Lambda.ico: {ex.Message}");
                
                // Fallback: use application icon
                return SystemIcons.Application;
            }
        }
        
        private void CheckAndAddToStartup()
        {
            try
            {
                // Check if this is the first run by looking for a marker file
                string markerFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Highlighter4", "first_run.txt");
                string markerDir = Path.GetDirectoryName(markerFile);
                
                if (!Directory.Exists(markerDir))
                {
                    Directory.CreateDirectory(markerDir);
                }
                
                if (!File.Exists(markerFile))
                {
                    // First run - add to startup
                    AddToStartup();
                    
                    // Create marker file
                    File.WriteAllText(markerFile, DateTime.Now.ToString());
                    
                    System.Diagnostics.Debug.WriteLine("First run detected - added to Windows startup");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking first run: {ex.Message}");
            }
        }
        
        private void AddToStartup()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(STARTUP_REGISTRY_KEY, true))
                {
                    if (key != null)
                    {
                        // Get the actual executable path, not the DLL
                        string executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                        
                        // Wrap in quotes to handle paths with spaces
                        string quotedPath = $"\"{executablePath}\"";
                        
                        key.SetValue(APP_NAME, quotedPath);
                        System.Diagnostics.Debug.WriteLine($"Added to Windows startup: {quotedPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding to startup: {ex.Message}");
            }
        }
        
        private void RemoveFromStartup()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(STARTUP_REGISTRY_KEY, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(APP_NAME, false);
                        System.Diagnostics.Debug.WriteLine("Removed from Windows startup");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing from startup: {ex.Message}");
            }
        }
        
        private bool IsStartupEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(STARTUP_REGISTRY_KEY, false))
                {
                    if (key != null)
                    {
                        return key.GetValue(APP_NAME) != null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking startup status: {ex.Message}");
            }
            return false;
        }
        
        public void UpdateTrayIconWithProgress(int progress)
        {
            try
            {
                // Create icon with progress percentage
                var icon = CreateProgressIcon(progress);
                
                if (icon != null && _trayIconVisible)
                {
                    var iconData = new NOTIFYICONDATA
                    {
                        cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                        hWnd = new System.Windows.Interop.WindowInteropHelper(this).Handle,
                        uID = 1,
                        uFlags = 0x00000002 | 0x00000010, // NIF_ICON | NIF_TIP
                        hIcon = icon.Handle,
                        szTip = $"Highlighter4 - Rendering GIF: {progress}%"
                    };
                    
                    Shell_NotifyIcon(NIM_MODIFY, ref iconData);
                    
                    icon.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating tray icon with progress: {ex.Message}");
            }
        }
        
        public void ResetTrayIcon()
        {
            try
            {
                // Restore normal icon
                var icon = CreateRedCircleIcon();
                
                if (icon != null && _trayIconVisible)
                {
                    var iconData = new NOTIFYICONDATA
                    {
                        cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                        hWnd = new System.Windows.Interop.WindowInteropHelper(this).Handle,
                        uID = 1,
                        uFlags = 0x00000002 | 0x00000010, // NIF_ICON | NIF_TIP
                        hIcon = icon.Handle,
                        szTip = "Highlighter4"
                    };
                    
                    Shell_NotifyIcon(NIM_MODIFY, ref iconData);
                    
                    icon.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error resetting tray icon: {ex.Message}");
            }
        }
        
        private System.Drawing.Icon CreateProgressIcon(int progress)
        {
            try
            {
                // Create a 16x16 bitmap
                var bitmap = new System.Drawing.Bitmap(16, 16);
                using (var g = System.Drawing.Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.Clear(System.Drawing.Color.Transparent);
                    
                    // Draw progress text (no circle)
                    string progressText = progress.ToString();
                    float fontSize = progress >= 100 ? 8.5f : (progressText.Length >= 2 ? 9f : 10f);
                    using (var font = new System.Drawing.Font("Arial", fontSize, System.Drawing.FontStyle.Bold))
                    {
                        var textSize = g.MeasureString(progressText, font);
                        var x = (16 - textSize.Width) / 2;
                        var y = (16 - textSize.Height) / 2;
                        
                        // Draw text shadow for better contrast
                        g.DrawString(progressText, font, System.Drawing.Brushes.Black, x + 1, y + 1);
                        
                        // Draw text
                        g.DrawString(progressText, font, System.Drawing.Brushes.White, x, y);
                    }
                }
                
                // Convert to Icon
                IntPtr hIcon = bitmap.GetHicon();
                System.Drawing.Icon icon = System.Drawing.Icon.FromHandle(hIcon);
                
                return icon;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating progress icon: {ex.Message}");
                return System.Drawing.SystemIcons.Warning;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public int dwState;
        public int dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public int uTimeout;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public int dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }
}
