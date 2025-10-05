using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Highlighter4
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
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
                Foreground = new SolidColorBrush(Color.FromRgb(88, 166, 255))
            };
            ContentPanel.Children.Add(title);

            // Startup Settings
            var startupGroup = new GroupBox
            {
                Header = "Startup",
                Margin = new Thickness(0, 0, 0, 15)
            };
            var startupStack = new StackPanel();
            
            var startWithWindows = new CheckBox
            {
                Content = "Start with Windows",
                IsChecked = true
            };
            startupStack.Children.Add(startWithWindows);
            
            var showTrayIcon = new CheckBox
            {
                Content = "Show tray icon",
                IsChecked = true
            };
            startupStack.Children.Add(showTrayIcon);
            
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
            
            var langCombo = new ComboBox
            {
                Width = 200,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            langCombo.Items.Add("English");
            langCombo.Items.Add("Español");
            langCombo.SelectedIndex = 0;
            languageStack.Children.Add(langCombo);
            
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
            
            var savePathText = new TextBox
            {
                Text = "Downloads/Highlighter4/",
                Width = 350,
                IsReadOnly = true,
                Margin = new Thickness(0, 0, 10, 0)
            };
            savePathStack.Children.Add(savePathText);
            
            var browseButton = new Button
            {
                Content = "Browse...",
                Padding = new Thickness(15, 5, 15, 5)
            };
            savePathStack.Children.Add(browseButton);
            
            saveStack.Children.Add(savePathStack);
            
            var organizeByDate = new CheckBox
            {
                Content = "Organize by date (dd-MM-yyyy folders)",
                IsChecked = true,
                Margin = new Thickness(0, 10, 0, 0)
            };
            saveStack.Children.Add(organizeByDate);
            
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
                Padding = new Thickness(20, 8, 20, 8)
            };
            saveButton.Click += (s, e) => MessageBox.Show("Settings saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void LoadHotkeysSettings()
        {
            ContentPanel.Children.Clear();

            var title = new TextBlock
            {
                Text = "Hotkeys Settings",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = new SolidColorBrush(Color.FromRgb(88, 166, 255))
            };
            ContentPanel.Children.Add(title);

            var description = new TextBlock
            {
                Text = "Configure global hotkeys for quick access to features",
                Foreground = new SolidColorBrush(Color.FromRgb(139, 148, 158)),
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
                Foreground = new SolidColorBrush(Color.FromRgb(255, 184, 0)),
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
                Foreground = new SolidColorBrush(Color.FromRgb(88, 166, 255))
            };
            ContentPanel.Children.Add(title);

            // Capture Options
            var captureGroup = new GroupBox
            {
                Header = "Capture Options",
                Margin = new Thickness(0, 0, 0, 15)
            };
            var captureStack = new StackPanel();
            
            var autoSave = new CheckBox
            {
                Content = "Auto-save captures",
                IsChecked = true
            };
            captureStack.Children.Add(autoSave);
            
            var copyToClipboard = new CheckBox
            {
                Content = "Copy to clipboard automatically",
                IsChecked = true
            };
            captureStack.Children.Add(copyToClipboard);
            
            var openInEditor = new CheckBox
            {
                Content = "Open in editor after capture",
                IsChecked = true
            };
            captureStack.Children.Add(openInEditor);
            
            var playSound = new CheckBox
            {
                Content = "Play sound on capture",
                IsChecked = true
            };
            captureStack.Children.Add(playSound);
            
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
            
            var formatCombo = new ComboBox
            {
                Width = 200,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            formatCombo.Items.Add("PNG (Recommended)");
            formatCombo.Items.Add("JPEG");
            formatCombo.Items.Add("BMP");
            formatCombo.SelectedIndex = 0;
            formatStack.Children.Add(formatCombo);
            
            formatGroup.Content = formatStack;
            ContentPanel.Children.Add(formatGroup);
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
                Foreground = new SolidColorBrush(Color.FromRgb(88, 166, 255))
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
                Text = "Default drawing color: Red",
                Margin = new Thickness(0, 5, 0, 5)
            };
            toolsStack.Children.Add(defaultColor);
            
            var defaultThickness = new TextBlock
            {
                Text = "Default line thickness: 3px",
                Margin = new Thickness(0, 5, 0, 5)
            };
            toolsStack.Children.Add(defaultThickness);
            
            toolsGroup.Content = toolsStack;
            ContentPanel.Children.Add(toolsGroup);

            // Editor Behavior
            var behaviorGroup = new GroupBox
            {
                Header = "Editor Behavior",
                Margin = new Thickness(0, 0, 0, 15)
            };
            var behaviorStack = new StackPanel();
            
            var autoFocus = new CheckBox
            {
                Content = "Auto-focus editor on open",
                IsChecked = true
            };
            behaviorStack.Children.Add(autoFocus);
            
            var closeAfterSave = new CheckBox
            {
                Content = "Close editor after saving",
                IsChecked = true
            };
            behaviorStack.Children.Add(closeAfterSave);
            
            var showGrid = new CheckBox
            {
                Content = "Show grid in editor",
                IsChecked = false
            };
            behaviorStack.Children.Add(showGrid);
            
            behaviorGroup.Content = behaviorStack;
            ContentPanel.Children.Add(behaviorGroup);
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
                Foreground = new SolidColorBrush(Color.FromRgb(88, 166, 255))
            };
            ContentPanel.Children.Add(title);

            // Notification Options
            var notifGroup = new GroupBox
            {
                Header = "Notification Options",
                Margin = new Thickness(0, 0, 0, 15)
            };
            var notifStack = new StackPanel();
            
            var showNotifications = new CheckBox
            {
                Content = "Show notifications on save",
                IsChecked = true
            };
            notifStack.Children.Add(showNotifications);
            
            var showThumbnail = new CheckBox
            {
                Content = "Show image thumbnail in notification",
                IsChecked = true
            };
            notifStack.Children.Add(showThumbnail);
            
            var clickToOpen = new CheckBox
            {
                Content = "Click notification to open image",
                IsChecked = true
            };
            notifStack.Children.Add(clickToOpen);
            
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
            
            var durationCombo = new ComboBox
            {
                Width = 200,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            durationCombo.Items.Add("3 seconds");
            durationCombo.Items.Add("6 seconds");
            durationCombo.Items.Add("10 seconds");
            durationCombo.SelectedIndex = 1;
            durationStack.Children.Add(durationCombo);
            
            durationGroup.Content = durationStack;
            ContentPanel.Children.Add(durationGroup);
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
                Foreground = new SolidColorBrush(Color.FromRgb(88, 166, 255))
            };
            ContentPanel.Children.Add(title);

            var warningText = new TextBlock
            {
                Text = "⚠️ Warning: Changing these settings may affect application performance",
                Foreground = new SolidColorBrush(Color.FromRgb(255, 100, 100)),
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
            
            var hardwareAccel = new CheckBox
            {
                Content = "Enable hardware acceleration",
                IsChecked = true
            };
            perfStack.Children.Add(hardwareAccel);
            
            var cacheImages = new CheckBox
            {
                Content = "Cache recent images",
                IsChecked = true
            };
            perfStack.Children.Add(cacheImages);
            
            perfGroup.Content = perfStack;
            ContentPanel.Children.Add(perfGroup);

            // Debug
            var debugGroup = new GroupBox
            {
                Header = "Debug",
                Margin = new Thickness(0, 0, 0, 15)
            };
            var debugStack = new StackPanel();
            
            var enableLogging = new CheckBox
            {
                Content = "Enable debug logging",
                IsChecked = false
            };
            debugStack.Children.Add(enableLogging);
            
            var showConsole = new CheckBox
            {
                Content = "Show debug console",
                IsChecked = false
            };
            debugStack.Children.Add(showConsole);
            
            debugGroup.Content = debugStack;
            ContentPanel.Children.Add(debugGroup);

            // Reset Button
            var resetButton = new Button
            {
                Content = "🔄 Reset All Settings to Default",
                Padding = new Thickness(20, 8, 20, 8),
                Margin = new Thickness(0, 20, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = new SolidColorBrush(Color.FromRgb(139, 0, 0))
            };
            resetButton.Click += (s, e) =>
            {
                var result = MessageBox.Show("Are you sure you want to reset all settings to default?", 
                    "Reset Settings", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    MessageBox.Show("Settings reset to default!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };
            ContentPanel.Children.Add(resetButton);
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
                Foreground = new SolidColorBrush(Color.FromRgb(88, 166, 255))
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
