# VideoPlayer - WinUI 3 Local Video Browser and Player

A YouTube-style local video browser and player built with WinUI 3 and the Windows App SDK.

## Features

### 📁 Video Folder Browsing
- Select a folder containing video files
- Recursive scanning of subdirectories
- Support for common video formats: MP4, MKV, AVI, WMV, MOV, FLV, WEBM, M4V, MPG, MPEG, 3GP

### 🖼️ YouTube-Style Gallery View
- Grid view with video thumbnails
- Display video information:
  - Thumbnail preview (auto-generated using FFmpeg)
  - Video name
  - Duration
  - File size
- Smooth scrolling for large video collections

### 🎬 Video Playback
- Built on WinUI 3's native `MediaPlayerElement`
- Full playback controls:
  - Play/Pause
  - Seek bar
  - Volume control
  - Playback speed adjustment
  - Full-screen mode
- Hardware-accelerated playback

## Technology Stack

- **Framework**: WinUI 3 (Windows App SDK)
- **Target Platform**: Windows 10 (version 1809+) / Windows 11
- **.NET Version**: .NET 10.0
- **Architecture**: MVVM pattern using CommunityToolkit.Mvvm
- **Video Processing**: FFMpegCore for thumbnail generation and metadata extraction
- **UI Components**: WinUI 3 controls with Fluent Design

## Project Structure

```
VideoPlayer/
├── App.xaml                        # Application definition
├── App.xaml.cs
├── MainWindow.xaml                 # Main window with navigation
├── MainWindow.xaml.cs
├── Views/
│   ├── VideoGalleryPage.xaml      # Video grid gallery
│   ├── VideoGalleryPage.xaml.cs
│   ├── VideoPlayerPage.xaml       # Video player interface
│   └── VideoPlayerPage.xaml.cs
├── ViewModels/
│   ├── VideoGalleryViewModel.cs   # Gallery logic and state
│   └── VideoPlayerViewModel.cs    # Player logic and state
├── Models/
│   └── VideoItem.cs               # Video metadata model
├── Services/
│   ├── VideoScannerService.cs     # Folder scanning service
│   └── ThumbnailService.cs        # Thumbnail generation service
└── Assets/                        # App icons and resources
```

## Building and Running

### Prerequisites

1. **Windows 10 (version 1809 or later) or Windows 11**
2. **Visual Studio 2022** with:
   - .NET desktop development workload
   - Windows App SDK (included with VS 2022)
3. **.NET 10 SDK** or later
4. **FFmpeg** (automatically downloaded by FFMpegCore NuGet package)

### Build Instructions

1. Open `HentaiDownloader.sln` in Visual Studio 2022
2. Set `VideoPlayer` as the startup project
3. Select the target platform (x64 or ARM64)
4. Build and run (F5)

Alternatively, use the command line:

```bash
cd VideoPlayer
dotnet restore
dotnet build
dotnet run
```

**Note**: This application requires Windows to build and run. It cannot be built or executed on Linux or macOS.

## Usage

1. **Launch the application**
2. **Click "Select Folder"** in the toolbar
3. **Choose a folder** containing video files
4. **Wait for scanning** - The app will scan for videos and generate thumbnails
5. **Browse videos** in the grid view
6. **Click a video thumbnail** to start playback
7. **Use playback controls** to control video playback
8. **Click back button** to return to the gallery

## Configuration

### Thumbnail Cache

Thumbnails are cached in the system temp folder:
- Location: `%TEMP%\VideoPlayerThumbnails\`
- Use "Clear Cache" button to remove cached thumbnails

### Supported Video Formats

The following video formats are supported:
- MP4 (MPEG-4)
- MKV (Matroska)
- AVI (Audio Video Interleave)
- WMV (Windows Media Video)
- MOV (QuickTime)
- FLV (Flash Video)
- WEBM (WebM)
- M4V (iTunes Video)
- MPG/MPEG (MPEG-1/2)
- 3GP (3GPP)

## Dependencies

- **Microsoft.WindowsAppSDK** - Windows App SDK for WinUI 3
- **Microsoft.Windows.SDK.BuildTools** - Windows SDK build tools
- **CommunityToolkit.Mvvm** - MVVM toolkit for simplified ViewModel implementation
- **CommunityToolkit.WinUI.UI.Controls** - Additional WinUI controls
- **FFMpegCore** - FFmpeg wrapper for thumbnail generation and metadata extraction

## Troubleshooting

### Video won't play
- Ensure the video codec is supported by Windows Media Foundation
- Try installing additional codecs (e.g., K-Lite Codec Pack)

### Thumbnails not generating
- FFmpeg binaries will be downloaded automatically on first use
- Check internet connection for initial FFmpeg download
- Verify write permissions to temp folder

### Application won't start
- Ensure Windows App SDK runtime is installed
- Update to the latest version of Windows 10/11
- Install Visual C++ Redistributables

## Future Enhancements

Potential features for future versions:
- Playlist support
- Video metadata editing
- Custom thumbnail selection
- Video search and filtering
- Subtitle support
- Network streaming support
- Video conversion tools

## License

This project is part of the HentaiDownloader solution. Refer to the main repository for license information.
