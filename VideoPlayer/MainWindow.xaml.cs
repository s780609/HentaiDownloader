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
using VideoPlayer2.Services;
using Windows.Media.Core;
using Windows.Media.Playback;
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
        private readonly PlaybackLogService _playbackLogService = new();
        private VideoItem? _currentVideo;
        private int _currentVideoIndex = -1;
        private bool _isSeekingToSavedPosition = false;

        // UI ���
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
        private bool _isInitialized = false;

        public MainWindow()
        {
            InitializeComponent();
            
            // �]�w�����j�p
            var appWindow = this.AppWindow;
            appWindow.Resize(new Windows.Graphics.SizeInt32(1400, 900));

            // �b Loaded �ƥ󤤪�l�Ʊ���Ѧ�
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

                // �j�w��Ʒ�
                if (_videosGridView != null)
                    _videosGridView.ItemsSource = _videos;
                if (_videosListView != null)
                    _videosListView.ItemsSource = _videos;
            }
        }

        /// <summary>
        /// ��ܸ�Ƨ����s�I��
        /// </summary>
        private async void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.VideosLibrary;
            folderPicker.FileTypeFilter.Add("*");

            // ���o��������N�X
            var hwnd = WindowNative.GetWindowHandle(this);
            InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                await LoadVideosFromFolderAsync(folder.Path);
            }
        }

        /// <summary>
        /// �q��Ƨ����J�v��
        /// </summary>
        private async Task LoadVideosFromFolderAsync(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
                return;

            // ��ܸ��J��
            if (_loadingProgressRing != null) _loadingProgressRing.IsActive = true;
            if (_emptyPanel != null) _emptyPanel.Visibility = Visibility.Collapsed;
            if (_videoListContainer != null) _videoListContainer.Visibility = Visibility.Collapsed;
            if (_playerContainer != null) _playerContainer.Visibility = Visibility.Collapsed;
            if (_backButton != null) _backButton.Visibility = Visibility.Collapsed;

            _videos.Clear();
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

                    _videos.Add(video);
                }

                System.Diagnostics.Debug.WriteLine($"已加入 {_videos.Count} 個影片到列表");

                // 非同步載入所有縮圖
                foreach (var video in _videos.ToList())
                {
                    _ = LoadThumbnailAsync(video);
                }

                // 顯示影片清單
                if (_videos.Count > 0)
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
        /// ���J�v���Y��
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

                    // ���s��z�M�涵�إH��s UI
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
                System.Diagnostics.Debug.WriteLine($"���J�Y�ϥ���: {video.FileName} - {ex.Message}");
            }
        }

        /// <summary>
        /// �榡���ɮפj�p
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
        /// ��l�˵����s�I��
        /// </summary>
        private void GridViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (_gridViewToggleButton != null) _gridViewToggleButton.IsChecked = true;
            if (_listViewToggleButton != null) _listViewToggleButton.IsChecked = false;
            if (_videosGridView != null) _videosGridView.Visibility = Visibility.Visible;
            if (_videosListView != null) _videosListView.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// �C���˵����s�I��
        /// </summary>
        private void ListViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (_listViewToggleButton != null) _listViewToggleButton.IsChecked = true;
            if (_gridViewToggleButton != null) _gridViewToggleButton.IsChecked = false;
            if (_videosGridView != null) _videosGridView.Visibility = Visibility.Collapsed;
            if (_videosListView != null) _videosListView.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// �v�������I��
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
                // 儲存上一個影片的播放位置
                await SaveCurrentPlaybackPositionAsync();

                _currentVideo = video;
                _currentVideoIndex = _videos.IndexOf(video);

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
        /// �W�@�Ӽv�����s�I��
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
        /// �U�@�Ӽv�����s�I��
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
