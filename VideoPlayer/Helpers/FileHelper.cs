namespace VideoPlayer2.Helpers;

/// <summary>
/// 檔案相關的工具方法
/// </summary>
public static class FileHelper
{
    /// <summary>
    /// 格式化檔案大小為人類可讀的字串
    /// </summary>
    /// <param name="bytes">檔案大小（位元組）</param>
    /// <returns>格式化後的檔案大小字串</returns>
    public static string FormatFileSize(long bytes)
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
