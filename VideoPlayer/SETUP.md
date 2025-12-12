# VideoPlayer Setup Guide

This document provides detailed instructions for building and running the VideoPlayer WinUI 3 application.

## Prerequisites

### System Requirements
- **Operating System**: Windows 10 version 1809 (build 17763) or later, or Windows 11
- **Visual Studio 2022**: Version 17.0 or later
- **.NET SDK**: .NET 10.0 or later

### Required Visual Studio Workloads

When installing Visual Studio 2022, ensure you have the following workloads:

1. **.NET Desktop Development**
   - Required for .NET application development
   - Includes .NET project templates and build tools

2. **Windows App SDK**
   - Automatically included with Visual Studio 2022
   - Provides WinUI 3 templates and runtime

### Optional but Recommended
- **Git for Windows**: For version control
- **Windows Terminal**: For better command-line experience

## Installation Steps

### Step 1: Clone the Repository

```bash
git clone https://github.com/s780609/HentaiDownloader.git
cd HentaiDownloader
```

### Step 2: Open the Solution

1. Launch **Visual Studio 2022**
2. Open `HentaiDownloader.sln`
3. Wait for Visual Studio to restore NuGet packages (this may take a few minutes)

### Step 3: Restore NuGet Packages

If packages don't restore automatically:

```bash
cd VideoPlayer
dotnet restore
```

Or in Visual Studio:
- Right-click the solution in Solution Explorer
- Select **Restore NuGet Packages**

### Step 4: Build the Project

#### Using Visual Studio:
1. Set **VideoPlayer** as the startup project
   - Right-click `VideoPlayer` in Solution Explorer
   - Select **Set as Startup Project**
2. Select target platform: **x64** or **ARM64**
3. Click **Build** → **Build Solution** (or press `Ctrl+Shift+B`)

#### Using Command Line:
```bash
cd VideoPlayer
dotnet build -c Release -r win-x64
```

For ARM64:
```bash
dotnet build -c Release -r win-arm64
```

### Step 5: Run the Application

#### From Visual Studio:
1. Press **F5** to run with debugging
2. Or press **Ctrl+F5** to run without debugging

#### From Command Line:
```bash
cd VideoPlayer
dotnet run
```

## First Run Setup

### FFmpeg Installation
FFMpegCore (used for thumbnail generation) will automatically download FFmpeg binaries on first use. This requires:
- Active internet connection
- Write permissions to the application's directory

If automatic download fails, manually download FFmpeg:
1. Download from: https://ffmpeg.org/download.html
2. Extract to a folder (e.g., `C:\ffmpeg`)
3. Add to system PATH or place binaries in application directory

### Folder Access Permissions
The application needs permission to:
- Read video files from selected folders
- Write thumbnails to temp folder (`%TEMP%\VideoPlayerThumbnails`)

Ensure you have appropriate permissions for these operations.

## Troubleshooting

### Build Errors

#### "Cannot find Windows SDK"
**Solution**: Install Windows SDK via Visual Studio Installer
1. Open Visual Studio Installer
2. Modify your Visual Studio 2022 installation
3. Under **Individual Components**, select:
   - Windows 10/11 SDK (latest version)

#### "Project targets framework 'net10.0-windows10.0.19041.0' which is not installed"
**Solution**: Install .NET 10.0 SDK
```bash
# Check installed SDKs
dotnet --list-sdks

# Download from: https://dotnet.microsoft.com/download/dotnet/10.0
```

#### "NuGet package restore failed"
**Solution**: 
1. Clear NuGet cache:
   ```bash
   dotnet nuget locals all --clear
   ```
2. Restore packages:
   ```bash
   dotnet restore
   ```

### Runtime Errors

#### "Application failed to start"
**Solution**: Install Windows App SDK Runtime
- Download from: https://aka.ms/windowsappsdk/1.6/latest/windowsappruntimeinstall-x64.exe
- Or install via Visual Studio Installer

#### "Video won't play"
**Possible causes and solutions**:
1. **Codec not supported**
   - Install K-Lite Codec Pack or VLC Codec Pack
   - Windows Media Foundation has limited codec support

2. **File access denied**
   - Run application as Administrator
   - Check file permissions

3. **File path too long**
   - Move video to folder with shorter path
   - Enable long path support in Windows

#### "Thumbnails not generating"
**Possible causes**:
1. **FFmpeg not available**
   - Check internet connection for initial download
   - Manually install FFmpeg (see First Run Setup)

2. **Temp folder not writable**
   - Check permissions on `%TEMP%` folder
   - Run application as Administrator

3. **Video file corrupted**
   - Test video with another player (VLC, Windows Media Player)

### Performance Issues

#### "Slow thumbnail generation"
- Thumbnails are generated on-demand in batches
- First scan of large folder will be slow
- Subsequent loads use cached thumbnails

#### "High memory usage"
- Large video collections may use significant memory
- Clear thumbnail cache periodically
- Limit number of videos loaded at once

## Configuration

### Changing Default Settings

The application currently uses hardcoded settings. To customize:

1. **Thumbnail size**: Edit `ThumbnailService.cs`
   ```csharp
   size: new System.Drawing.Size(320, 180)  // Change width/height
   ```

2. **Supported formats**: Edit `VideoScannerService.cs`
   ```csharp
   private readonly string[] _supportedExtensions = 
   {
       ".mp4", ".mkv", /* add more formats */
   };
   ```

3. **Thumbnail cache location**: Edit `ThumbnailService.cs`
   ```csharp
   _thumbnailCacheFolder = Path.Combine(/* your path */);
   ```

## Development

### Project Structure
```
VideoPlayer/
├── App.xaml/cs              # Application entry point
├── MainWindow.xaml/cs       # Main window with navigation
├── Views/                   # UI pages
├── ViewModels/              # MVVM ViewModels
├── Models/                  # Data models
├── Services/                # Business logic services
└── Assets/                  # Resources (icons, images)
```

### Architecture
- **Pattern**: MVVM (Model-View-ViewModel)
- **UI Framework**: WinUI 3
- **Navigation**: Frame-based navigation
- **Data Binding**: x:Bind (compiled binding)

### Adding New Features

1. **Add new page**:
   - Create XAML + code-behind in `Views/`
   - Create corresponding ViewModel in `ViewModels/`
   - Add navigation in `MainWindow.xaml.cs`

2. **Add new service**:
   - Create service class in `Services/`
   - Inject into ViewModels as needed

3. **Add new model**:
   - Create model class in `Models/`
   - Inherit from `ObservableObject` for data binding

## Packaging for Distribution

### Create MSIX Package

1. In Visual Studio:
   - Right-click VideoPlayer project
   - Select **Publish** → **Create App Packages**
   - Follow the wizard

2. Using command line:
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained
   ```

### Sideloading Package

To install on other machines:
1. Copy the generated `.msix` or `.appx` file
2. Double-click to install
3. May require enabling Developer Mode or sideloading in Windows Settings

## Additional Resources

- **WinUI 3 Documentation**: https://learn.microsoft.com/windows/apps/winui/
- **Windows App SDK**: https://learn.microsoft.com/windows/apps/windows-app-sdk/
- **MVVM Toolkit**: https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/
- **FFMpegCore**: https://github.com/rosenbjerg/FFMpegCore

## Support

For issues or questions:
1. Check existing issues on GitHub
2. Create new issue with:
   - Windows version
   - Visual Studio version
   - Complete error message
   - Steps to reproduce

## License

This project is part of the HentaiDownloader repository. See the main repository LICENSE file for details.
