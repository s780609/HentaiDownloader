using FFMpegCore;
using VideoPlayer.Models;

namespace VideoPlayer.Services;

/// <summary>
/// Service for generating video thumbnails
/// </summary>
public class ThumbnailService
{
    private readonly string _thumbnailCacheFolder;
    private const double DefaultCapturePercentage = 0.1; // 10% of video duration
    private const int MaxCaptureTimeSeconds = 5;
    private const int FallbackCaptureTimeSeconds = 1;

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

            // Generate thumbnail at configurable percentage of video duration or max time
            var captureTime = videoItem.Duration.TotalSeconds > 0 
                ? TimeSpan.FromSeconds(Math.Min(videoItem.Duration.TotalSeconds * DefaultCapturePercentage, MaxCaptureTimeSeconds))
                : TimeSpan.FromSeconds(FallbackCaptureTimeSeconds);

            await FFMpeg.SnapshotAsync(
                videoItem.FilePath,
                thumbnailPath,
                captureTime: captureTime,
                size: new System.Drawing.Size(320, 180));

            return thumbnailPath;
        }
        catch (Exception ex) when (ex is FFMpegCore.Exceptions.FFMpegException or IOException or UnauthorizedAccessException)
        {
            // Return null if thumbnail generation fails due to known exceptions
            return null;
        }
    }

    /// <summary>
    /// Generates a hash for the file path to use as cache key
    /// </summary>
    private string GetFileHash(string filePath)
    {
        // Use a more robust hash to avoid collisions
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(filePath);
        var hash = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "").Substring(0, 16);
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
