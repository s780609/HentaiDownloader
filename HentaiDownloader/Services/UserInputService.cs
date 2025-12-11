namespace HentaiDownloader.Services;

/// <summary>
/// 處理使用者輸入相關的服務
/// </summary>
public static class UserInputService
{
    /// <summary>
    /// 取得影片 URL (透過直接輸入或從網頁提取)
    /// </summary>
    public static async Task<string?> GetVideoUrlAsync()
    {
        Console.WriteLine("請選擇模式:");
        Console.WriteLine("  1. 直接輸入影片 URL (M3U8/MP4/TS)");
        Console.WriteLine("  2. 輸入網頁 URL，自動提取影片連結");
        Console.Write("請選擇 (1 或 2): ");
        string? modeInput = Console.ReadLine();

        string? videoUrl = null;

        if (modeInput == "2")
        {
            videoUrl = await GetVideoUrlFromWebPageAsync();
        }
        else
        {
            videoUrl = GetVideoUrlDirectly();
        }

        // 驗證 URL
        if (string.IsNullOrWhiteSpace(videoUrl))
        {
            Console.WriteLine("輸入不能為空");
            return null;
        }

        if (!videoUrl.StartsWith("http"))
        {
            Console.WriteLine("請輸入有效的 URL");
            return null;
        }

        return videoUrl;
    }

    /// <summary>
    /// 從網頁提取影片 URL
    /// </summary>
    private static async Task<string?> GetVideoUrlFromWebPageAsync()
    {
        Console.Write("請輸入網頁 URL: ");
        string? pageUrl = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(pageUrl) || !pageUrl.StartsWith("http"))
        {
            Console.WriteLine("請輸入有效的 URL");
            return null;
        }

        string? videoUrl = await VideoExtractorService.ExtractVideoUrlFromPageAsync(pageUrl);

        if (string.IsNullOrEmpty(videoUrl))
        {
            Console.WriteLine("無法從網頁中提取影片連結");
            return null;
        }

        Console.WriteLine($"\n找到影片連結: {videoUrl}");
        return videoUrl;
    }

    /// <summary>
    /// 直接輸入影片 URL
    /// </summary>
    private static string? GetVideoUrlDirectly()
    {
        Console.Write("請輸入影片 URL: ");
        return Console.ReadLine();
    }

    /// <summary>
    /// 取得輸出檔案名稱
    /// </summary>
    public static string GetOutputFileName()
    {
        Console.Write("請輸入輸出檔案名稱 (不含副檔名，支援中文/日文/空格): ");
        string? outputName = Console.ReadLine();

        // 如果未輸入，使用預設名稱
        if (string.IsNullOrWhiteSpace(outputName))
        {
            outputName = $"output_{DateTime.Now:yyyyMMdd_HHmmss}";
            return outputName;
        }

        // 移除 Windows 檔名中的非法字元
        char[] invalidChars = Path.GetInvalidFileNameChars();
        string sanitizedName = outputName;
        foreach (char c in invalidChars)
        {
            sanitizedName = sanitizedName.Replace(c.ToString(), "");
        }

        // 如果清理後為空，使用預設名稱
        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            sanitizedName = $"output_{DateTime.Now:yyyyMMdd_HHmmss}";
        }

        return sanitizedName;
    }
}
