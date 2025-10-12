# Highlighter4

A powerful screen capture and annotation tool for Windows, inspired by ShareX. Capture, edit, and organize screenshots with advanced features and a beautiful OLED dark theme.

## 🎯 Overview

Highlighter4 is a comprehensive screen capture and image editing application that runs silently in the background, ready to capture and annotate your screen at any moment with global hotkeys. Built with C# and WPF, it offers professional-grade features in a lightweight package.

**Inspired by**: ShareX - The ultimate screen capture tool

## ✨ Key Features

### 📸 **Multiple Capture Modes**
- **GIF Recording** (`Ctrl+Numpad .`) - Record screen region as animated GIF (30 FPS)
- **Overlay Capture** (`Alt+X` → `C`) - Highlight mode with capture option
- **Scroll Capture** (`Ctrl+Numpad 0`) - Capture long scrolling content automatically
- **Automatic Save** - All captures saved to organized folders by date

### 🎨 **Advanced Image Editor**
- **F1-F7 Tools** - Complete set of annotation tools
  - **F1** - Arrow tool (red arrows with lines)
  - **F2** - Line tool (straight lines)
  - **F3** - Rectangle tool (hollow red rectangles)
  - **F4** - Highlight tool (semi-transparent overlays)
  - **F5** - Blur tool (real blur effect on regions)
  - **F6** - Speech balloon tool (draggable with tail stretching)
  - **F7** - Crop tool (animated borders with handles)
- **OLED Dark Theme** - Beautiful black theme optimized for OLED displays
- **Undo/Redo** (`Ctrl+Z`/`Ctrl+Y`) - Full history support
- **Auto-save** (`Enter` or `Ctrl+S`) - Quick save with notifications
- **Smart Clipboard** (`Ctrl+C`) - Copy and save simultaneously

### 🔔 **Smart Notifications**
- **Image Thumbnails** - 300x300px preview with black OLED background
- **Click to Open** - Click notification to open image in default viewer
- **Auto-dismiss** - Disappears after 6 seconds
- **No Popups** - Clean, non-intrusive notifications

### 💾 **Organized Storage**
- **Date-based Folders** - `Downloads/Highlighter4/dd-MM-yyyy/`
- **Timestamped Files** - `dd-MM-yyyy_HH-mm-ss.png`
- **Automatic Organization** - No manual file management needed
- **Clipboard Integration** - Always copied to clipboard

### ⚙️ **Settings Panel**
- **Modern UI** - OLED dark theme with sidebar navigation
- **7 Categories** - General, Hotkeys, Capture, Editor, Notifications, Advanced, About
- **Easy Access** - Right-click tray icon → Settings
- **Persistent Settings** - JSON-based configuration saved to AppData
- **FFmpeg Integration** - Built-in FFmpeg installer and verifier
- **Fully Functional** - All settings saved and applied instantly

### 👻 **Background Operation**
- **System Tray Icon** - Yellow warning icon (⚠️)
- **Global Hotkeys** - Work from any application
- **Auto-start** - Automatic Windows startup with registry integration
- **Invisible Mode** - Runs silently in the background
- **FFmpeg Auto-check** - Verifies FFmpeg installation on startup

### 🎬 **FFmpeg Integration & GIF Recording**
- **Automatic Detection** - Checks for FFmpeg on first run
- **One-Click Installation** - Installs via winget, chocolatey, or scoop
- **GIF Recording** - Record screen region as 30 FPS animated GIF
- **Visual Border** - White 1px animated border during recording
- **Timer Overlay** - Real-time recording timer with milliseconds (mm:ss.cs)
- **Progress Indicator** - Tray icon shows rendering progress (5% → 100%)
- **Settings Integration** - Install/reinstall/verify from Advanced Settings
- **Visual Status** - Green ✅ or red ❌ indicator in settings

## 🚀 Quick Start

### Installation
1. Download `Highlighter4.exe`
2. Run the executable
3. The program starts in the system tray (yellow warning icon)
4. Use hotkeys to capture and edit

### Basic Usage
1. **GIF Recording**: Press `Ctrl+Numpad .` → Select region → Press again to stop → GIF saved
2. **Scroll Capture**: Press `Ctrl+Numpad 0` → Select region → Auto-scroll capture
3. **Overlay Capture**: Press `Alt+X` → Press `C` → Select region → Editor opens
4. **Edit**: Use F1-F7 tools to annotate
5. **Save**: Press `Enter` to save with notification
6. **Open**: Click notification to view in default viewer

## ⌨️ Keyboard Shortcuts

### Global Hotkeys (Work Anywhere)
- `Alt+X` - Show highlight overlay
- `Shift+Alt+X` - Toggle capture mode (when overlay active)
- `C` - Capture region (when overlay is active)
- `Ctrl+Numpad .` - GIF recording (press once to start, again to stop)
- `Ctrl+Numpad 0` - Scroll capture + editor

### Editor Shortcuts
- `F1` - Arrow tool
- `F2` - Line tool
- `F3` - Rectangle tool
- `F4` - Highlight tool
- `F5` - Blur tool
- `F6` - Speech balloon tool
- `F7` - Crop tool
- `Ctrl+S` or `Enter` - Save image
- `Ctrl+C` - Copy to clipboard and save
- `Ctrl+Z` - Undo
- `Ctrl+Y` - Redo
- `Escape` - Exit tool/mode or close editor

### Speech Balloon Tool
- `F6` or `Shift+Alt+X` - Activate speech balloon mode
- Click to place balloon
- Drag balloon to move
- Drag red dot on tail to adjust tail position
- `Ctrl+Enter` - Confirm text (supports line breaks)

### Crop Tool
- `F7` - Activate crop tool
- Drag borders or handles to adjust crop area
- Animated white dashed lines show crop region
- `Enter` - Confirm crop
- `Escape` - Cancel crop

## 🛠️ Technical Details

### Built With
- **Framework**: .NET 10.0 (Windows)
- **UI**: WPF (Windows Presentation Foundation)
- **Forms**: Windows Forms (for specific components)
- **Graphics**: System.Drawing and System.Windows.Media
- **Language**: C# with nullable reference types

### Architecture
- **Event-driven**: Asynchronous event handling
- **Global Hotkeys**: Windows API (P/Invoke)
- **Custom Notifications**: Floating windows with thumbnails
- **Image Processing**: Real-time blur, crop, and annotation
- **Undo/Redo System**: Complete action history

### System Requirements
- **OS**: Windows 10/11 (64-bit)
- **Minimum**: Windows 7 SP1 (with .NET 10.0)
- **Memory**: 100MB RAM
- **Disk**: 50MB free space
- **Display**: Any resolution supported

## 📦 Compilation

### Prerequisites
- .NET 10.0 SDK or higher
- Windows 10/11
- Visual Studio 2022 or VS Code (optional)

### Build Commands
```bash
# Clean previous builds
dotnet clean Highlighter4.csproj --configuration Release

# Build the project
dotnet build Highlighter4.csproj --configuration Release

# Run the application
dotnet run --project Highlighter4.csproj --configuration Release
```

### Output
- **Location**: `bin/Release/`
- **Main Executable**: `Highlighter4.exe`
- **Dependencies**: Included in output folder

## 🎨 Features in Detail

### Image Editor
The built-in image editor provides professional annotation tools:

#### Drawing Tools
- **Arrows**: Red arrows with customizable direction
- **Lines**: Straight lines for connections
- **Rectangles**: Hollow red rectangles for emphasis
- **Highlights**: Semi-transparent yellow overlays
- **Blur**: Real Gaussian blur effect on selected regions
- **Speech Balloons**: Draggable text bubbles with adjustable tails
- **Crop**: Advanced cropping with animated borders and handles

#### Editor Features
- **Real-time Preview**: See changes as you draw
- **Undo/Redo**: Complete action history
- **Auto-save**: Saves to organized folders automatically
- **Clipboard Integration**: Always copies to clipboard
- **Focus Management**: Auto-focus on open
- **Keyboard Navigation**: Full keyboard support

### Capture Modes

#### GIF Recording (`Ctrl+Numpad .`)
- **Start**: Press `Ctrl+Numpad .`, select region, recording begins
- **Visual Feedback**: White 1px animated border around region
- **Timer**: Real-time recording timer with milliseconds (mm:ss.cs)
- **Stop**: Press `Ctrl+Numpad .` again to stop recording
- **Rendering**: Tray icon shows progress (5% → 100%)
- **30 FPS**: High-quality animated GIF with optimized palette
- **Auto-save**: Saved to `Downloads/Highlighter4/dd-MM-yyyy/`
- **Notification**: Thumbnail notification when complete
- **Perfect for**: Tutorials, bug reports, demos, animations

#### Overlay Capture (`Alt+X` → `C`)
- Show highlight overlay first
- Press C to switch to capture mode
- Select region and capture
- Opens in editor automatically

#### Scroll Capture (`Ctrl+Numpad 0`)
- Automatically captures scrolling content
- Visual border shows capture region (animated green 1px)
- Detects scroll position
- Combines multiple screenshots
- Perfect for long web pages or documents

### Notification System
- **Custom Windows**: Floating notification windows
- **Image Thumbnails**: 300x300px preview
- **OLED Theme**: Black background (#000000)
- **Click to Open**: Opens image in default viewer
- **Auto-dismiss**: 6-second display duration
- **Non-intrusive**: Bottom-right corner placement

### Settings Panel
Access via right-click tray icon → Settings (⚙️)

All settings are **persistent** - saved to `%AppData%/Highlighter4/settings.json`

#### General Settings
- ✅ Start with Windows (registry integration)
- ✅ Show tray icon
- ✅ Interface language (English/Español)
- ✅ Default save folder (with Browse button)
- ✅ Date-based organization toggle

#### Hotkeys
- View all global hotkeys
- Keyboard shortcut reference
- Customization (coming in future update)

#### Capture Options
- ✅ Auto-save captures
- ✅ Copy to clipboard automatically
- ✅ Open in editor after capture
- ✅ Play sound on capture
- ✅ Image format (PNG/JPEG/BMP)

#### Editor Options
- Default drawing color (Red)
- Default line thickness (3px)
- ✅ Auto-focus behavior
- ✅ Close after save
- ✅ Show grid toggle

#### Notifications
- ✅ Show notifications on save
- ✅ Show image thumbnail
- ✅ Click to open functionality
- ✅ Display duration (3/6/10 seconds)

#### Advanced
- ✅ Hardware acceleration
- ✅ Cache settings
- ✅ Debug logging
- ✅ Show debug console
- ✅ FFmpeg installation/verification
- ✅ Reset all settings to default

## 📁 File Organization

### Default Save Location
```
Downloads/
└── Highlighter4/
    ├── 05-10-2025/
    │   ├── 05-10-2025_08-30-15.png
    │   ├── 05-10-2025_08-45-22.png
    │   └── 05-10-2025.png (Ctrl+C saves)
    └── 06-10-2025/
        └── 06-10-2025_09-12-33.png
```

### File Naming Convention
- **GIF recordings**: `dd-MM-yyyy_HH-mm-ss.gif`
- **Editor saves**: `dd-MM-yyyy_HH-mm-ss.png`
- **Quick saves**: `dd-MM-yyyy.png`
- **Format**: PNG (lossless) / GIF (30 FPS, optimized palette)
- **Organization**: Automatic date-based folders

## 🎬 FFmpeg Integration & GIF Recording

### What is FFmpeg?
FFmpeg is a powerful multimedia framework used for video recording, conversion, and streaming. Highlighter4 uses it for GIF recording and advanced video capture features.

### GIF Recording Feature
Record any region of your screen as a high-quality animated GIF at 30 FPS.

#### How to Record a GIF
1. Press `Ctrl+Numpad .` to start
2. Select the region you want to record with your mouse
3. Recording starts automatically with visual indicators:
   - White 1px animated border around the region
   - Timer overlay in bottom-left showing elapsed time (mm:ss.cs)
4. Press `Ctrl+Numpad .` again to stop recording
5. Tray icon shows rendering progress (5% → 100%)
6. Notification appears when GIF is ready
7. Click notification to open the GIF

#### GIF Quality Settings
- **Frame Rate**: 30 FPS (smooth playback)
- **Palette**: Optimized 256-color palette with dithering
- **Compression**: Bayer dithering (bayer_scale=5) for smooth gradients
- **Scaling**: Lanczos algorithm for high-quality resizing
- **Loop**: Infinite loop by default
- **File Size**: Optimized for quality vs. size balance

### Automatic Installation
On first run, Highlighter4 automatically:
1. Waits 3 seconds for UI to load
2. Checks if FFmpeg is installed
3. Prompts user if installation is needed
4. Attempts installation via:
   - **Winget** (Windows Package Manager) - Recommended
   - **Chocolatey** - Popular package manager
   - **Scoop** - Lightweight alternative
5. Falls back to manual installation if all fail

### Manual Installation from Settings
1. Open Settings → Advanced
2. Scroll to "FFmpeg" section
3. Click "📥 Install FFmpeg" or "🔄 Reinstall FFmpeg"
4. Wait for installation (may take a few minutes)
5. Click "🔍 Verify Installation" to confirm

### Status Indicator
- **✅ Green**: FFmpeg installed and working
- **❌ Red**: FFmpeg not found

### Troubleshooting FFmpeg
- If auto-install fails, try each method manually:
  - Winget: `winget install Gyan.FFmpeg`
  - Chocolatey: `choco install ffmpeg`
  - Scoop: `scoop install ffmpeg`
- Download manually: https://ffmpeg.org/download.html
- Ensure FFmpeg is in your system PATH
- Restart application after installation

## 🔧 Troubleshooting

### Hotkeys Not Working
- Check if another application is using the same hotkeys
- Try restarting the application
- Check Windows "Do Not Disturb" settings (may conflict with Alt+X)

### Tray Icon Not Visible
- Check system tray overflow area (hidden icons)
- Restart the application
- The icon auto-restores every 30 seconds

### Editor Not Opening
- Check if antivirus is blocking the application
- Verify .NET 10.0 runtime is installed
- Check application logs in debug mode

### Images Not Saving
- Verify Downloads folder permissions
- Check available disk space
- Ensure folder path is accessible

### Notifications Not Showing
- Check Windows notification settings
- Verify application has notification permissions
- Try restarting the application

### Settings Not Persisting
- Check `%AppData%/Highlighter4/settings.json` exists
- Verify folder has write permissions
- Try resetting settings to defaults

### Auto-start Not Working
- Open Settings → General
- Verify "Start with Windows" is checked
- Check registry: `HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`
- Ensure executable path is correct

### GIF Recording Issues
- **FFmpeg not found**: Install FFmpeg from Settings → Advanced
- **Recording doesn't start**: Verify FFmpeg is in system PATH
- **No notification after recording**: Recording may have been too short (< 1 second)
- **Poor quality**: Ensure 30 FPS is maintained during recording
- **File too large**: Consider recording smaller regions or shorter duration
- **Tray icon stuck on progress**: Restart the application

## 🎯 Use Cases

### For Developers
- Capture code snippets with annotations
- Document bugs with arrows and highlights
- Create tutorials with speech balloons
- Quick screenshots for documentation

### For Content Creators
- Capture and edit screenshots for videos
- Annotate images for tutorials
- Create professional-looking captures
- Quick edits without opening heavy editors

### For Support Teams
- Capture and annotate issues
- Create step-by-step guides
- Highlight important areas
- Share screenshots quickly

### For Students
- Capture lecture slides
- Annotate study materials
- Create visual notes
- Organize captures by date

## 🔐 Privacy & Security

- **No Internet Connection**: Works completely offline
- **No Data Collection**: No telemetry or analytics
- **Local Storage**: All images saved locally
- **No Cloud**: No uploads to external servers
- **Open Source**: Code available for review

## 📝 Development

### Project Structure
```
Highlighter4/
├── App.cs                      # Application entry point
├── Program.cs                  # Main program logic and FFmpeg check
├── MainWindow.xaml(.cs)        # Main window and tray icon (650+ lines)
├── HighlighterWindow.xaml(.cs) # Highlight overlay
├── CaptureWindow.xaml(.cs)     # Region selection with visual border
├── ImageEditor.xaml(.cs)       # Image editor (2200+ lines)
├── SettingsWindow.xaml(.cs)    # Settings panel (1200+ lines)
├── AppSettings.cs              # Persistent settings manager
├── FFmpegManager.cs            # FFmpeg detection and installation
├── GifRecorder.cs              # GIF recording with FFmpeg (500+ lines)
├── NotificationManager.cs      # Custom notifications with thumbnails
├── ScrollCaptureSingleFile.cs  # Scroll capture with visual border
├── ScrollCaptureManager.cs     # Scroll capture manager
├── RegionSelectionForm.cs      # Region selection form
├── TrayIconManager.cs          # Tray icon management
├── HotkeyManager.cs            # Global hotkey registration
├── Screenshot.cs               # Screenshot utilities
├── NativeMethods.cs            # Windows API P/Invoke
├── Highlighter4.csproj         # Project file
├── README.md                   # Documentation
└── .gitignore                  # Git ignore file
```

### Key Technologies
- **WPF**: Main UI framework
- **Windows Forms**: Specific components and notifications
- **P/Invoke**: Native Windows API integration
- **System.Drawing**: Image manipulation
- **XAML**: UI markup
- **Async/Await**: Asynchronous operations
- **System.Text.Json**: Settings serialization
- **Process Management**: FFmpeg installation automation

### Design Patterns
- **Event-driven architecture**: Loose coupling between components
- **MVVM-inspired**: Separation of concerns
- **Command pattern**: Undo/redo implementation
- **Observer pattern**: Event subscriptions
- **Singleton pattern**: Settings and FFmpeg manager
- **Strategy pattern**: Multiple installation methods (winget/choco/scoop)

## 🌟 Inspiration

This project is inspired by **ShareX**, the ultimate screen capture and sharing tool. While ShareX offers extensive features and cloud integration, Highlighter4 focuses on:
- **Simplicity**: Core features without complexity
- **Speed**: Instant capture and edit
- **Privacy**: Completely offline operation
- **OLED Theme**: Beautiful dark interface
- **Lightweight**: Minimal resource usage

## 👨‍💻 Developer

**Unnamed10110**

- 📧 Email: trojan.v6@gmail.com
- 📧 Alternate: sergiobritos10110@gmail.com
- 🔗 GitHub: Unnamed10110

## 📜 License

This project is provided as-is for personal and educational use.

## 🙏 Acknowledgments

- **ShareX Team** - For inspiration and reference
- **.NET Team** - For the excellent framework
- **Windows API** - For global hotkey support
- **Community** - For feedback and suggestions

## 🔄 Version History

### Version 1.0.0 (Current)
#### Core Features
- ✅ GIF recording at 30 FPS with FFmpeg integration
- ✅ Multiple capture modes (GIF, Overlay, Scroll)
- ✅ Advanced image editor with F1-F7 tools
- ✅ Speech balloons with draggable tails
- ✅ Advanced crop tool with animated borders and draggable handles
- ✅ Visual borders during recording/capture (animated, 1px)

#### User Interface
- ✅ OLED dark theme throughout
- ✅ Custom notifications with 300x300px thumbnails
- ✅ Click-to-open notifications
- ✅ Settings panel with sidebar navigation (7 categories)
- ✅ Complete keyboard navigation

#### Settings & Configuration
- ✅ Persistent settings with JSON serialization
- ✅ All settings functional and saved
- ✅ Windows startup integration (registry)
- ✅ Browse button for save folder selection
- ✅ Reset to defaults functionality

#### FFmpeg & GIF Recording
- ✅ Automatic FFmpeg detection on startup
- ✅ One-click installation (winget/chocolatey/scoop)
- ✅ GIF recording at 30 FPS with optimized palette
- ✅ Visual recording border (white 1px, animated)
- ✅ Real-time timer overlay with milliseconds
- ✅ Rendering progress indicator in tray icon (5% → 100%)
- ✅ Settings panel integration with status indicator
- ✅ Verify installation button
- ✅ Manual fallback option

#### File Management
- ✅ Date-based file organization
- ✅ Auto-save and clipboard integration
- ✅ Timestamped filenames
- ✅ No cache directory creation

#### Advanced Features
- ✅ Undo/redo support with complete history
- ✅ Real-time blur effect
- ✅ Draggable crop borders (1px animated)
- ✅ Immediate crop/cancel buttons
- ✅ Auto-focus and close-after-save options

## 🚀 Future Enhancements

### Planned Features
- [ ] Customizable hotkeys (UI ready, implementation pending)
- [ ] Multiple monitor support
- [ ] MP4 video recording (FFmpeg ready)
- [ ] Screen recording with audio
- [ ] OCR text recognition
- [ ] Custom color picker for tools
- [ ] Annotation templates

### Advanced Features
- [ ] Cloud upload options (optional)
- [ ] Batch processing for multiple images
- [ ] Plugin system for extensibility
- [ ] Image filters and effects
- [ ] Watermark support
- [ ] Annotation presets
- [ ] Keyboard shortcut customization UI

## 📞 Support

For issues, questions, or suggestions:
- Email: trojan.v6@gmail.com or sergiobritos10110@gmail.com
- Include "Highlighter4" in the subject line
- Provide detailed description and screenshots if applicable

## 🎓 Educational Purpose

This project serves as an example of:
- WPF application development
- Windows API integration
- Image processing in C#
- Event-driven architecture
- Custom UI controls
- Global hotkey implementation
- Async programming patterns

---

**Made with ❤️ by Unnamed10110**

*Inspired by ShareX - Taking the best ideas and making them our own*