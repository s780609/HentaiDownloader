using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VideoPlayer.Models;

namespace VideoPlayer.ViewModels;

/// <summary>
/// ViewModel for the video player page
/// </summary>
public partial class VideoPlayerViewModel : ObservableObject
{
    [ObservableProperty]
    private VideoItem? currentVideo;

    [ObservableProperty]
    private bool isPlaying;

    [ObservableProperty]
    private double volume = 1.0;

    [ObservableProperty]
    private bool isMuted;

    [ObservableProperty]
    private TimeSpan currentPosition;

    [ObservableProperty]
    private double playbackRate = 1.0;

    /// <summary>
    /// Sets the video to play
    /// </summary>
    public void SetVideo(VideoItem video)
    {
        CurrentVideo = video;
        CurrentPosition = TimeSpan.Zero;
    }

    /// <summary>
    /// Toggles play/pause
    /// </summary>
    [RelayCommand]
    private void TogglePlayPause()
    {
        IsPlaying = !IsPlaying;
    }

    /// <summary>
    /// Toggles mute
    /// </summary>
    [RelayCommand]
    private void ToggleMute()
    {
        IsMuted = !IsMuted;
    }

    /// <summary>
    /// Seeks forward by specified seconds
    /// </summary>
    [RelayCommand]
    private void SeekForward(int seconds = 10)
    {
        if (CurrentVideo != null)
        {
            CurrentPosition = TimeSpan.FromSeconds(
                Math.Min(CurrentPosition.TotalSeconds + seconds, CurrentVideo.Duration.TotalSeconds));
        }
    }

    /// <summary>
    /// Seeks backward by specified seconds
    /// </summary>
    [RelayCommand]
    private void SeekBackward(int seconds = 10)
    {
        CurrentPosition = TimeSpan.FromSeconds(Math.Max(0, CurrentPosition.TotalSeconds - seconds));
    }

    /// <summary>
    /// Changes playback speed
    /// </summary>
    [RelayCommand]
    private void ChangePlaybackSpeed(double rate)
    {
        PlaybackRate = Math.Clamp(rate, 0.25, 2.0);
    }
}
