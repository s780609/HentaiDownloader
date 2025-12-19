# Photo Manager Feature Guide

## Overview

The Photo Manager feature is a new addition to the VideoPlayer application that provides a YouTube-style interface for browsing and viewing photos from local folders.

## Features

### 📸 Photo Browsing
- **Folder Selection**: Choose any folder containing photos
- **Recursive Scanning**: Automatically scans subfolders for images
- **Multiple Formats**: Supports JPG, JPEG, PNG, GIF, BMP, WEBP, TIFF, and ICO
- **Grid View**: YouTube-style grid layout with photo cards
- **Thumbnail Preview**: Auto-generated thumbnails for quick browsing
- **Photo Information**: Displays file name, dimensions, file size

### 🔍 Photo Preview
- **Full-Screen View**: Click any photo to view it in full size
- **Zoom Support**: Pinch-to-zoom or use scroll wheel to zoom in/out
- **Navigation**: Previous/Next buttons to browse through photos
- **Photo Details**: View file information in the preview mode

### ⚙️ Configuration
- **appsettings.json**: Configure default photo path
- **Custom Extensions**: Add or remove supported image formats
- **Persistent Settings**: Settings are saved between app restarts

## Architecture

### Project Structure

```
VideoPlayer/
├── appsettings.json              # Configuration file
├── Pages/
│   ├── PhotoManagerPage.xaml    # Photo manager UI
│   ├── PhotoManagerPage.xaml.cs # Photo manager code-behind
│   ├── VideoManagerPage.xaml    # Video manager UI (moved from MainWindow)
│   └── VideoManagerPage.xaml.cs # Video manager code-behind
├── Models/
│   ├── PhotoItem.cs              # Photo data model
│   └── VideoItem.cs              # Video data model (existing)
├── Services/
│   ├── ConfigurationService.cs   # Configuration reading service
│   ├── PhotoScannerService.cs    # Photo folder scanning service
│   └── PhotoThumbnailService.cs  # Photo thumbnail generation service
├── ViewModels/
│   └── PhotoManagerViewModel.cs  # Photo manager view model (MVVM)
└── MainWindow.xaml               # Main window with navigation
```

### MVVM Pattern

The photo manager follows the Model-View-ViewModel (MVVM) pattern:

- **Model**: `PhotoItem` - represents photo metadata
- **View**: `PhotoManagerPage.xaml` - XAML UI definition
- **ViewModel**: `PhotoManagerViewModel` - business logic and state management

### Services Layer

Three main services handle the photo management functionality:

1. **ConfigurationService**: Reads settings from appsettings.json
2. **PhotoScannerService**: Scans folders and identifies image files
3. **PhotoThumbnailService**: Generates thumbnails and loads full images

## Configuration

### appsettings.json

The configuration file is located at `VideoPlayer/appsettings.json`:

```json
{
  "PhotoSettings": {
    "PhotoPath": "C:\\Photos",
    "SupportedExtensions": [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".ico"]
  }
}
```

#### Configuration Options

- **PhotoPath**: Default folder to load photos from on startup
- **SupportedExtensions**: List of image file extensions to recognize

### Customization

To add support for additional image formats:

1. Open `appsettings.json`
2. Add the file extension to the `SupportedExtensions` array
3. Save the file and restart the application

Example:
```json
"SupportedExtensions": [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".ico", ".svg"]
```

## Usage

### Accessing Photo Manager

1. Launch the VideoPlayer application
2. Click on **"照片管理"** (Photo Manager) in the navigation pane
3. The photo manager page will open

### Browsing Photos

1. Click **"選擇資料夾"** (Select Folder) button
2. Choose a folder containing photos
3. Wait for the photos to load (progress indicator shows loading status)
4. Scroll through the grid to view all photos

### Viewing Photos

1. Click on any photo thumbnail in the grid
2. The photo will open in full-screen preview mode
3. Use the following controls:
   - **返回** (Back): Return to grid view
   - **←** (Previous): View previous photo
   - **→** (Next): View next photo
   - **Mouse Wheel**: Zoom in/out
   - **Drag**: Pan when zoomed in

### Navigation Between Features

Use the navigation pane on the left to switch between:
- **影片管理** (Video Manager): Video browsing and playback
- **照片管理** (Photo Manager): Photo browsing and viewing

## Technical Details

### Photo Loading Strategy

1. **Folder Scanning**: Background thread scans folder for image files
2. **Batch Loading**: Photos are added to the collection immediately
3. **Lazy Thumbnail Loading**: Thumbnails are loaded asynchronously as needed
4. **UI Update**: ObservableCollection ensures UI updates automatically

### Performance Optimizations

- **Async Operations**: All I/O operations are asynchronous
- **Thumbnail Caching**: Windows handles thumbnail caching automatically
- **Parallel Loading**: Multiple thumbnails load simultaneously
- **Responsive UI**: Loading doesn't block user interaction

### Error Handling

The photo manager handles various error scenarios:

- **Missing Folder**: Shows empty state message
- **No Permissions**: Displays error dialog
- **Corrupted Files**: Skips files that cannot be loaded
- **Network Paths**: Supports UNC paths for network drives

## Integration with Existing Features

### Navigation Structure

The application now uses `NavigationView` for switching between features:

```
MainWindow (NavigationView)
├── Video Manager (VideoManagerPage)
└── Photo Manager (PhotoManagerPage)
```

### Shared Components

Both video and photo managers share:
- **BoolToVisibilityConverter**: Converts boolean values to visibility
- **Window Management**: Unified window size and backdrop settings
- **Navigation Pattern**: Consistent navigation experience

## Code Examples

### Loading Photos from Configuration

```csharp
var configService = new ConfigurationService();
var defaultPath = configService.GetPhotoPath();
var supportedExtensions = configService.GetSupportedImageExtensions();

var scannerService = new PhotoScannerService(supportedExtensions);
var photos = await scannerService.ScanFolderAsync(defaultPath);
```

### Displaying Photo Preview

```csharp
var thumbnailService = new PhotoThumbnailService();
var fullImage = await thumbnailService.LoadFullImageAsync(photo.FilePath);
PreviewImage.Source = fullImage;
```

## Troubleshooting

### Photos Not Loading

**Problem**: Photos don't appear after selecting a folder

**Solutions**:
1. Check if the folder path is correct
2. Verify file extensions match supported formats
3. Ensure you have read permissions for the folder
4. Check if files are actually image files (not renamed)

### Thumbnails Not Showing

**Problem**: Grid shows placeholder icons instead of thumbnails

**Solutions**:
1. Wait for thumbnails to load (they load asynchronously)
2. Check if Windows thumbnail service is enabled
3. Verify image files are not corrupted
4. Try clearing Windows thumbnail cache

### Preview Not Opening

**Problem**: Clicking photo doesn't open preview

**Solutions**:
1. Check if file still exists at the path
2. Verify you have read permissions
3. Try with a different image format
4. Check application logs for error messages

### Configuration Not Applied

**Problem**: Settings in appsettings.json are ignored

**Solutions**:
1. Verify JSON syntax is correct
2. Check if file is copied to output directory
3. Ensure file encoding is UTF-8
4. Restart the application after changes

## Future Enhancements

Potential features for future versions:

- [ ] Search and filter functionality
- [ ] Sorting options (by name, date, size, type)
- [ ] Folder tree view for quick navigation
- [ ] Photo metadata editing (EXIF data)
- [ ] Slideshow mode
- [ ] Image comparison view
- [ ] Tags and categories
- [ ] Favorites/bookmarks
- [ ] Export and share options
- [ ] Image editing tools (crop, rotate, adjust)

## Requirements

- Windows 10 (build 17763+) or Windows 11
- .NET 10.0 Runtime
- Windows App SDK 1.8 or later
- Read permissions for photo folders

## Known Limitations

1. **Windows Only**: WinUI 3 is Windows-specific
2. **Local Files Only**: Does not support web URLs or cloud storage directly
3. **No RAW Support**: RAW camera formats require additional codecs
4. **Memory Usage**: Large images consume more memory in preview mode
5. **Subfolder Depth**: Very deep folder hierarchies may take longer to scan

## Support

For issues or questions:
1. Check this guide first
2. Review application logs in Debug output
3. Check GitHub issues for similar problems
4. Create a new issue with detailed information

## License

This feature is part of the VideoPlayer project, which is included in the HentaiDownloader solution. Refer to the main repository for license information.

---

**Version**: 1.0  
**Last Updated**: 2025-12-19  
**Compatibility**: VideoPlayer with Windows App SDK 1.8+
