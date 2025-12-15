using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
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

namespace VideoPlayer2
{
    /// <summary>
    /// 影片播放器主視窗
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private readonly ObservableCollection<VideoItem> _videos = new();
        private VideoItem? _currentVideo;
        private int _currentVideoIndex = -1;

        // UI 控制項
        private GridView? _videosGridView;
        private ListView? _videosListView;
        private ProgressRing? _loadingProgressRing;
        private StackPanel? _emptyPanel;
        private Grid? _videoListContainer;
        private Grid? _playerContainer;
        private Button? _backButton;
        private TextBlock? _currentPathTextBlock;
        private ToggleButton? _gridViewToggleButton;
        private ToggleButton? _listViewToggleButton;
        private MediaPlayerElement? _videoPlayerElement;
        private TextBlock? _currentVideoTitleText;

        public MainWindow()
        {
            InitializeComponent();
            
            // 設定視窗大小
            var appWindow = this.AppWindow;
            appWindow.Resize(new Windows.Graphics.SizeInt32(1400, 900));

            // 在 Loaded 事件中初始化控制項參考
            if (Content is FrameworkElement rootElement)
            {
                rootElement.Loaded += RootElement_Loaded;
            }
        }

        private void RootElement_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeControls();
        }

        private void InitializeControls()
        {
            if (Content is FrameworkElement root)
            {
                _videosGridView = root.FindName("VideosGridView") as GridView;
                _videosListView = root.FindName("VideosListView") as ListView;
                _loadingProgressRing = root.FindName("LoadingProgressRing") as ProgressRing;
                _emptyPanel = root.FindName("EmptyPanel") as StackPanel;
                _videoListContainer = root.FindName("VideoListContainer") as Grid;
                _playerContainer = root.FindName("PlayerContainer") as Grid;
                _backButton = root.FindName("BackButton") as Button;
                _currentPathTextBlock = root.FindName("CurrentPathTextBlock") as TextBlock;
                _gridViewToggleButton = root.FindName("GridViewToggleButton") as ToggleButton;
                _listViewToggleButton = root.FindName("ListViewToggleButton") as ToggleButton;
                _videoPlayerElement = root.FindName("VideoPlayerElement") as MediaPlayerElement;
                _currentVideoTitleText = root.FindName("CurrentVideoTitleText") as TextBlock;

                // 綁定資料源
                if (_videosGridView != null)
                    _videosGridView.ItemsSource = _videos;
                if (_videosListView != null)
                    _videosListView.ItemsSource = _videos;
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
            var hwnd = WindowNative.GetWindowHandle(this);
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
            if (_loadingProgressRing != null) _loadingProgressRing.IsActive = true;
            if (_emptyPanel != null) _emptyPanel.Visibility = Visibility.Collapsed;
            if (_videoListContainer != null) _videoListContainer.Visibility = Visibility.Collapsed;
            if (_playerContainer != null) _playerContainer.Visibility = Visibility.Collapsed;
            if (_backButton != null) _backButton.Visibility = Visibility.Collapsed;

            _videos.Clear();
            if (_currentPathTextBlock != null) _currentPathTextBlock.Text = folderPath;

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

                    _videos.Add(video);
                }

                // 異步載入所有縮圖
                foreach (var video in _videos.ToList())
                {
                    _ = LoadThumbnailAsync(video);
                }

                // 顯示影片清單
                if (_videos.Count > 0)
                {
                    if (_videoListContainer != null) _videoListContainer.Visibility = Visibility.Visible;
                    if (_emptyPanel != null) _emptyPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    if (_emptyPanel != null) _emptyPanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"載入影片失敗: {ex.Message}");
                if (_emptyPanel != null) _emptyPanel.Visibility = Visibility.Visible;
            }
            finally
            {
                if (_loadingProgressRing != null) _loadingProgressRing.IsActive = false;
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

                    // 重新整理清單項目以更新 UI
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
            if (_gridViewToggleButton != null) _gridViewToggleButton.IsChecked = true;
            if (_listViewToggleButton != null) _listViewToggleButton.IsChecked = false;
            if (_videosGridView != null) _videosGridView.Visibility = Visibility.Visible;
            if (_videosListView != null) _videosListView.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 列表檢視按鈕點擊
        /// </summary>
        private void ListViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (_listViewToggleButton != null) _listViewToggleButton.IsChecked = true;
            if (_gridViewToggleButton != null) _gridViewToggleButton.IsChecked = false;
            if (_videosGridView != null) _videosGridView.Visibility = Visibility.Collapsed;
            if (_videosListView != null) _videosListView.Visibility = Visibility.Visible;
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
                if (_videoPlayerElement != null)
                    _videoPlayerElement.Source = MediaSource.CreateFromStorageFile(file);

                if (_currentVideoTitleText != null)
                    _currentVideoTitleText.Text = video.Title;

                // 切換到播放器檢視
                if (_videoListContainer != null) _videoListContainer.Visibility = Visibility.Collapsed;
                if (_playerContainer != null) _playerContainer.Visibility = Visibility.Visible;
                if (_backButton != null) _backButton.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"播放影片失敗: {ex.Message}");

                var dialog = new ContentDialog
                {
                    Title = "播放失敗",
                    Content = $"無法播放影片: {ex.Message}",
                    CloseButtonText = "確定",
                    XamlRoot = this.Content.XamlRoot
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
            if (_videoPlayerElement != null)
                _videoPlayerElement.Source = null;

            // 切換回清單檢視
            if (_playerContainer != null) _playerContainer.Visibility = Visibility.Collapsed;
            if (_videoListContainer != null) _videoListContainer.Visibility = Visibility.Visible;
            if (_backButton != null) _backButton.Visibility = Visibility.Collapsed;
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
