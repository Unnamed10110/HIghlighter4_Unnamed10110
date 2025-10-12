using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace Highlighter4
{
    public partial class SettingsWindow : Window
    {
        private AppSettings settings;
        
        // Control references for saving
        private CheckBox startWithWindowsCheck;
        private CheckBox showTrayIconCheck;
        private ComboBox languageCombo;
        private TextBox savePathText;
        private CheckBox organizeByDateCheck;
        
        private CheckBox autoSaveCheck;
        private CheckBox copyToClipboardCheck;
        private CheckBox openInEditorCheck;
        private CheckBox playSoundCheck;
        private ComboBox formatCombo;
        
        private CheckBox autoFocusCheck;
        private CheckBox closeAfterSaveCheck;
        private CheckBox showGridCheck;
        
        private CheckBox showNotificationsCheck;
        private CheckBox showThumbnailCheck;
        private CheckBox clickToOpenCheck;
        private ComboBox durationCombo;
        
        private CheckBox hardwareAccelCheck;
        private CheckBox cacheImagesCheck;
        private CheckBox enableLoggingCheck;
        private CheckBox showConsoleCheck;
        
        public SettingsWindow()
        {
            InitializeComponent();
            settings = AppSettings.Instance;
            LoadGeneralSettings();
        }

        private void ClearActiveTab()
        {
            GeneralTab.Style = (Style)FindResource("SidebarButton");
            HotkeysTab.Style = (Style)FindResource("SidebarButton");
            CaptureTab.Style = (Style)FindResource("SidebarButton");
            EditorTab.Style = (Style)FindResource("SidebarButton");
            NotificationsTab.Style = (Style)FindResource("SidebarButton");
            AdvancedTab.Style = (Style)FindResource("SidebarButton");
            AboutTab.Style = (Style)FindResource("SidebarButton");
        }

        private void GeneralTab_Click(object sender, RoutedEventArgs e)
        {
            ClearActiveTab();
            GeneralTab.Style = (Style)FindResource("ActiveSidebarButton");
            LoadGeneralSettings();
        }

        private void HotkeysTab_Click(object sender, RoutedEventArgs e)
        {
            ClearActiveTab();
            HotkeysTab.Style = (Style)FindResource("ActiveSidebarButton");
            LoadHotkeysSettings();
        }

        private void CaptureTab_Click(object sender, RoutedEventArgs e)
        {
            ClearActiveTab();
            CaptureTab.Style = (Style)FindResource("ActiveSidebarButton");
            LoadCaptureSettings();
        }

        private void EditorTab_Click(object sender, RoutedEventArgs e)
        {
            ClearActiveTab();
            EditorTab.Style = (Style)FindResource("ActiveSidebarButton");
            LoadEditorSettings();
        }

        private void NotificationsTab_Click(object sender, RoutedEventArgs e)
        {
            ClearActiveTab();
            NotificationsTab.Style = (Style)FindResource("ActiveSidebarButton");
            LoadNotificationsSettings();
        }

        private void AdvancedTab_Click(object sender, RoutedEventArgs e)
        {
            ClearActiveTab();
            AdvancedTab.Style = (Style)FindResource("ActiveSidebarButton");
            LoadAdvancedSettings();
        }

        private void AboutTab_Click(object sender, RoutedEventArgs e)
        {
            ClearActiveTab();
            AboutTab.Style = (Style)FindResource("ActiveSidebarButton");
            LoadAboutInfo();
        }

        #region Settings Panels

        private void LoadGeneralSettings()
        {
            ContentPanel.Children.Clear();

            var title = new TextBlock
            {
                Text = "General Settings",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 68))
            };
            ContentPanel.Children.Add(title);

            // Startup Settings
            var startupGroup = new GroupBox
            {
                Header = "Startup",
                Margin = new Thickness(0, 0, 0, 15)
            };
            var startupStack = new StackPanel();
            
            startWithWindowsCheck = new CheckBox
            {
                Content = "Start with Windows",
                IsChecked = settings.StartWithWindows
            };
            startupStack.Children.Add(startWithWindowsCheck);
            
            showTrayIconCheck = new CheckBox
            {
                Content = "Show tray icon",
                IsChecked = settings.ShowTrayIcon
            };
            startupStack.Children.Add(showTrayIconCheck);
            
            startupGroup.Content = startupStack;
            ContentPanel.Children.Add(startupGroup);

            // Language Settings
            var languageGroup = new GroupBox
            {
                Header = "Language",
                Margin = new Thickness(0, 0, 0, 15)
            };
            var languageStack = new StackPanel();
            
            var langLabel = new TextBlock
            {
                Text = "Interface Language:",
                Margin = new Thickness(0, 0, 0, 5)
            };
            languageStack.Children.Add(langLabel);
            
            languageCombo = new ComboBox
            {
                Width = 200,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            languageCombo.Items.Add("English");
            languageCombo.Items.Add("Español");
            languageCombo.SelectedItem = settings.Language;
            if (languageCombo.SelectedItem == null)
                languageCombo.SelectedIndex = 0;
            languageStack.Children.Add(languageCombo);
            
            languageGroup.Content = languageStack;
            ContentPanel.Children.Add(languageGroup);

            // Save Location
            var saveGroup = new GroupBox
            {
                Header = "Save Location",
                Margin = new Thickness(0, 0, 0, 15)
            };
            var saveStack = new StackPanel();
            
            var saveLabel = new TextBlock
            {
                Text = "Default save folder:",
                Margin = new Thickness(0, 0, 0, 5)
            };
            saveStack.Children.Add(saveLabel);
            
            var savePathStack = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            
            savePathText = new TextBox
            {
                Text = settings.SaveLocation,
                Width = 350,
                IsReadOnly = false,
                Margin = new Thickness(0, 0, 10, 0)
            };
            savePathStack.Children.Add(savePathText);
            
            var browseButton = new Button
            {
                Content = "Browse...",
                Padding = new Thickness(15, 5, 15, 5)
            };
            browseButton.Click += BrowseButton_Click;
            savePathStack.Children.Add(browseButton);
            
            saveStack.Children.Add(savePathStack);
            
            organizeByDateCheck = new CheckBox
            {
                Content = "Organize by date (dd-MM-yyyy folders)",
                IsChecked = settings.OrganizeByDate,
                Margin = new Thickness(0, 10, 0, 0)
            };
            saveStack.Children.Add(organizeByDateCheck);
            
            saveGroup.Content = saveStack;
            ContentPanel.Children.Add(saveGroup);

            // Buttons
            var buttonStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };
            
            var saveButton = new Button
            {
                Content = "💾 Save Changes",
                Padding = new Thickness(20, 8, 20, 8),
                Margin = new Thickness(0, 0, 10, 0)
            };
            saveButton.Click += SaveGeneralSettings_Click;
            buttonStack.Children.Add(saveButton);
            
            var cancelButton = new Button
            {
                Content = "❌ Cancel",
                Padding = new Thickness(20, 8, 20, 8)
            };
            cancelButton.Click += (s, e) => this.Close();
            buttonStack.Children.Add(cancelButton);
            
            ContentPanel.Children.Add(buttonStack);
        }
        
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select default save folder",
                ShowNewFolderButton = true
            };
            
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                savePathText.Text = dialog.SelectedPath;
            }
        }
        
        private void SaveGeneralSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                settings.StartWithWindows = startWithWindowsCheck.IsChecked ?? true;
                settings.ShowTrayIcon = showTrayIconCheck.IsChecked ?? true;
                settings.Language = languageCombo.SelectedItem?.ToString() ?? "English";
                settings.SaveLocation = savePathText.Text;
                settings.OrganizeByDate = organizeByDateCheck.IsChecked ?? true;
                
                settings.Save();
                
                // Update startup registry if needed
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (settings.StartWithWindows && mainWindow != null)
                {
                    // Call AddToStartup via reflection or public method
                    var method = mainWindow.GetType().GetMethod("AddToStartup", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    method?.Invoke(mainWindow, null);
                }
                else if (!settings.StartWithWindows && mainWindow != null)
                {
                    var method = mainWindow.GetType().GetMethod("RemoveFromStartup", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    method?.Invoke(mainWindow, null);
                }
                
                MessageBox.Show("General settings saved successfully!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadHotkeysSettings()
        {
            ContentPanel.Children.Clear();

            var title = new TextBlock
            {
                Text = "Hotkeys Settings",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 68))
            };
            ContentPanel.Children.Add(title);

            var description = new TextBlock
            {
                Text = "Configure global hotkeys for quick access to features",
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                Margin = new Thickness(0, 0, 0, 20)
            };
            ContentPanel.Children.Add(description);

            // Hotkeys List
            var hotkeysGroup = new GroupBox
            {
                Header = "Global Hotkeys",
                Margin = new Thickness(0, 0, 0, 15)
            };
            var hotkeysStack = new StackPanel();
            
            AddHotkeyRow(hotkeysStack, "Highlight Mode:", "Alt + X");
            AddHotkeyRow(hotkeysStack, "Capture Mode:", "Shift + Alt + X");
            AddHotkeyRow(hotkeysStack, "Quick Capture:", "Ctrl + Numpad .");
            AddHotkeyRow(hotkeysStack, "Scroll Capture:", "Ctrl + Numpad 0");
            
            hotkeysGroup.Content = hotkeysStack;
            ContentPanel.Children.Add(hotkeysGroup);

            var warningText = new TextBlock
            {
                Text = "⚠️ Note: Hotkey customization will be available in a future update",
                Foreground = new SolidColorBrush(Color.FromRgb(255, 200, 0)),
                Margin = new Thickness(0, 10, 0, 0),
                FontStyle = FontStyles.Italic
            };
            ContentPanel.Children.Add(warningText);
        }

        private void AddHotkeyRow(StackPanel parent, string label, string hotkey)
        {
            var row = new Grid
            {
                Margin = new Thickness(0, 8, 0, 8)
            };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var labelText = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(labelText, 0);
            row.Children.Add(labelText);

            var hotkeyButton = new Button
            {
                Content = hotkey,
                Width = 200,
                IsEnabled = false
            };
            Grid.SetColumn(hotkeyButton, 1);
            row.Children.Add(hotkeyButton);

            parent.Children.Add(row);
        }

        private void LoadCaptureSettings()
        {
            ContentPanel.Children.Clear();

            var title = new TextBlock
            {
                Text = "Capture Settings",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 68))
            };
            ContentPanel.Children.Add(title);

            // Capture Options
            var captureGroup = new GroupBox
            {
                Header = "Capture Options",
                Margin = new Thickness(0, 0, 0, 15)
            };
            var captureStack = new StackPanel();
            
            autoSaveCheck = new CheckBox
            {
                Content = "Auto-save captures",
                IsChecked = settings.AutoSaveCaptures
            };
            captureStack.Children.Add(autoSaveCheck);
            
            copyToClipboardCheck = new CheckBox
            {
                Content = "Copy to clipboard automatically",
                IsChecked = settings.CopyToClipboard
            };
            captureStack.Children.Add(copyToClipboardCheck);
            
            openInEditorCheck = new CheckBox
            {
                Content = "Open in editor after capture",
                IsChecked = settings.OpenInEditor
            };
            captureStack.Children.Add(openInEditorCheck);
            
            playSoundCheck = new CheckBox
            {
                Content = "Play sound on capture",
                IsChecked = settings.PlaySound
            };
            captureStack.Children.Add(playSoundCheck);
            
            captureGroup.Content = captureStack;
            ContentPanel.Children.Add(captureGroup);

            // Image Format
            var formatGroup = new GroupBox
            {
                Header = "Image Format",
                Margin = new Thickness(0, 0, 0, 15)
            };
            var formatStack = new StackPanel();
            
            var formatLabel = new TextBlock
            {
                Text = "Default format:",
                Margin = new Thickness(0, 0, 0, 5)
            };
            formatStack.Children.Add(formatLabel);
            
            formatCombo = new ComboBox
            {
                Width = 200,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            formatCombo.Items.Add("PNG (Recommended)");
            formatCombo.Items.Add("JPEG");
            formatCombo.Items.Add("BMP");
            
            // Select current format
            string currentFormat = settings.ImageFormat.ToUpper();
            if (currentFormat.Contains("PNG"))
                formatCombo.SelectedIndex = 0;
            else if (currentFormat.Contains("JPEG") || currentFormat.Contains("JPG"))
                formatCombo.SelectedIndex = 1;
            else if (currentFormat.Contains("BMP"))
                formatCombo.SelectedIndex = 2;
            else
                formatCombo.SelectedIndex = 0;
                
            formatStack.Children.Add(formatCombo);
            
            formatGroup.Content = formatStack;
            ContentPanel.Children.Add(formatGroup);
            
            // Buttons
            var buttonStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };
            
            var saveButton = new Button
            {
                Content = "💾 Save Changes",
                Padding = new Thickness(20, 8, 20, 8),
                Margin = new Thickness(0, 0, 10, 0)
            };
            saveButton.Click += SaveCaptureSettings_Click;
            buttonStack.Children.Add(saveButton);
            
            var cancelButton = new Button
            {
                Content = "❌ Cancel",
                Padding = new Thickness(20, 8, 20, 8)
            };
            cancelButton.Click += (s, e) => this.Close();
            buttonStack.Children.Add(cancelButton);
            
            ContentPanel.Children.Add(buttonStack);
        }
        
        private void SaveCaptureSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                settings.AutoSaveCaptures = autoSaveCheck.IsChecked ?? true;
                settings.CopyToClipboard = copyToClipboardCheck.IsChecked ?? true;
                settings.OpenInEditor = openInEditorCheck.IsChecked ?? true;
                settings.PlaySound = playSoundCheck.IsChecked ?? true;
                
                // Parse format from combo
                string selectedFormat = formatCombo.SelectedItem?.ToString() ?? "PNG (Recommended)";
                if (selectedFormat.Contains("PNG"))
                    settings.ImageFormat = "PNG";
                else if (selectedFormat.Contains("JPEG"))
                    settings.ImageFormat = "JPEG";
                else if (selectedFormat.Contains("BMP"))
                    settings.ImageFormat = "BMP";
                
                settings.Save();
                
                MessageBox.Show("Capture settings saved successfully!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadEditorSettings()
        {
            ContentPanel.Children.Clear();

            var title = new TextBlock
            {
                Text = "Editor Settings",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 68))
            };
            ContentPanel.Children.Add(title);

            // Drawing Tools
            var toolsGroup = new GroupBox
            {
                Header = "Drawing Tools",
                Margin = new Thickness(0, 0, 0, 15)
            };
            var toolsStack = new StackPanel();
            
            var defaultColor = new TextBlock
            {
                Text = $"Default drawing color: {settings.DefaultDrawingColor}",
                Margin = new Thickness(0, 5, 0, 5)
            };
            toolsStack.Children.Add(defaultColor);
            
            var defaultThickness = new TextBlock
            {
                Text = $"Default line thickness: {settings.DefaultLineThickness}px",
                Margin = new Thickness(0, 5, 0, 5)
            };
            toolsStack.Children.Add(defaultThickness);
            
            var noteText = new TextBlock
            {
                Text = "⚠️ Note: Tool customization will be available in a future update",
                Foreground = new SolidColorBrush(Color.FromRgb(255, 200, 0)),
                Margin = new Thickness(0, 10, 0, 0),
                FontStyle = FontStyles.Italic,
                FontSize = 12
            };
            toolsStack.Children.Add(noteText);
            
            toolsGroup.Content = toolsStack;
            ContentPanel.Children.Add(toolsGroup);

            // Editor Behavior
            var behaviorGroup = new GroupBox
            {
                Header = "Editor Behavior",
                Margin = new Thickness(0, 0, 0, 15)
            };
            var behaviorStack = new StackPanel();
            
            autoFocusCheck = new CheckBox
            {
                Content = "Auto-focus editor on open",
                IsChecked = settings.AutoFocusEditor
            };
            behaviorStack.Children.Add(autoFocusCheck);
            
            closeAfterSaveCheck = new CheckBox
            {
                Content = "Close editor after saving",
                IsChecked = settings.CloseAfterSave
            };
            behaviorStack.Children.Add(closeAfterSaveCheck);
            
            showGridCheck = new CheckBox
            {
                Content = "Show grid in editor",
                IsChecked = settings.ShowGrid
            };
            behaviorStack.Children.Add(showGridCheck);
            
            behaviorGroup.Content = behaviorStack;
            ContentPanel.Children.Add(behaviorGroup);
            
            // Buttons
            var buttonStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };
            
            var saveButton = new Button
            {
                Content = "💾 Save Changes",
                Padding = new Thickness(20, 8, 20, 8),
                Margin = new Thickness(0, 0, 10, 0)
            };
            saveButton.Click += SaveEditorSettings_Click;
            buttonStack.Children.Add(saveButton);
            
            var cancelButton = new Button
            {
                Content = "❌ Cancel",
                Padding = new Thickness(20, 8, 20, 8)
            };
            cancelButton.Click += (s, e) => this.Close();
            buttonStack.Children.Add(cancelButton);
            
            ContentPanel.Children.Add(buttonStack);
        }
        
        private void SaveEditorSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                settings.AutoFocusEditor = autoFocusCheck.IsChecked ?? true;
                settings.CloseAfterSave = closeAfterSaveCheck.IsChecked ?? false;
                settings.ShowGrid = showGridCheck.IsChecked ?? false;
                
                settings.Save();
                
                MessageBox.Show("Editor settings saved successfully!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadNotificationsSettings()
        {
            ContentPanel.Children.Clear();

            var title = new TextBlock
            {
                Text = "Notifications Settings",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 68))
            };
            ContentPanel.Children.Add(title);

            // Notification Options
            var notifGroup = new GroupBox
            {
                Header = "Notification Options",
                Margin = new Thickness(0, 0, 0, 15)
            };
            var notifStack = new StackPanel();
            
            showNotificationsCheck = new CheckBox
            {
                Content = "Show notifications on save",
                IsChecked = settings.ShowNotifications
            };
            notifStack.Children.Add(showNotificationsCheck);
            
            showThumbnailCheck = new CheckBox
            {
                Content = "Show image thumbnail in notification",
                IsChecked = settings.ShowThumbnail
            };
            notifStack.Children.Add(showThumbnailCheck);
            
            clickToOpenCheck = new CheckBox
            {
                Content = "Click notification to open image",
                IsChecked = settings.ClickToOpen
            };
            notifStack.Children.Add(clickToOpenCheck);
            
            notifGroup.Content = notifStack;
            ContentPanel.Children.Add(notifGroup);

            // Duration
            var durationGroup = new GroupBox
            {
                Header = "Display Duration",
                Margin = new Thickness(0, 0, 0, 15)
            };
            var durationStack = new StackPanel();
            
            var durationLabel = new TextBlock
            {
                Text = "Notification duration:",
                Margin = new Thickness(0, 0, 0, 5)
            };
            durationStack.Children.Add(durationLabel);
            
            durationCombo = new ComboBox
            {
                Width = 200,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            durationCombo.Items.Add("3 seconds");
            durationCombo.Items.Add("6 seconds");
            durationCombo.Items.Add("10 seconds");
            
            // Select current duration
            int duration = settings.NotificationDuration;
            if (duration <= 3)
                durationCombo.SelectedIndex = 0;
            else if (duration <= 6)
                durationCombo.SelectedIndex = 1;
            else
                durationCombo.SelectedIndex = 2;
                
            durationStack.Children.Add(durationCombo);
            
            durationGroup.Content = durationStack;
            ContentPanel.Children.Add(durationGroup);
            
            // Buttons
            var buttonStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };
            
            var saveButton = new Button
            {
                Content = "💾 Save Changes",
                Padding = new Thickness(20, 8, 20, 8),
                Margin = new Thickness(0, 0, 10, 0)
            };
            saveButton.Click += SaveNotificationsSettings_Click;
            buttonStack.Children.Add(saveButton);
            
            var cancelButton = new Button
            {
                Content = "❌ Cancel",
                Padding = new Thickness(20, 8, 20, 8)
            };
            cancelButton.Click += (s, e) => this.Close();
            buttonStack.Children.Add(cancelButton);
            
            ContentPanel.Children.Add(buttonStack);
        }
        
        private void SaveNotificationsSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                settings.ShowNotifications = showNotificationsCheck.IsChecked ?? true;
                settings.ShowThumbnail = showThumbnailCheck.IsChecked ?? true;
                settings.ClickToOpen = clickToOpenCheck.IsChecked ?? true;
                
                // Parse duration from combo
                string selectedDuration = durationCombo.SelectedItem?.ToString() ?? "6 seconds";
                if (selectedDuration.Contains("3"))
                    settings.NotificationDuration = 3;
                else if (selectedDuration.Contains("6"))
                    settings.NotificationDuration = 6;
                else if (selectedDuration.Contains("10"))
                    settings.NotificationDuration = 10;
                
                settings.Save();
                
                MessageBox.Show("Notification settings saved successfully!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAdvancedSettings()
        {
            ContentPanel.Children.Clear();

            var title = new TextBlock
            {
                Text = "Advanced Settings",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 68))
            };
            ContentPanel.Children.Add(title);

            var warningText = new TextBlock
            {
                Text = "⚠️ Warning: Changing these settings may affect application performance",
                Foreground = new SolidColorBrush(Color.FromRgb(255, 80, 80)),
                Margin = new Thickness(0, 0, 0, 20),
                FontWeight = FontWeights.Bold
            };
            ContentPanel.Children.Add(warningText);

            // Performance
            var perfGroup = new GroupBox
            {
                Header = "Performance",
                Margin = new Thickness(0, 0, 0, 15)
            };
            var perfStack = new StackPanel();
            
            hardwareAccelCheck = new CheckBox
            {
                Content = "Enable hardware acceleration",
                IsChecked = settings.HardwareAcceleration
            };
            perfStack.Children.Add(hardwareAccelCheck);
            
            cacheImagesCheck = new CheckBox
            {
                Content = "Cache recent images",
                IsChecked = settings.CacheImages
            };
            perfStack.Children.Add(cacheImagesCheck);
            
            perfGroup.Content = perfStack;
            ContentPanel.Children.Add(perfGroup);

            // Debug
            var debugGroup = new GroupBox
            {
                Header = "Debug",
                Margin = new Thickness(0, 0, 0, 15)
            };
            var debugStack = new StackPanel();
            
            enableLoggingCheck = new CheckBox
            {
                Content = "Enable debug logging",
                IsChecked = settings.EnableLogging
            };
            debugStack.Children.Add(enableLoggingCheck);
            
            showConsoleCheck = new CheckBox
            {
                Content = "Show debug console",
                IsChecked = settings.ShowConsole
            };
            debugStack.Children.Add(showConsoleCheck);
            
            debugGroup.Content = debugStack;
            ContentPanel.Children.Add(debugGroup);
            
            // FFmpeg Installation
            var ffmpegGroup = new GroupBox
            {
                Header = "FFmpeg",
                Margin = new Thickness(0, 0, 0, 15)
            };
            var ffmpegStack = new StackPanel();
            
            // Check FFmpeg status
            bool isInstalled = FFmpegManager.IsFFmpegInstalled();
            
            var ffmpegStatusText = new TextBlock
            {
                Text = isInstalled ? "✅ FFmpeg is installed" : "❌ FFmpeg is not installed",
                Foreground = new SolidColorBrush(isInstalled ? Color.FromRgb(0, 255, 68) : Color.FromRgb(255, 80, 80)),
                Margin = new Thickness(0, 5, 0, 10),
                FontWeight = FontWeights.Bold
            };
            ffmpegStack.Children.Add(ffmpegStatusText);
            
            var ffmpegInfoText = new TextBlock
            {
                Text = "FFmpeg is required for advanced video capture and recording features.",
                Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };
            ffmpegStack.Children.Add(ffmpegInfoText);
            
            var ffmpegButtonStack = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            
            var installFFmpegButton = new Button
            {
                Content = isInstalled ? "🔄 Reinstall FFmpeg" : "📥 Install FFmpeg",
                Padding = new Thickness(15, 5, 15, 5),
                Margin = new Thickness(0, 0, 10, 0)
            };
            installFFmpegButton.Click += InstallFFmpeg_Click;
            ffmpegButtonStack.Children.Add(installFFmpegButton);
            
            var verifyFFmpegButton = new Button
            {
                Content = "🔍 Verify Installation",
                Padding = new Thickness(15, 5, 15, 5)
            };
            verifyFFmpegButton.Click += VerifyFFmpeg_Click;
            ffmpegButtonStack.Children.Add(verifyFFmpegButton);
            
            ffmpegStack.Children.Add(ffmpegButtonStack);
            
            ffmpegGroup.Content = ffmpegStack;
            ContentPanel.Children.Add(ffmpegGroup);
            
            // Buttons
            var buttonStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };
            
            var saveButton = new Button
            {
                Content = "💾 Save Changes",
                Padding = new Thickness(20, 8, 20, 8),
                Margin = new Thickness(0, 0, 10, 0)
            };
            saveButton.Click += SaveAdvancedSettings_Click;
            buttonStack.Children.Add(saveButton);
            
            var cancelButton = new Button
            {
                Content = "❌ Cancel",
                Padding = new Thickness(20, 8, 20, 8)
            };
            cancelButton.Click += (s, e) => this.Close();
            buttonStack.Children.Add(cancelButton);
            
            ContentPanel.Children.Add(buttonStack);

            // Reset Button
            var resetButton = new Button
            {
                Content = "🔄 Reset All Settings to Default",
                Padding = new Thickness(20, 8, 20, 8),
                Margin = new Thickness(20, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = new SolidColorBrush(Color.FromRgb(180, 0, 0))
            };
            resetButton.Click += ResetSettings_Click;
            ContentPanel.Children.Add(resetButton);
        }
        
        private async void InstallFFmpeg_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button != null)
                {
                    button.IsEnabled = false;
                    button.Content = "⏳ Installing...";
                }
                
                // Force installation even if already installed
                await System.Threading.Tasks.Task.Run(async () =>
                {
                    bool isInstalled = FFmpegManager.IsFFmpegInstalled();
                    
                    if (isInstalled)
                    {
                        // Ask if user wants to reinstall
                        bool shouldReinstall = false;
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            var result = MessageBox.Show(
                                "FFmpeg is already installed.\n\nDo you want to reinstall it?",
                                "FFmpeg Already Installed",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);
                            shouldReinstall = (result == MessageBoxResult.Yes);
                        });
                        
                        if (!shouldReinstall)
                        {
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                if (button != null)
                                {
                                    button.IsEnabled = true;
                                    button.Content = "🔄 Reinstall FFmpeg";
                                }
                            });
                            return;
                        }
                    }
                    
                    // Proceed with installation
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show(
                            "Attempting to install FFmpeg...\n\n" +
                            "This may take a few minutes. Please wait.",
                            "Installing FFmpeg",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    });
                    
                    // Try installation methods
                    bool installed = false;
                    
                    // Try winget first
                    System.Diagnostics.Debug.WriteLine("Attempting FFmpeg installation with winget...");
                    installed = await FFmpegManager.TryInstallWithWingetAsync();
                    
                    if (!installed)
                    {
                        System.Diagnostics.Debug.WriteLine("Winget failed, trying chocolatey...");
                        installed = await FFmpegManager.TryInstallWithChocoAsync();
                    }
                    
                    if (!installed)
                    {
                        System.Diagnostics.Debug.WriteLine("Chocolatey failed, trying scoop...");
                        installed = await FFmpegManager.TryInstallWithScoopAsync();
                    }
                    
                    // Show result
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (installed)
                        {
                            MessageBox.Show(
                                "FFmpeg has been installed successfully!\n\n" +
                                "You may need to restart the application for changes to take effect.",
                                "Installation Successful",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                            
                            // Reload the settings panel to update status
                            LoadAdvancedSettings();
                        }
                        else
                        {
                            var result = MessageBox.Show(
                                "Automatic installation failed.\n\n" +
                                "Would you like to open the FFmpeg download page for manual installation?",
                                "Manual Installation Required",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Warning);
                                
                            if (result == MessageBoxResult.Yes)
                            {
                                try
                                {
                                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                    {
                                        FileName = "https://ffmpeg.org/download.html",
                                        UseShellExecute = true
                                    });
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error opening browser: {ex.Message}");
                                }
                            }
                            
                            if (button != null)
                            {
                                button.IsEnabled = true;
                                button.Content = "📥 Install FFmpeg";
                            }
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error installing FFmpeg: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                
                var button = sender as Button;
                if (button != null)
                {
                    button.IsEnabled = true;
                    button.Content = "📥 Install FFmpeg";
                }
            }
        }
        
        private void VerifyFFmpeg_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool isInstalled = FFmpegManager.IsFFmpegInstalled();
                
                if (isInstalled)
                {
                    MessageBox.Show(
                        "✅ FFmpeg is installed and working correctly!\n\n" +
                        "FFmpeg is available in your system PATH and ready to use.",
                        "FFmpeg Verified",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "❌ FFmpeg is not installed or not found in PATH.\n\n" +
                        "Please install FFmpeg to use advanced video features.",
                        "FFmpeg Not Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
                
                // Reload the settings panel to update status
                LoadAdvancedSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error verifying FFmpeg: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void SaveAdvancedSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                settings.HardwareAcceleration = hardwareAccelCheck.IsChecked ?? true;
                settings.CacheImages = cacheImagesCheck.IsChecked ?? true;
                settings.EnableLogging = enableLoggingCheck.IsChecked ?? false;
                settings.ShowConsole = showConsoleCheck.IsChecked ?? false;
                
                settings.Save();
                
                MessageBox.Show("Advanced settings saved successfully!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void ResetSettings_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to reset all settings to default?\n\nThis action cannot be undone.", 
                "Reset Settings", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                settings.ResetToDefault();
                MessageBox.Show("Settings reset to default!\n\nPlease restart the application for all changes to take effect.", 
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Reload current tab to show updated values
                LoadAdvancedSettings();
            }
        }

        private void LoadAboutInfo()
        {
            ContentPanel.Children.Clear();

            var title = new TextBlock
            {
                Text = "About Highlighter4",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 68))
            };
            ContentPanel.Children.Add(title);

            var appName = new TextBlock
            {
                Text = "Highlighter4",
                FontSize = 32,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 20, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            ContentPanel.Children.Add(appName);

            var version = new TextBlock
            {
                Text = "Version 1.0.0",
                FontSize = 16,
                Margin = new Thickness(0, 0, 0, 20),
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158))
            };
            ContentPanel.Children.Add(version);

            var description = new TextBlock
            {
                Text = "A powerful screen capture and annotation tool with advanced features.",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 30),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };
            ContentPanel.Children.Add(description);

            // Features
            var featuresGroup = new GroupBox
            {
                Header = "Features",
                Margin = new Thickness(0, 0, 0, 15)
            };
            var featuresStack = new StackPanel();
            
            AddFeature(featuresStack, "✅ Quick screen capture with hotkeys");
            AddFeature(featuresStack, "✅ Advanced image editor with F1-F7 tools");
            AddFeature(featuresStack, "✅ Scroll capture for long content");
            AddFeature(featuresStack, "✅ Speech balloons and annotations");
            AddFeature(featuresStack, "✅ Crop, blur, and highlight tools");
            AddFeature(featuresStack, "✅ Auto-save with date organization");
            AddFeature(featuresStack, "✅ Click-to-open notifications");
            AddFeature(featuresStack, "✅ OLED dark theme");
            
            featuresGroup.Content = featuresStack;
            ContentPanel.Children.Add(featuresGroup);

            var copyright = new TextBlock
            {
                Text = "© 2025 Highlighter4. All rights reserved.",
                FontSize = 12,
                Margin = new Thickness(0, 30, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158))
            };
            ContentPanel.Children.Add(copyright);
        }

        private void AddFeature(StackPanel parent, string feature)
        {
            var featureText = new TextBlock
            {
                Text = feature,
                Margin = new Thickness(0, 5, 0, 5)
            };
            parent.Children.Add(featureText);
        }

        #endregion
    }
}
