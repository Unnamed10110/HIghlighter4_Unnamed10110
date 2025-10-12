using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Highlighter4
{
    /// <summary>
    /// Manages application settings with JSON persistence
    /// </summary>
    public class AppSettings
    {
        private static AppSettings _instance;
        private static readonly string SettingsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "Highlighter4"
        );
        private static readonly string SettingsFile = Path.Combine(SettingsFolder, "settings.json");

        #region General Settings
        [JsonPropertyName("startWithWindows")]
        public bool StartWithWindows { get; set; } = true;

        [JsonPropertyName("showTrayIcon")]
        public bool ShowTrayIcon { get; set; } = true;

        [JsonPropertyName("language")]
        public string Language { get; set; } = "English";

        [JsonPropertyName("saveLocation")]
        public string SaveLocation { get; set; } = "Downloads/Highlighter4/";

        [JsonPropertyName("organizeByDate")]
        public bool OrganizeByDate { get; set; } = true;
        #endregion

        #region Capture Settings
        [JsonPropertyName("autoSaveCaptures")]
        public bool AutoSaveCaptures { get; set; } = true;

        [JsonPropertyName("copyToClipboard")]
        public bool CopyToClipboard { get; set; } = true;

        [JsonPropertyName("openInEditor")]
        public bool OpenInEditor { get; set; } = true;

        [JsonPropertyName("playSound")]
        public bool PlaySound { get; set; } = true;

        [JsonPropertyName("imageFormat")]
        public string ImageFormat { get; set; } = "PNG";
        #endregion

        #region Editor Settings
        [JsonPropertyName("defaultDrawingColor")]
        public string DefaultDrawingColor { get; set; } = "Red";

        [JsonPropertyName("defaultLineThickness")]
        public int DefaultLineThickness { get; set; } = 3;

        [JsonPropertyName("autoFocusEditor")]
        public bool AutoFocusEditor { get; set; } = true;

        [JsonPropertyName("closeAfterSave")]
        public bool CloseAfterSave { get; set; } = false;

        [JsonPropertyName("showGrid")]
        public bool ShowGrid { get; set; } = false;
        #endregion

        #region Notification Settings
        [JsonPropertyName("showNotifications")]
        public bool ShowNotifications { get; set; } = true;

        [JsonPropertyName("showThumbnail")]
        public bool ShowThumbnail { get; set; } = true;

        [JsonPropertyName("clickToOpen")]
        public bool ClickToOpen { get; set; } = true;

        [JsonPropertyName("notificationDuration")]
        public int NotificationDuration { get; set; } = 6; // seconds
        #endregion

        #region Advanced Settings
        [JsonPropertyName("hardwareAcceleration")]
        public bool HardwareAcceleration { get; set; } = true;

        [JsonPropertyName("cacheImages")]
        public bool CacheImages { get; set; } = true;

        [JsonPropertyName("enableLogging")]
        public bool EnableLogging { get; set; } = false;

        [JsonPropertyName("showConsole")]
        public bool ShowConsole { get; set; } = false;
        #endregion

        /// <summary>
        /// Gets the singleton instance of AppSettings
        /// </summary>
        public static AppSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Loads settings from file or creates default settings
        /// </summary>
        private static AppSettings Load()
        {
            try
            {
                if (!Directory.Exists(SettingsFolder))
                {
                    Directory.CreateDirectory(SettingsFolder);
                }

                if (File.Exists(SettingsFile))
                {
                    string json = File.ReadAllText(SettingsFile);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    System.Diagnostics.Debug.WriteLine($"Settings loaded from: {SettingsFile}");
                    return settings ?? new AppSettings();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No settings file found, using defaults");
                    var defaultSettings = new AppSettings();
                    defaultSettings.Save();
                    return defaultSettings;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
                return new AppSettings();
            }
        }

        /// <summary>
        /// Saves current settings to file
        /// </summary>
        public void Save()
        {
            try
            {
                if (!Directory.Exists(SettingsFolder))
                {
                    Directory.CreateDirectory(SettingsFolder);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never
                };

                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsFile, json);
                System.Diagnostics.Debug.WriteLine($"Settings saved to: {SettingsFile}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Resets all settings to default values
        /// </summary>
        public void ResetToDefault()
        {
            StartWithWindows = true;
            ShowTrayIcon = true;
            Language = "English";
            SaveLocation = "Downloads/Highlighter4/";
            OrganizeByDate = true;

            AutoSaveCaptures = true;
            CopyToClipboard = true;
            OpenInEditor = true;
            PlaySound = true;
            ImageFormat = "PNG";

            DefaultDrawingColor = "Red";
            DefaultLineThickness = 3;
            AutoFocusEditor = true;
            CloseAfterSave = false;
            ShowGrid = false;

            ShowNotifications = true;
            ShowThumbnail = true;
            ClickToOpen = true;
            NotificationDuration = 6;

            HardwareAcceleration = true;
            CacheImages = true;
            EnableLogging = false;
            ShowConsole = false;

            Save();
            System.Diagnostics.Debug.WriteLine("Settings reset to default");
        }

        /// <summary>
        /// Gets the full save path for images
        /// </summary>
        public string GetFullSavePath()
        {
            string basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                SaveLocation
            );

            if (OrganizeByDate)
            {
                string dateFolder = DateTime.Now.ToString("dd-MM-yyyy");
                return Path.Combine(basePath, dateFolder);
            }

            return basePath;
        }

        /// <summary>
        /// Gets the default filename for a new capture
        /// </summary>
        public string GetDefaultFileName()
        {
            string timestamp = DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss");
            string extension = ImageFormat.ToLower();
            if (extension == "png (recommended)")
                extension = "png";
            
            return $"{timestamp}.{extension}";
        }
    }
}

