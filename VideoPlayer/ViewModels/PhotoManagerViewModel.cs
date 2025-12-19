using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using VideoPlayer2.Models;
using VideoPlayer2.Services;

namespace VideoPlayer2.ViewModels;

/// <summary>
/// 照片管理 ViewModel
/// </summary>
public class PhotoManagerViewModel : INotifyPropertyChanged
{
    private readonly PhotoScannerService _scannerService;
    private readonly PhotoThumbnailService _thumbnailService;
    private readonly ConfigurationService _configService;
    private string _currentFolderPath = string.Empty;
    private bool _isLoading = false;
    private PhotoItem? _selectedPhoto;

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 照片集合
    /// </summary>
    public ObservableCollection<PhotoItem> Photos { get; } = new();

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
    /// 選中的照片
    /// </summary>
    public PhotoItem? SelectedPhoto
    {
        get => _selectedPhoto;
        set
        {
            if (_selectedPhoto != value)
            {
                _selectedPhoto = value;
                OnPropertyChanged();
            }
        }
    }

    public PhotoManagerViewModel()
    {
        _configService = new ConfigurationService();
        var supportedExtensions = _configService.GetSupportedImageExtensions();
        _scannerService = new PhotoScannerService(supportedExtensions);
        _thumbnailService = new PhotoThumbnailService();
    }

    /// <summary>
    /// 從資料夾載入照片
    /// </summary>
    public async Task LoadPhotosFromFolderAsync(string folderPath)
    {
        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            return;

        IsLoading = true;
        Photos.Clear();
        CurrentFolderPath = folderPath;

        try
        {
            // 掃描資料夾
            var photos = await _scannerService.ScanFolderAsync(folderPath, includeSubfolders: true);

            // 加入照片到集合
            foreach (var photo in photos)
            {
                Photos.Add(photo);
            }

            // 非同步載入所有縮圖
            foreach (var photo in Photos.ToList())
            {
                _ = LoadThumbnailForPhotoAsync(photo);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"載入照片失敗: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 載入單張照片的縮圖
    /// </summary>
    private async Task LoadThumbnailForPhotoAsync(PhotoItem photo)
    {
        // PhotoItem 實作 INotifyPropertyChanged，屬性變更會自動更新 UI
        await _thumbnailService.LoadThumbnailAsync(photo);
    }

    /// <summary>
    /// 載入配置檔案中的預設路徑
    /// </summary>
    public async Task LoadDefaultFolderAsync()
    {
        var defaultPath = _configService.GetPhotoPath();
        if (Directory.Exists(defaultPath))
        {
            await LoadPhotosFromFolderAsync(defaultPath);
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
