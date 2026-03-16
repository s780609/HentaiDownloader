using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
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
        private readonly ObservableCollection<VideoGroup> _videoGroups = new();
        private List<VideoItem> _allVideos = new();
        private VideoItem? _currentVideo;
        private int _currentVideoIndex = -1;
        private bool _sortByDateDescending = true;
        private string _searchText = string.Empty;
        private const double SimilarityThreshold = 0.99;

        // UI 參考
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
        private TextBox? _searchTextBox;
        private Button? _sortButton;
        private FontIcon? _sortIcon;
        private GridView? _groupedGridView;
        private Grid? _groupDetailPanel;
        private TextBlock? _groupDetailTitle;
        private GridView? _groupDetailGridView;
        private Button? _groupDetailBackButton;
        private bool _isInitialized = false;

        public MainWindow()
        {
            InitializeComponent();
            
            // 設定視窗大小
            var appWindow = this.AppWindow;
            appWindow.Resize(new Windows.Graphics.SizeInt32(1400, 900));

            // 在 Activated 事件中初始化控制項
            this.Activated += MainWindow_Activated;
        }

        private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (_isInitialized) return;
            _isInitialized = true;

            InitializeControls();

            var defaultFolder = @"C:\Users\a0204\Documents\H";
            if (Directory.Exists(defaultFolder))
            {
                   await LoadVideosFromFolderAsync(defaultFolder);
            }
        }

        // 舊的事件處理器（已棄用）
        private async void RootElement_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeControls();
            
            // 自動載入預設資料夾
            var defaultFolder = @"C:\Users\a0204\Documents\H";
            try
            {
                if (Directory.Exists(defaultFolder))
                {
                    System.Diagnostics.Debug.WriteLine($"正在載入預設資料夾: {defaultFolder}");
                    await LoadVideosFromFolderAsync(defaultFolder);
                    System.Diagnostics.Debug.WriteLine($"載入完成，影片數量: {_videos.Count}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"預設資料夾不存在: {defaultFolder}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"載入預設資料夾失敗: {ex.Message}");
            }
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
                _searchTextBox = root.FindName("SearchTextBox") as TextBox;
                _sortButton = root.FindName("SortButton") as Button;
                _sortIcon = root.FindName("SortIcon") as FontIcon;
                _groupedGridView = root.FindName("GroupedGridView") as GridView;
                _groupDetailPanel = root.FindName("GroupDetailPanel") as Grid;
                _groupDetailTitle = root.FindName("GroupDetailTitle") as TextBlock;
                _groupDetailGridView = root.FindName("GroupDetailGridView") as GridView;
                _groupDetailBackButton = root.FindName("GroupDetailBackButton") as Button;

                // 綁定資料源
                if (_videosGridView != null)
                    _videosGridView.ItemsSource = _videos;
                if (_videosListView != null)
                    _videosListView.ItemsSource = _videos;
                if (_groupedGridView != null)
                    _groupedGridView.ItemsSource = _videoGroups;
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

            // 取得視窗代碼
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
            _allVideos.Clear();
            _videoGroups.Clear();
            if (_currentPathTextBlock != null) _currentPathTextBlock.Text = folderPath;

            try
            {
                // 在背景執行緒搜尋檔案，避免阻塞 UI
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

                    _allVideos.Add(video);
                }

                System.Diagnostics.Debug.WriteLine($"已加入 {_allVideos.Count} 個影片到列表");

                // 套用排序與篩選
                ApplyFilterAndSort();

                // 建立相似度群組
                BuildSimilarityGroups();

                // 非同步載入所有縮圖
                foreach (var video in _allVideos.ToList())
                {
                    _ = LoadThumbnailAsync(video);
                }

                // 顯示影片清單
                if (_allVideos.Count > 0)
                {
                    if (_videoListContainer != null) _videoListContainer.Visibility = Visibility.Visible;
                    if (_emptyPanel != null) _emptyPanel.Visibility = Visibility.Collapsed;
                    System.Diagnostics.Debug.WriteLine("顯示影片列表");
                }
                else
                {
                    if (_emptyPanel != null) _emptyPanel.Visibility = Visibility.Visible;
                    System.Diagnostics.Debug.WriteLine("顯示空白面板（無影片）");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"載入影片失敗: {ex.Message}\n{ex.StackTrace}");
                if (_emptyPanel != null) _emptyPanel.Visibility = Visibility.Visible;
            }
            finally
            {
                if (_loadingProgressRing != null) _loadingProgressRing.IsActive = false;
            }
        }

        /// <summary>
        /// 套用篩選與排序
        /// </summary>
        private void ApplyFilterAndSort()
        {
            var filtered = _allVideos.AsEnumerable();

            // 文字搜尋篩選
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var searchLower = _searchText.ToLowerInvariant();
                filtered = filtered.Where(v =>
                    v.Title.ToLowerInvariant().Contains(searchLower) ||
                    v.FileName.ToLowerInvariant().Contains(searchLower) ||
                    v.FolderName.ToLowerInvariant().Contains(searchLower));
            }

            // 按時間排序
            filtered = _sortByDateDescending
                ? filtered.OrderByDescending(v => v.ModifiedDate)
                : filtered.OrderBy(v => v.ModifiedDate);

            _videos.Clear();
            foreach (var video in filtered)
            {
                _videos.Add(video);
            }
        }

        /// <summary>
        /// 建立名稱相似度群組
        /// </summary>
        private void BuildSimilarityGroups()
        {
            _videoGroups.Clear();

            var videoList = _videos.ToList();
            var assignedVideoIndices = new HashSet<int>();

            for (int i = 0; i < videoList.Count; i++)
            {
                if (assignedVideoIndices.Contains(i)) continue;

                var group = new VideoGroup
                {
                    GroupTitle = videoList[i].Title
                };
                group.Videos.Add(videoList[i]);
                assignedVideoIndices.Add(i);

                for (int j = i + 1; j < videoList.Count; j++)
                {
                    if (assignedVideoIndices.Contains(j)) continue;

                    double similarity = CalculateSimilarity(videoList[i].Title, videoList[j].Title);
                    if (similarity >= SimilarityThreshold)
                    {
                        group.Videos.Add(videoList[j]);
                        assignedVideoIndices.Add(j);
                    }
                }

                _videoGroups.Add(group);
            }
        }

        /// <summary>
        /// 計算兩個字串的相似度 (0.0 ~ 1.0)，使用 Levenshtein 距離
        /// </summary>
        private static double CalculateSimilarity(string a, string b)
        {
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return 1.0;
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0.0;

            int maxLen = Math.Max(a.Length, b.Length);
            if (maxLen == 0) return 1.0;

            int distance = LevenshteinDistance(a, b);
            return 1.0 - (double)distance / maxLen;
        }

        /// <summary>
        /// 計算 Levenshtein 編輯距離
        /// </summary>
        private static int LevenshteinDistance(string a, string b)
        {
            int[,] dp = new int[a.Length + 1, b.Length + 1];

            for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) dp[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                    dp[i, j] = Math.Min(
                        Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                        dp[i - 1, j - 1] + cost);
                }
            }

            return dp[a.Length, b.Length];
        }

        /// <summary>
        /// 搜尋文字變更事件
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                _searchText = textBox.Text;
                ApplyFilterAndSort();
                BuildSimilarityGroups();
            }
        }

        /// <summary>
        /// 排序按鈕點擊事件
        /// </summary>
        private void SortButton_Click(object sender, RoutedEventArgs e)
        {
            _sortByDateDescending = !_sortByDateDescending;

            // 更新排序圖示
            if (_sortIcon != null)
            {
                _sortIcon.Glyph = _sortByDateDescending ? "\uE74B" : "\uE74A";
            }

            ApplyFilterAndSort();
            BuildSimilarityGroups();
        }

        /// <summary>
        /// 群組卡片點擊事件
        /// </summary>
        private async void GroupItem_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is VideoGroup group)
            {
                if (group.IsGroup)
                {
                    // 顯示群組詳情面板
                    if (_groupDetailPanel != null)
                    {
                        _groupDetailPanel.Visibility = Visibility.Visible;
                        if (_groupDetailTitle != null)
                            _groupDetailTitle.Text = $"{group.GroupTitle} ({group.Count} 個影片)";
                        if (_groupDetailGridView != null)
                            _groupDetailGridView.ItemsSource = group.Videos;
                    }
                    // 隱藏主列表
                    if (_videosGridView != null) _videosGridView.Visibility = Visibility.Collapsed;
                    if (_videosListView != null) _videosListView.Visibility = Visibility.Collapsed;
                    if (_groupedGridView != null) _groupedGridView.Visibility = Visibility.Collapsed;
                }
                else if (group.RepresentativeVideo != null)
                {
                    // 單一影片直接播放
                    await PlayVideoAsync(group.RepresentativeVideo);
                }
            }
        }

        /// <summary>
        /// 群組詳情中的影片點擊事件
        /// </summary>
        private async void GroupDetailVideoItem_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is VideoItem video)
            {
                await PlayVideoAsync(video);
            }
        }

        /// <summary>
        /// 群組詳情返回按鈕
        /// </summary>
        private void GroupDetailBackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_groupDetailPanel != null)
                _groupDetailPanel.Visibility = Visibility.Collapsed;

            // 恢復主列表顯示
            if (_groupedGridView != null) _groupedGridView.Visibility = Visibility.Visible;
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

                    // 重新插入以更新 UI
                    var index = _videos.IndexOf(video);
                    if (index >= 0)
                    {
                        _videos.RemoveAt(index);
                        _videos.Insert(index, video);
                    }

                    // 同時更新群組中的顯示
                    for (int gIdx = 0; gIdx < _videoGroups.Count; gIdx++)
                    {
                        if (_videoGroups[gIdx].RepresentativeVideo == video)
                        {
                            var group = _videoGroups[gIdx];
                            _videoGroups.RemoveAt(gIdx);
                            _videoGroups.Insert(gIdx, group);
                            break;
                        }
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

                // 切換到播放介面
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

            // 切換回清單介面
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
