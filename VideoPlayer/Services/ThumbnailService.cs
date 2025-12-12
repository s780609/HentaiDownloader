using FFMpegCore;
using VideoPlayer.Models;

namespace VideoPlayer.Services;

/// <summary>
/// Service for generating video thumbnails
/// </summary>
public class ThumbnailService
{
    private readonly string _thumbnailCacheFolder;

    public ThumbnailService()
    {
        // Create cache folder in temp directory
        _thumbnailCacheFolder = Path.Combine(Path.GetTempPath(), "VideoPlayerThumbnails");
        Directory.CreateDirectory(_thumbnailCacheFolder);
    }

    /// <summary>
    /// Generates a thumbnail for the video
    /// </summary>
    /// <param name="videoItem">Video item to generate thumbnail for</param>
    /// <returns>Path to the generated thumbnail</returns>
    public async Task<string?> GenerateThumbnailAsync(VideoItem videoItem)
    {
        try
        {
            // Create unique thumbnail filename based on video path hash
            var hash = GetFileHash(videoItem.FilePath);
            var thumbnailPath = Path.Combine(_thumbnailCacheFolder, $"{hash}.jpg");

            // Check if thumbnail already exists
            if (File.Exists(thumbnailPath))
            {
                return thumbnailPath;
            }

            // Generate thumbnail at 10% of video duration or 5 seconds
            var captureTime = videoItem.Duration.TotalSeconds > 0 
                ? TimeSpan.FromSeconds(Math.Min(videoItem.Duration.TotalSeconds * 0.1, 5))
                : TimeSpan.FromSeconds(1);

            await FFMpeg.SnapshotAsync(
                videoItem.FilePath,
                thumbnailPath,
                captureTime: captureTime,
                size: new System.Drawing.Size(320, 180));

            return thumbnailPath;
        }
        catch
        {
            // Return null if thumbnail generation fails
            return null;
        }
    }

    /// <summary>
    /// Generates a simple hash for the file path
    /// </summary>
    private string GetFileHash(string filePath)
    {
        return Math.Abs(filePath.GetHashCode()).ToString();
    }

    /// <summary>
    /// Clears the thumbnail cache
    /// </summary>
    public void ClearCache()
    {
        try
        {
            if (Directory.Exists(_thumbnailCacheFolder))
            {
                Directory.Delete(_thumbnailCacheFolder, true);
                Directory.CreateDirectory(_thumbnailCacheFolder);
            }
        }
        catch
        {
            // Ignore errors when clearing cache
        }
    }
}
