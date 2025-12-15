using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VideoPlayer2.Models;
using Windows.Storage;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.FileProperties;

namespace VideoPlayer2.ViewModels;

/// <summary>
/// 主視窗 ViewModel
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private string _currentFolderPath = @"C:\Users\a0204\Documents\H";
    private VideoItem? _selectedVideo;
    private bool _isGridView = true;
    private bool _isLoading = false;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 影片清單
    /// </summary>
    public ObservableCollection<VideoItem> Videos { get; } = new();

    /// <summary>
    /// 目前資料夾路徑
    /// </summary>
    public string CurrentFolderPath
    {
        get => _currentFolderPath;
        set
        {
            if (_currentFolderPath != value)
            {
                _currentFolderPath = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 選中的影片
    /// </summary>
    public VideoItem? SelectedVideo
    {
        get => _selectedVideo;
        set
        {
            if (_selectedVideo != value)
            {
                _selectedVideo = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否為格子檢視模式
    /// </summary>
    public bool IsGridView
    {
        get => _isGridView;
        set
        {
            if (_isGridView != value)
            {
                _isGridView = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsListView));
            }
        }
    }

    /// <summary>
    /// 是否為列表檢視模式
    /// </summary>
    public bool IsListView => !_isGridView;

    /// <summary>
    /// 是否正在載入
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 從資料夾載入影片
    /// </summary>
    public async Task LoadVideosFromFolderAsync(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            return;

        IsLoading = true;
        Videos.Clear();
        CurrentFolderPath = folderPath;

        try
        {
            // 搜尋所有 MP4 檔案（包含子資料夾）
            var mp4Files = Directory.GetFiles(folderPath, "*.mp4", SearchOption.AllDirectories);

            foreach (var filePath in mp4Files)
            {
                var fileInfo = new FileInfo(filePath);
                var video = new VideoItem
                {
                    FilePath = filePath,
                    FileName = fileInfo.Name,
                    Title = Path.GetFileNameWithoutExtension(filePath),
                    FileSize = FormatFileSize(fileInfo.Length),
                    ModifiedDate = fileInfo.LastWriteTime,
                    FolderName = fileInfo.Directory?.Name ?? string.Empty
                };

                Videos.Add(video);

                // 異步載入縮圖
                _ = LoadThumbnailAsync(video);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"載入影片失敗: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 載入影片縮圖
    /// </summary>
    private async Task LoadThumbnailAsync(VideoItem video)
    {
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(video.FilePath);
            var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.VideosView, 200);

            if (thumbnail != null)
            {
                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(thumbnail);
                video.Thumbnail = bitmapImage;
                
                // 通知 UI 更新
                var index = Videos.IndexOf(video);
                if (index >= 0)
                {
                    Videos[index] = video;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"載入縮圖失敗: {video.FileName} - {ex.Message}");
        }
    }

    /// <summary>
    /// 格式化檔案大小
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;

        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
