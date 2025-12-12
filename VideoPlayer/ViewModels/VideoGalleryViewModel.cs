using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VideoPlayer.Models;
using VideoPlayer.Services;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace VideoPlayer.ViewModels;

/// <summary>
/// ViewModel for the video gallery page
/// </summary>
public partial class VideoGalleryViewModel : ObservableObject
{
    private readonly VideoScannerService _scannerService;
    private readonly ThumbnailService _thumbnailService;
    private IntPtr _windowHandle;

    [ObservableProperty]
    private ObservableCollection<VideoItem> videos = new();

    [ObservableProperty]
    private VideoItem? selectedVideo;

    [ObservableProperty]
    private string currentFolderPath = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = "No folder selected. Click 'Select Folder' to begin.";

    public VideoGalleryViewModel()
    {
        _scannerService = new VideoScannerService();
        _thumbnailService = new ThumbnailService();
    }

    /// <summary>
    /// Sets the window handle for file picker dialogs
    /// </summary>
    public void SetWindowHandle(IntPtr handle)
    {
        _windowHandle = handle;
    }

    /// <summary>
    /// Opens folder picker and scans for videos
    /// </summary>
    [RelayCommand]
    private async Task SelectFolderAsync()
    {
        var folderPicker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.VideosLibrary,
            ViewMode = PickerViewMode.List
        };
        
        folderPicker.FileTypeFilter.Add("*");

        // Initialize picker with window handle
        InitializeWithWindow.Initialize(folderPicker, _windowHandle);

        var folder = await folderPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            await ScanFolderAsync(folder.Path);
        }
    }

    /// <summary>
    /// Scans a folder for video files
    /// </summary>
    private async Task ScanFolderAsync(string folderPath)
    {
        IsLoading = true;
        StatusMessage = "Scanning folder...";
        Videos.Clear();

        try
        {
            CurrentFolderPath = folderPath;
            var videoItems = await _scannerService.ScanFolderAsync(folderPath);

            StatusMessage = $"Found {videoItems.Count} videos. Loading metadata...";

            // Load videos in batches
            var batchSize = 10;
            for (int i = 0; i < videoItems.Count; i += batchSize)
            {
                var batch = videoItems.Skip(i).Take(batchSize).ToList();
                
                // Load metadata and thumbnails for batch
                var tasks = batch.Select(async video =>
                {
                    await _scannerService.GetVideoMetadataAsync(video);
                    var thumbnailPath = await _thumbnailService.GenerateThumbnailAsync(video);
                    video.ThumbnailPath = thumbnailPath;
                    return video;
                });

                var completedVideos = await Task.WhenAll(tasks);
                
                // Add to collection on UI thread
                foreach (var video in completedVideos)
                {
                    Videos.Add(video);
                }

                StatusMessage = $"Loaded {Videos.Count} of {videoItems.Count} videos...";
            }

            StatusMessage = $"Loaded {Videos.Count} videos from {folderPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error scanning folder: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Refreshes the current folder
    /// </summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (!string.IsNullOrEmpty(CurrentFolderPath))
        {
            await ScanFolderAsync(CurrentFolderPath);
        }
    }

    /// <summary>
    /// Clears the thumbnail cache
    /// </summary>
    [RelayCommand]
    private void ClearCache()
    {
        _thumbnailService.ClearCache();
        StatusMessage = "Thumbnail cache cleared.";
    }
}
