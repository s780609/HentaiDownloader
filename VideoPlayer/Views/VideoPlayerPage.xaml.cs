using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VideoPlayer.Models;
using VideoPlayer.ViewModels;
using Windows.Media.Core;

namespace VideoPlayer.Views;

/// <summary>
/// Video player page for playing videos
/// </summary>
public sealed partial class VideoPlayerPage : Page
{
    public VideoPlayerViewModel ViewModel { get; }

    public VideoPlayerPage()
    {
        this.InitializeComponent();
        ViewModel = new VideoPlayerViewModel();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is VideoItem video)
        {
            ViewModel.SetVideo(video);
            LoadVideo(video);
        }
    }

    private void LoadVideo(VideoItem video)
    {
        try
        {
            // Create media source from file path
            var source = MediaSource.CreateFromUri(new Uri(video.FilePath));
            MediaPlayer.Source = source;

            // Hide loading indicator when media opens
            MediaPlayer.MediaPlayer.MediaOpened += (s, e) =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    LoadingRing.IsActive = false;
                });
            };

            // Handle media failed
            MediaPlayer.MediaPlayer.MediaFailed += (s, e) =>
            {
                DispatcherQueue.TryEnqueue(async () =>
                {
                    LoadingRing.IsActive = false;
                    var dialog = new ContentDialog
                    {
                        Title = "Playback Error",
                        Content = $"Failed to load video: {e.ErrorMessage}",
                        CloseButtonText = "OK",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                });
            };
        }
        catch (Exception ex)
        {
            LoadingRing.IsActive = false;
            ShowErrorDialog($"Error loading video: {ex.Message}");
        }
    }

    private async void ShowErrorDialog(string message)
    {
        var dialog = new ContentDialog
        {
            Title = "Error",
            Content = message,
            CloseButtonText = "OK",
            XamlRoot = this.XamlRoot
        };
        await dialog.ShowAsync();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            // Stop playback before navigating back
            MediaPlayer.MediaPlayer?.Pause();
            Frame.GoBack();
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        
        // Clean up media player
        if (MediaPlayer.MediaPlayer != null)
        {
            MediaPlayer.MediaPlayer.Pause();
            MediaPlayer.Source = null;
        }
    }
}
