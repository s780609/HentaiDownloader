using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VideoPlayer2.Models;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace VideoPlayer2.Pages
{
    /// <summary>
    /// 影片管理頁面
    /// </summary>
    public sealed partial class VideoManagerPage : Page
    {
        private readonly ObservableCollection<VideoItem> _videos = new();
        private VideoItem? _currentVideo;
        private int _currentVideoIndex = -1;

        public VideoManagerPage()
        {
            this.InitializeComponent();

            // 綁定資料源
            VideosGridView.ItemsSource = _videos;
            VideosListView.ItemsSource = _videos;

            // 載入時自動載入預設資料夾
            this.Loaded += VideoManagerPage_Loaded;
        }

        private async void VideoManagerPage_Loaded(object sender, RoutedEventArgs e)
        {
            var defaultFolder = @"C:\Users\a0204\Documents\H";
            if (Directory.Exists(defaultFolder))
            {
                await LoadVideosFromFolderAsync(defaultFolder);
            }
        }

        /// <summary>
        /// 選擇資料夾按鈕點擊
        /// </summary>
        private async void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            folderPicker.FileTypeFilter.Add("*");

            // 取得視窗控制代碼
            var window = App.MainWindow;
            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                await LoadVideosFromFolderAsync(folder.Path);
            }
        }

        /// <summary>
        /// 從資料夾載入影片
        /// </summary>
        private async Task LoadVideosFromFolderAsync(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return;

            // 顯示載入中
            LoadingProgressRing.IsActive = true;
            EmptyPanel.Visibility = Visibility.Collapsed;
            VideoListContainer.Visibility = Visibility.Collapsed;
            PlayerContainer.Visibility = Visibility.Collapsed;
            BackButton.Visibility = Visibility.Collapsed;

            _videos.Clear();
            CurrentPathTextBlock.Text = folderPath;

            try
            {
                // 在背景執行緒搜尋檔案
                var mp4Files = await Task.Run(() =>
                    Directory.GetFiles(folderPath, "*.mp4", SearchOption.AllDirectories));

                System.Diagnostics.Debug.WriteLine($"找到 {mp4Files.Length} 個 MP4 檔案");

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

                    _videos.Add(video);
                }

                // 非同步載入所有縮圖
                foreach (var video in _videos.ToList())
                {
                    _ = LoadThumbnailAsync(video);
                }

                // 顯示影片清單
                if (_videos.Count > 0)
                {
                    VideoListContainer.Visibility = Visibility.Visible;
                    EmptyPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    EmptyPanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"載入影片失敗: {ex.Message}");
                EmptyPanel.Visibility = Visibility.Visible;
            }
            finally
            {
                LoadingProgressRing.IsActive = false;
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

                    // 觸發 UI 更新
                    var index = _videos.IndexOf(video);
                    if (index >= 0)
                    {
                        _videos.RemoveAt(index);
                        _videos.Insert(index, video);
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

        /// <summary>
        /// 格子檢視按鈕點擊
        /// </summary>
        private void GridViewButton_Click(object sender, RoutedEventArgs e)
        {
            GridViewToggleButton.IsChecked = true;
            ListViewToggleButton.IsChecked = false;
            VideosGridView.Visibility = Visibility.Visible;
            VideosListView.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 列表檢視按鈕點擊
        /// </summary>
        private void ListViewButton_Click(object sender, RoutedEventArgs e)
        {
            ListViewToggleButton.IsChecked = true;
            GridViewToggleButton.IsChecked = false;
            VideosGridView.Visibility = Visibility.Collapsed;
            VideosListView.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 影片項目點擊
        /// </summary>
        private async void VideoItem_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is VideoItem video)
            {
                await PlayVideoAsync(video);
            }
        }

        /// <summary>
        /// 播放影片
        /// </summary>
        private async Task PlayVideoAsync(VideoItem video)
        {
            try
            {
                _currentVideo = video;
                _currentVideoIndex = _videos.IndexOf(video);

                var file = await StorageFile.GetFileFromPathAsync(video.FilePath);
                VideoPlayerElement.Source = MediaSource.CreateFromStorageFile(file);
                CurrentVideoTitleText.Text = video.Title;

                // 切換到播放器視圖
                VideoListContainer.Visibility = Visibility.Collapsed;
                PlayerContainer.Visibility = Visibility.Visible;
                BackButton.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"播放影片失敗: {ex.Message}");

                var dialog = new ContentDialog
                {
                    Title = "錯誤",
                    Content = $"無法播放影片: {ex.Message}",
                    CloseButtonText = "確定",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        /// <summary>
        /// 返回清單按鈕點擊
        /// </summary>
        private void BackToListButton_Click(object sender, RoutedEventArgs e)
        {
            // 停止播放
            VideoPlayerElement.Source = null;

            // 切換回清單視圖
            PlayerContainer.Visibility = Visibility.Collapsed;
            VideoListContainer.Visibility = Visibility.Visible;
            BackButton.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 上一個影片按鈕點擊
        /// </summary>
        private async void PreviousVideoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_videos.Count == 0) return;

            _currentVideoIndex--;
            if (_currentVideoIndex < 0)
            {
                _currentVideoIndex = _videos.Count - 1;
            }

            await PlayVideoAsync(_videos[_currentVideoIndex]);
        }

        /// <summary>
        /// 下一個影片按鈕點擊
        /// </summary>
        private async void NextVideoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_videos.Count == 0) return;

            _currentVideoIndex++;
            if (_currentVideoIndex >= _videos.Count)
            {
                _currentVideoIndex = 0;
            }

            await PlayVideoAsync(_videos[_currentVideoIndex]);
        }
    }
}
