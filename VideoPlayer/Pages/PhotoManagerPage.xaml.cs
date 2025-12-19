using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VideoPlayer2.Models;
using VideoPlayer2.Services;
using VideoPlayer2.ViewModels;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace VideoPlayer2.Pages
{
    /// <summary>
    /// 照片管理頁面
    /// </summary>
    public sealed partial class PhotoManagerPage : Page
    {
        private readonly ObservableCollection<PhotoItem> _photos = new();
        private readonly PhotoScannerService _scannerService;
        private readonly PhotoThumbnailService _thumbnailService;
        private readonly ConfigurationService _configService;
        private PhotoItem? _currentPhoto;
        private int _currentPhotoIndex = -1;

        public PhotoManagerPage()
        {
            this.InitializeComponent();

            // 初始化服務
            _configService = new ConfigurationService();
            var supportedExtensions = _configService.GetSupportedImageExtensions();
            _scannerService = new PhotoScannerService(supportedExtensions);
            _thumbnailService = new PhotoThumbnailService();

            // 綁定資料源
            PhotosGridView.ItemsSource = _photos;

            // 載入時自動載入預設資料夾
            this.Loaded += PhotoManagerPage_Loaded;
        }

        private async void PhotoManagerPage_Loaded(object sender, RoutedEventArgs e)
        {
            var defaultPath = _configService.GetPhotoPath();
            if (Directory.Exists(defaultPath))
            {
                await LoadPhotosFromFolderAsync(defaultPath);
            }
        }

        /// <summary>
        /// 選擇資料夾按鈕點擊事件
        /// </summary>
        private async void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            folderPicker.FileTypeFilter.Add("*");

            // 取得視窗控制代碼
            var window = App.MainWindow;
            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                await LoadPhotosFromFolderAsync(folder.Path);
            }
        }

        /// <summary>
        /// 從資料夾載入照片
        /// </summary>
        private async Task LoadPhotosFromFolderAsync(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return;

            // 顯示載入中
            LoadingProgressRing.IsActive = true;
            EmptyPanel.Visibility = Visibility.Collapsed;
            PhotoScrollViewer.Visibility = Visibility.Collapsed;
            PhotoPreviewContainer.Visibility = Visibility.Collapsed;

            _photos.Clear();
            CurrentPathTextBlock.Text = folderPath;

            try
            {
                // 掃描資料夾
                var photos = await _scannerService.ScanFolderAsync(folderPath, includeSubfolders: true);

                System.Diagnostics.Debug.WriteLine($"找到 {photos.Count} 張照片");

                // 加入照片到集合
                foreach (var photo in photos)
                {
                    _photos.Add(photo);
                }

                // 更新照片數量
                PhotoCountTextBlock.Text = $"{_photos.Count} 張照片";

                // 非同步載入所有縮圖
                foreach (var photo in _photos.ToList())
                {
                    _ = LoadThumbnailForPhotoAsync(photo);
                }

                // 顯示照片網格
                if (_photos.Count > 0)
                {
                    PhotoScrollViewer.Visibility = Visibility.Visible;
                    EmptyPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    EmptyPanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"載入照片失敗: {ex.Message}");
                EmptyPanel.Visibility = Visibility.Visible;
            }
            finally
            {
                LoadingProgressRing.IsActive = false;
            }
        }

        /// <summary>
        /// 載入單張照片的縮圖
        /// </summary>
        private async Task LoadThumbnailForPhotoAsync(PhotoItem photo)
        {
            await _thumbnailService.LoadThumbnailAsync(photo);

            // 觸發 UI 更新
            var index = _photos.IndexOf(photo);
            if (index >= 0)
            {
                _photos.RemoveAt(index);
                _photos.Insert(index, photo);
            }
        }

        /// <summary>
        /// 照片項目點擊事件
        /// </summary>
        private async void PhotoItem_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is PhotoItem photo)
            {
                await ShowPhotoPreviewAsync(photo);
            }
        }

        /// <summary>
        /// 顯示照片預覽
        /// </summary>
        private async Task ShowPhotoPreviewAsync(PhotoItem photo)
        {
            try
            {
                _currentPhoto = photo;
                _currentPhotoIndex = _photos.IndexOf(photo);

                // 載入完整圖片
                var fullImage = await _thumbnailService.LoadFullImageAsync(photo.FilePath);
                if (fullImage != null)
                {
                    PreviewImage.Source = fullImage;
                }

                // 顯示照片資訊
                PreviewTitleText.Text = photo.Title;
                PreviewInfoText.Text = $"{photo.DimensionsText} • {photo.FileSize} • {photo.ModifiedDateString}";

                // 切換到預覽視圖
                PhotoScrollViewer.Visibility = Visibility.Collapsed;
                PhotoPreviewContainer.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"顯示照片失敗: {ex.Message}");

                var dialog = new ContentDialog
                {
                    Title = "錯誤",
                    Content = $"無法顯示照片: {ex.Message}",
                    CloseButtonText = "確定",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        /// <summary>
        /// 關閉預覽按鈕點擊事件
        /// </summary>
        private void ClosePreviewButton_Click(object sender, RoutedEventArgs e)
        {
            // 切換回網格視圖
            PhotoPreviewContainer.Visibility = Visibility.Collapsed;
            PhotoScrollViewer.Visibility = Visibility.Visible;
            PreviewImage.Source = null;
        }

        /// <summary>
        /// 上一張照片按鈕點擊事件
        /// </summary>
        private async void PreviousPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_photos.Count == 0) return;

            _currentPhotoIndex--;
            if (_currentPhotoIndex < 0)
            {
                _currentPhotoIndex = _photos.Count - 1;
            }

            await ShowPhotoPreviewAsync(_photos[_currentPhotoIndex]);
        }

        /// <summary>
        /// 下一張照片按鈕點擊事件
        /// </summary>
        private async void NextPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_photos.Count == 0) return;

            _currentPhotoIndex++;
            if (_currentPhotoIndex >= _photos.Count)
            {
                _currentPhotoIndex = 0;
            }

            await ShowPhotoPreviewAsync(_photos[_currentPhotoIndex]);
        }
    }
}
