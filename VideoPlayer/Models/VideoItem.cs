using CommunityToolkit.Mvvm.ComponentModel;

namespace VideoPlayer.Models;

/// <summary>
/// Represents a video file with its metadata
/// </summary>
public partial class VideoItem : ObservableObject
{
    /// <summary>
    /// Full path to the video file
    /// </summary>
    [ObservableProperty]
    private string filePath = string.Empty;

    /// <summary>
    /// Display name of the video
    /// </summary>
    [ObservableProperty]
    private string name = string.Empty;

    /// <summary>
    /// Duration of the video in TimeSpan format
    /// </summary>
    [ObservableProperty]
    private TimeSpan duration;

    /// <summary>
    /// File size in bytes
    /// </summary>
    [ObservableProperty]
    private long fileSize;

    /// <summary>
    /// Path to the thumbnail image
    /// </summary>
    [ObservableProperty]
    private string? thumbnailPath;

    /// <summary>
    /// Formatted file size string (e.g., "1.5 GB")
    /// </summary>
    public string FormattedFileSize
    {
        get
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = FileSize;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    /// <summary>
    /// Formatted duration string (e.g., "1:23:45")
    /// </summary>
    public string FormattedDuration
    {
        get
        {
            if (Duration.TotalHours >= 1)
                return Duration.ToString(@"h\:mm\:ss");
            else
                return Duration.ToString(@"m\:ss");
        }
    }
}
