using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VideoPlayer2.Models;

namespace VideoPlayer2.Services;

/// <summary>
/// 照片掃描服務，負責掃描資料夾中的圖片檔案
/// </summary>
public class PhotoScannerService
{
    private readonly List<string> _supportedExtensions;

    public PhotoScannerService(List<string> supportedExtensions)
    {
        _supportedExtensions = supportedExtensions ?? new List<string> { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
    }

    /// <summary>
    /// 掃描資料夾中的所有圖片檔案
    /// </summary>
    /// <param name="folderPath">資料夾路徑</param>
    /// <param name="includeSubfolders">是否包含子資料夾</param>
    /// <returns>照片項目列表</returns>
    public async Task<List<PhotoItem>> ScanFolderAsync(string folderPath, bool includeSubfolders = true)
    {
        var photos = new List<PhotoItem>();

        if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
        {
            return photos;
        }

        try
        {
            var searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            // 在背景執行緒執行檔案搜尋
            var files = await Task.Run(() =>
            {
                return Directory.GetFiles(folderPath, "*.*", searchOption)
                    .Where(file => _supportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                    .ToArray();
            });

            foreach (var filePath in files)
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    var photo = new PhotoItem
                    {
                        FilePath = filePath,
                        FileName = fileInfo.Name,
                        Title = Path.GetFileNameWithoutExtension(filePath),
                        FileSize = FormatFileSize(fileInfo.Length),
                        ModifiedDate = fileInfo.LastWriteTime,
                        FolderName = fileInfo.Directory?.Name ?? string.Empty
                    };

                    photos.Add(photo);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"載入照片失敗: {filePath} - {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"掃描資料夾失敗: {ex.Message}");
        }

        return photos;
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
}
