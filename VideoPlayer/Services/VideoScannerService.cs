using VideoPlayer.Models;

namespace VideoPlayer.Services;

/// <summary>
/// Service for scanning video files in a directory
/// </summary>
public class VideoScannerService
{
    private readonly string[] _supportedExtensions = 
    {
        ".mp4", ".mkv", ".avi", ".wmv", ".mov", ".flv", 
        ".webm", ".m4v", ".mpg", ".mpeg", ".3gp"
    };

    /// <summary>
    /// Scans a directory for video files
    /// </summary>
    /// <param name="folderPath">Path to the folder to scan</param>
    /// <param name="recursive">Whether to scan subdirectories</param>
    /// <returns>List of video items found</returns>
    public async Task<List<VideoItem>> ScanFolderAsync(string folderPath, bool recursive = true)
    {
        return await Task.Run(() =>
        {
            var videoItems = new List<VideoItem>();

            try
            {
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = Directory.GetFiles(folderPath, "*.*", searchOption)
                    .Where(f => _supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .ToList();

                foreach (var file in files)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        var videoItem = new VideoItem
                        {
                            FilePath = file,
                            Name = Path.GetFileNameWithoutExtension(file),
                            FileSize = fileInfo.Length
                        };

                        videoItems.Add(videoItem);
                    }
                    catch
                    {
                        // Skip files that can't be accessed
                        continue;
                    }
                }
            }
            catch
            {
                // Return empty list if folder can't be accessed
            }

            return videoItems;
        });
    }

    /// <summary>
    /// Gets video metadata using FFmpeg
    /// </summary>
    /// <param name="videoItem">Video item to update with metadata</param>
    public async Task GetVideoMetadataAsync(VideoItem videoItem)
    {
        try
        {
            var mediaInfo = await FFMpegCore.FFProbe.AnalyseAsync(videoItem.FilePath);
            videoItem.Duration = mediaInfo.Duration;
        }
        catch
        {
            // Set default duration if metadata can't be read
            videoItem.Duration = TimeSpan.Zero;
        }
    }
}
