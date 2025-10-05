# Highlighter4

A powerful screen capture and annotation tool for Windows, inspired by ShareX. Capture, edit, and organize screenshots with advanced features and a beautiful OLED dark theme.

## 🎯 Overview

Highlighter4 is a comprehensive screen capture and image editing application that runs silently in the background, ready to capture and annotate your screen at any moment with global hotkeys. Built with C# and WPF, it offers professional-grade features in a lightweight package.

**Inspired by**: ShareX - The ultimate screen capture tool

## ✨ Key Features

### 📸 **Multiple Capture Modes**
- **Quick Region Capture** (`Ctrl+Numpad .`) - Instant capture with automatic editor
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
- **Customizable** - Configure all aspects of the application

### 👻 **Background Operation**
- **System Tray Icon** - Yellow warning icon (⚠️)
- **Global Hotkeys** - Work from any application
- **Auto-start** - Optionally start with Windows
- **Invisible Mode** - Runs silently in the background

## 🚀 Quick Start

### Installation
1. Download `Highlighter4.exe`
2. Run the executable
3. The program starts in the system tray (yellow warning icon)
4. Use hotkeys to capture and edit

### Basic Usage
1. **Quick Capture**: Press `Ctrl+Numpad .` → Select region → Editor opens
2. **Edit**: Use F1-F7 tools to annotate
3. **Save**: Press `Enter` to save with notification
4. **Open**: Click notification to view in default image viewer

## ⌨️ Keyboard Shortcuts

### Global Hotkeys (Work Anywhere)
- `Alt+X` - Show highlight overlay
- `Shift+Alt+X` - Toggle capture mode (when overlay active)
- `C` - Capture region (when overlay is active)
- `Ctrl+Numpad .` - Quick region capture + editor
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

#### Quick Capture (`Ctrl+Numpad .`)
- Fastest way to capture and edit
- Direct to editor after selection
- No intermediate steps
- Perfect for quick annotations

#### Overlay Capture (`Alt+X` → `C`)
- Show highlight overlay first
- Press C to switch to capture mode
- Select region and capture
- Opens in editor automatically

#### Scroll Capture (`Ctrl+Numpad 0`)
- Automatically captures scrolling content
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
Access via right-click tray icon → Settings

#### General Settings
- Start with Windows
- Show tray icon
- Interface language (English/Español)
- Default save folder
- Date-based organization

#### Hotkeys
- View all global hotkeys
- Customization (coming soon)

#### Capture Options
- Auto-save captures
- Copy to clipboard automatically
- Open in editor after capture
- Play sound on capture
- Image format (PNG/JPEG/BMP)

#### Editor Options
- Default drawing color
- Default line thickness
- Auto-focus behavior
- Close after save
- Show grid

#### Notifications
- Show notifications on save
- Show image thumbnail
- Click to open functionality
- Display duration

#### Advanced
- Hardware acceleration
- Cache settings
- Debug logging
- Reset all settings

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
- **Editor saves**: `dd-MM-yyyy_HH-mm-ss.png`
- **Quick saves**: `dd-MM-yyyy.png`
- **Format**: PNG (lossless compression)
- **Organization**: Automatic date-based folders

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
├── Program.cs                  # Main program logic
├── MainWindow.xaml(.cs)        # Main window and tray icon
├── HighlighterWindow.xaml(.cs) # Highlight overlay
├── CaptureWindow.xaml(.cs)     # Region selection
├── ImageEditor.xaml(.cs)       # Image editor (2200+ lines)
├── SettingsWindow.xaml(.cs)    # Settings panel
├── NotificationManager.cs      # Custom notifications
├── ScrollCaptureSingleFile.cs  # Scroll capture logic
├── ScrollCaptureManager.cs     # Scroll capture manager
├── RegionSelectionForm.cs      # Region selection form
├── TrayIconManager.cs          # Tray icon management
├── HotkeyManager.cs            # Hotkey registration
├── Screenshot.cs               # Screenshot utilities
├── NativeMethods.cs            # Windows API P/Invoke
└── Highlighter4.csproj         # Project file
```

### Key Technologies
- **WPF**: Main UI framework
- **Windows Forms**: Specific components
- **P/Invoke**: Native Windows API integration
- **System.Drawing**: Image manipulation
- **XAML**: UI markup
- **Async/Await**: Asynchronous operations

### Design Patterns
- **Event-driven architecture**: Loose coupling between components
- **MVVM-inspired**: Separation of concerns
- **Command pattern**: Undo/redo implementation
- **Observer pattern**: Event subscriptions
- **Factory pattern**: Icon creation

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
- ✅ Multiple capture modes (Quick, Overlay, Scroll)
- ✅ Advanced image editor with F1-F7 tools
- ✅ Speech balloons with draggable tails
- ✅ Advanced crop tool with animated borders
- ✅ Custom notifications with thumbnails
- ✅ OLED dark theme throughout
- ✅ Settings panel with sidebar navigation
- ✅ Date-based file organization
- ✅ Click-to-open notifications
- ✅ Complete keyboard navigation
- ✅ Undo/redo support
- ✅ Auto-save and clipboard integration

## 🚀 Future Enhancements

- [ ] Customizable hotkeys
- [ ] Multiple monitor support
- [ ] Video recording
- [ ] GIF creation
- [ ] OCR text recognition
- [ ] Cloud upload options
- [ ] Custom color picker
- [ ] Annotation templates
- [ ] Batch processing
- [ ] Plugin system

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