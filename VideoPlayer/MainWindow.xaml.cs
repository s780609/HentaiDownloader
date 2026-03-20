using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VideoPlayer2.Models;
using VideoPlayer2.Services;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace VideoPlayer2
{
    /// <summary>
    /// 影片播放器主視窗
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        // 混合顯示集合 (資料夾 + 影片)
        private readonly ObservableCollection<object> _displayItems = new();
        private readonly ObservableCollection<VideoGroup> _videoGroups = new();
        private readonly PlaybackLogService _playbackLogService = new();

        // 當前資料夾中的影片 (用於播放器上下切換)
        private List<VideoItem> _currentFolderVideos = new();
        private VideoItem? _currentVideo;
        private int _currentVideoIndex = -1;
        private bool _isSeekingToSavedPosition = false;
        private bool _sortByDateDescending = true;
        private string _searchText = string.Empty;
        private const double SimilarityThreshold = 0.99;

        // 資料夾導航狀態
        private string _rootFolderPath = string.Empty;
        private string _currentFolderPath = string.Empty;

        // 播放器控制列自動隱藏
        private DispatcherTimer? _controlsHideTimer;
        private Button? _hideControlsButton;
        private static readonly TimeSpan ControlsHideDelay = TimeSpan.FromSeconds(1.5);
        private static readonly TimeSpan SkipInterval = TimeSpan.FromSeconds(5);

        // UI 參考
        private GridView? _videosGridView;
        private ListView? _videosListView;
        private ProgressRing? _loadingProgressRing;
        private StackPanel? _emptyPanel;
        private Grid? _videoListContainer;
        private Grid? _playerContainer;
        private Button? _backButton;
        private Button? _navigateUpButton;
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

            // 載入播放紀錄
            await _playbackLogService.LoadAsync();

            var defaultFolder = @"C:\Users\a0204\Documents\H";
            if (Directory.Exists(defaultFolder))
            {
                _rootFolderPath = defaultFolder;
                await NavigateToFolderAsync(defaultFolder);
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
                _navigateUpButton = root.FindName("NavigateUpButton") as Button;
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

                _hideControlsButton = root.FindName("HideControlsButton") as Button;

                // 綁定資料源
                if (_videosGridView != null)
                    _videosGridView.ItemsSource = _displayItems;
                if (_videosListView != null)
                    _videosListView.ItemsSource = _displayItems;
                if (_groupedGridView != null)
                    _groupedGridView.ItemsSource = _videoGroups;

                // 設定播放器控制列自動隱藏
                SetupPlayerControls();
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
                _rootFolderPath = folder.Path;
                await NavigateToFolderAsync(folder.Path);
            }
        }

        /// <summary>
        /// 導航到指定資料夾 (只掃描當前層級)
        /// </summary>
        private async Task NavigateToFolderAsync(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return;

            _currentFolderPath = folderPath;

            // 顯示載入中
            if (_loadingProgressRing != null) _loadingProgressRing.IsActive = true;
            if (_emptyPanel != null) _emptyPanel.Visibility = Visibility.Collapsed;
            if (_videoListContainer != null) _videoListContainer.Visibility = Visibility.Collapsed;
            if (_playerContainer != null) _playerContainer.Visibility = Visibility.Collapsed;
            if (_backButton != null) _backButton.Visibility = Visibility.Collapsed;

            _displayItems.Clear();
            _currentFolderVideos.Clear();
            _videoGroups.Clear();

            // 更新路徑顯示
            if (_currentPathTextBlock != null) _currentPathTextBlock.Text = folderPath;

            // 更新上一層按鈕可見性
            UpdateNavigateUpButton();

            try
            {
                // 在背景執行緒掃描當前資料夾
                var (subFolders, mp4Files) = await Task.Run(() =>
                {
                    var folders = Directory.GetDirectories(folderPath)
                        .OrderBy(f => Path.GetFileName(f))
                        .ToArray();
                    var files = Directory.GetFiles(folderPath, "*.mp4", SearchOption.TopDirectoryOnly);
                    return (folders, files);
                });

                // 建立資料夾項目
                var folderItems = new List<FolderItem>();
                foreach (var subFolder in subFolders)
                {
                    var dirInfo = new DirectoryInfo(subFolder);
                    var folderItem = await Task.Run(() =>
                    {
                        int videoCount = 0;
                        int subFolderCount = 0;
                        try
                        {
                            videoCount = Directory.GetFiles(subFolder, "*.mp4", SearchOption.AllDirectories).Length;
                            subFolderCount = Directory.GetDirectories(subFolder).Length;
                        }
                        catch { }

                        var parts = new List<string>();
                        if (subFolderCount > 0) parts.Add($"{subFolderCount} 個資料夾");
                        if (videoCount > 0) parts.Add($"{videoCount} 個影片");

                        return new FolderItem
                        {
                            FolderPath = subFolder,
                            Name = dirInfo.Name,
                            SubFolderCount = subFolderCount,
                            VideoCount = videoCount,
                            ItemCountText = parts.Count > 0 ? string.Join(", ", parts) : "空資料夾"
                        };
                    });

                    folderItems.Add(folderItem);
                }

                // 建立影片項目
                var videoItems = new List<VideoItem>();
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
                    videoItems.Add(video);
                }

                _currentFolderVideos = videoItems;

                // 套用篩選與排序後加入顯示集合
                ApplyFilterAndSort(folderItems, videoItems);

                // 建立相似度群組 (僅針對影片)
                BuildSimilarityGroups();

                // 非同步載入縮圖
                foreach (var video in videoItems)
                {
                    _ = LoadThumbnailAsync(video);
                }

                // 顯示結果
                if (_displayItems.Count > 0)
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
                System.Diagnostics.Debug.WriteLine($"載入資料夾失敗: {ex.Message}\n{ex.StackTrace}");
                if (_emptyPanel != null) _emptyPanel.Visibility = Visibility.Visible;
            }
            finally
            {
                if (_loadingProgressRing != null) _loadingProgressRing.IsActive = false;
            }
        }

        /// <summary>
        /// 更新上一層按鈕的可見性
        /// </summary>
        private void UpdateNavigateUpButton()
        {
            if (_navigateUpButton == null) return;

            // 不在根目錄時顯示上一層按鈕
            bool canGoUp = !string.IsNullOrEmpty(_rootFolderPath) &&
                           !string.IsNullOrEmpty(_currentFolderPath) &&
                           !string.Equals(_currentFolderPath, _rootFolderPath, StringComparison.OrdinalIgnoreCase);

            _navigateUpButton.Visibility = canGoUp ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// 上一層按鈕點擊
        /// </summary>
        private async void NavigateUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFolderPath)) return;

            var parentPath = Directory.GetParent(_currentFolderPath)?.FullName;
            if (parentPath == null) return;

            // 不允許超出根目錄
            if (!parentPath.StartsWith(_rootFolderPath, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(parentPath, _rootFolderPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            await NavigateToFolderAsync(parentPath);
        }

        /// <summary>
        /// 混合項目 (資料夾或影片) 點擊事件
        /// </summary>
        private async void MixedItem_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is FolderItem folder)
            {
                // 點擊資料夾 → 進入該資料夾
                await NavigateToFolderAsync(folder.FolderPath);
            }
            else if (e.ClickedItem is VideoItem video)
            {
                // 點擊影片 → 播放
                await PlayVideoAsync(video);
            }
        }

        /// <summary>
        /// 套用篩選與排序 (資料夾在前，影片在後)
        /// </summary>
        private void ApplyFilterAndSort(List<FolderItem>? folderItems = null, List<VideoItem>? videoItems = null)
        {
            // 如果沒傳入，從現有的 displayItems 和 _currentFolderVideos 取得
            var folders = folderItems ?? _displayItems.OfType<FolderItem>().ToList();
            var videos = videoItems ?? _currentFolderVideos;

            var filteredFolders = folders.AsEnumerable();
            var filteredVideos = videos.AsEnumerable();

            // 文字搜尋篩選
            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                var searchLower = _searchText.ToLowerInvariant();
                filteredFolders = filteredFolders.Where(f =>
                    f.Name.ToLowerInvariant().Contains(searchLower));
                filteredVideos = filteredVideos.Where(v =>
                    v.Title.ToLowerInvariant().Contains(searchLower) ||
                    v.FileName.ToLowerInvariant().Contains(searchLower) ||
                    v.FolderName.ToLowerInvariant().Contains(searchLower));
            }

            // 影片按時間排序
            filteredVideos = _sortByDateDescending
                ? filteredVideos.OrderByDescending(v => v.ModifiedDate)
                : filteredVideos.OrderBy(v => v.ModifiedDate);

            _displayItems.Clear();

            // 資料夾排在前面
            foreach (var folder in filteredFolders)
            {
                _displayItems.Add(folder);
            }

            // 影片排在後面
            foreach (var video in filteredVideos)
            {
                _displayItems.Add(video);
            }
        }

        /// <summary>
        /// 建立名稱相似度群組
        /// </summary>
        private void BuildSimilarityGroups()
        {
            _videoGroups.Clear();

            var videoList = _displayItems.OfType<VideoItem>().ToList();
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
                    var index = _displayItems.IndexOf(video);
                    if (index >= 0)
                    {
                        _displayItems.RemoveAt(index);
                        _displayItems.Insert(index, video);
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

        #region 播放器控制列與跳轉

        /// <summary>
        /// 設定播放器控制列行為 (自動隱藏 + 鍵盤快捷鍵)
        /// </summary>
        private void SetupPlayerControls()
        {
            // 自動隱藏計時器
            _controlsHideTimer = new DispatcherTimer();
            _controlsHideTimer.Interval = ControlsHideDelay;
            _controlsHideTimer.Tick += (s, e) =>
            {
                _controlsHideTimer.Stop();
                HideTransportControls();
            };

            // 監聽滑鼠移動以自動顯示/隱藏控制列
            if (_playerContainer != null)
            {
                _playerContainer.AddHandler(UIElement.PointerMovedEvent,
                    new PointerEventHandler(PlayerContainer_PointerMoved), true);
                _playerContainer.AddHandler(UIElement.PointerPressedEvent,
                    new PointerEventHandler(PlayerContainer_PointerMoved), true);
            }

            // 全域鍵盤快捷鍵 (左右方向鍵跳 5 秒)
            if (Content is UIElement rootElement)
            {
                rootElement.AddHandler(UIElement.KeyDownEvent,
                    new KeyEventHandler(Content_KeyDown), true);
            }
        }

        private void PlayerContainer_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            ShowTransportControls();
            ResetHideTimer();
        }

        private void ShowTransportControls()
        {
            if (_videoPlayerElement?.TransportControls is MediaTransportControls tc)
            {
                tc.Show();
            }
            if (_hideControlsButton != null)
                _hideControlsButton.Visibility = Visibility.Visible;
        }

        private void HideTransportControls()
        {
            if (_videoPlayerElement?.TransportControls is MediaTransportControls tc)
            {
                tc.Hide();
            }
            if (_hideControlsButton != null)
                _hideControlsButton.Visibility = Visibility.Collapsed;
        }

        private void ResetHideTimer()
        {
            _controlsHideTimer?.Stop();
            _controlsHideTimer?.Start();
        }

        private void HideControlsButton_Click(object sender, RoutedEventArgs e)
        {
            _controlsHideTimer?.Stop();
            HideTransportControls();
        }

        private void Content_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            // 只在播放器可見時處理
            if (_playerContainer?.Visibility != Visibility.Visible) return;

            // 避免在搜尋框等輸入控件中觸發
            if (e.OriginalSource is TextBox) return;

            switch (e.Key)
            {
                case VirtualKey.Left:
                    SkipVideo(TimeSpan.FromSeconds(-5));
                    e.Handled = true;
                    break;
                case VirtualKey.Right:
                    SkipVideo(TimeSpan.FromSeconds(5));
                    e.Handled = true;
                    break;
            }
        }

        private void SkipBackward5s_Click(object sender, RoutedEventArgs e)
        {
            SkipVideo(TimeSpan.FromSeconds(-5));
        }

        private void SkipForward5s_Click(object sender, RoutedEventArgs e)
        {
            SkipVideo(TimeSpan.FromSeconds(5));
        }

        private void SkipVideo(TimeSpan amount)
        {
            if (_videoPlayerElement?.MediaPlayer?.PlaybackSession == null) return;

            var session = _videoPlayerElement.MediaPlayer.PlaybackSession;
            var newPosition = session.Position + amount;

            if (newPosition < TimeSpan.Zero)
                newPosition = TimeSpan.Zero;
            else if (newPosition > session.NaturalDuration)
                newPosition = session.NaturalDuration;

            session.Position = newPosition;
        }

        #endregion

        /// <summary>
        /// 播放影片
        /// </summary>
        private async Task PlayVideoAsync(VideoItem video)
        {
            try
            {
                // 儲存上一個影片的播放位置
                await SaveCurrentPlaybackPositionAsync();

                _currentVideo = video;
                _currentVideoIndex = _currentFolderVideos.IndexOf(video);

                var file = await StorageFile.GetFileFromPathAsync(video.FilePath);
                if (_videoPlayerElement != null)
                {
                    _videoPlayerElement.Source = MediaSource.CreateFromStorageFile(file);

                    // 取得上次播放位置
                    var savedPosition = _playbackLogService.GetPlaybackPosition(video.FilePath);
                    if (savedPosition > 0)
                    {
                        _isSeekingToSavedPosition = true;
                        // 使用 MediaOpened 事件來設定播放位置
                        if (_videoPlayerElement.MediaPlayer != null)
                        {
                            _videoPlayerElement.MediaPlayer.MediaOpened += (s, e) =>
                            {
                                if (_isSeekingToSavedPosition && _currentVideo?.FilePath == video.FilePath)
                                {
                                    _videoPlayerElement.MediaPlayer.Position = TimeSpan.FromSeconds(savedPosition);
                                    _isSeekingToSavedPosition = false;
                                    System.Diagnostics.Debug.WriteLine($"已跳轉到上次播放位置: {savedPosition:F1} 秒");
                                }
                            };
                        }
                    }
                }

                if (_currentVideoTitleText != null)
                    _currentVideoTitleText.Text = video.Title;

                // 切換到播放模式
                if (_videoListContainer != null) _videoListContainer.Visibility = Visibility.Collapsed;
                if (_playerContainer != null) _playerContainer.Visibility = Visibility.Visible;
                if (_backButton != null) _backButton.Visibility = Visibility.Visible;

                // 顯示控制列並啟動自動隱藏
                ShowTransportControls();
                ResetHideTimer();
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
        /// 儲存目前影片的播放位置
        /// </summary>
        private async Task SaveCurrentPlaybackPositionAsync()
        {
            if (_currentVideo != null && _videoPlayerElement?.MediaPlayer != null)
            {
                var position = _videoPlayerElement.MediaPlayer.Position.TotalSeconds;
                var duration = _videoPlayerElement.MediaPlayer.NaturalDuration.TotalSeconds;

                // 只有在有效位置時才儲存（排除剛開始或已播完的情況）
                if (position > 1 && position < duration - 1)
                {
                    await _playbackLogService.SavePlaybackPositionAsync(_currentVideo.FilePath, position);
                    System.Diagnostics.Debug.WriteLine($"已儲存播放位置: {_currentVideo.Title} - {position:F1} 秒");
                }
                else if (position >= duration - 1)
                {
                    // 如果播放完畢，移除紀錄
                    _playbackLogService.RemovePlaybackPosition(_currentVideo.FilePath);
                    await _playbackLogService.SaveAsync();
                    System.Diagnostics.Debug.WriteLine($"影片播放完畢，已移除播放紀錄: {_currentVideo.Title}");
                }
            }
        }

        /// <summary>
        /// 返回清單按鈕點擊
        /// </summary>
        private async void BackToListButton_Click(object sender, RoutedEventArgs e)
        {
            // 儲存目前播放位置
            await SaveCurrentPlaybackPositionAsync();

            // 停止播放
            if (_videoPlayerElement != null)
                _videoPlayerElement.Source = null;

            _currentVideo = null;

            // 切換回清單模式
            if (_playerContainer != null) _playerContainer.Visibility = Visibility.Collapsed;
            if (_videoListContainer != null) _videoListContainer.Visibility = Visibility.Visible;
            if (_backButton != null) _backButton.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 上一個影片按鈕點擊
        /// </summary>
        private async void PreviousVideoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFolderVideos.Count == 0) return;

            _currentVideoIndex--;
            if (_currentVideoIndex < 0)
            {
                _currentVideoIndex = _currentFolderVideos.Count - 1;
            }

            await PlayVideoAsync(_currentFolderVideos[_currentVideoIndex]);
        }

        /// <summary>
        /// 下一個影片按鈕點擊
        /// </summary>
        private async void NextVideoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFolderVideos.Count == 0) return;

            _currentVideoIndex++;
            if (_currentVideoIndex >= _currentFolderVideos.Count)
            {
                _currentVideoIndex = 0;
            }

            await PlayVideoAsync(_currentFolderVideos[_currentVideoIndex]);
        }

        // 舊的事件處理器（保留相容性）
        private async void RootElement_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeControls();
            var defaultFolder = @"C:\Users\a0204\Documents\H";
            try
            {
                if (Directory.Exists(defaultFolder))
                {
                    _rootFolderPath = defaultFolder;
                    await NavigateToFolderAsync(defaultFolder);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"載入預設資料夾失敗: {ex.Message}");
            }
        }
    }
}
