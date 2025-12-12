namespace HentaiDownloader.Services;

/// <summary>
/// 處理使用者輸入相關的服務
/// </summary>
public static class UserInputService
{
    // 直接影片連結的副檔名
    private static readonly string[] DirectVideoExtensions = { ".m3u8", ".mp4", ".mpd", ".ts", ".webm", ".flv" };

    /// <summary>
    /// 檢查 URL 是否為直接影片連結
    /// </summary>
    private static bool IsDirectVideoUrl(string url)
    {
        // 移除查詢參數後檢查副檔名
        var urlWithoutQuery = url.Split('?')[0].ToLowerInvariant();
        return DirectVideoExtensions.Any(ext => urlWithoutQuery.EndsWith(ext));
    }

    /// <summary>
    /// 取得影片 URL (自動判斷是直接連結還是需要從網頁提取)
    /// 回傳 (影片URL, 原始輸入URL)
    /// </summary>
    public static async Task<(string? videoUrl, string? inputUrl)> GetVideoUrlAsync()
    {
        Console.Write("請輸入 URL: ");
        string? inputUrl = Console.ReadLine();

        // 驗證 URL
        if (string.IsNullOrWhiteSpace(inputUrl))
        {
            Console.WriteLine("輸入不能為空");
            return (null, null);
        }

        if (!inputUrl.StartsWith("http"))
        {
            Console.WriteLine("請輸入有效的 URL");
            return (null, null);
        }

        // 自動判斷 URL 類型
        if (IsDirectVideoUrl(inputUrl))
        {
            Console.WriteLine($"✅ 偵測到直接影片連結");
            return (inputUrl, inputUrl);
        }
        else
        {
            Console.WriteLine($"🔍 偵測到網頁連結，將自動提取影片...");
            var videoUrl = await VideoExtractorService.ExtractVideoUrlFromPageAsync(inputUrl);
            return (videoUrl, inputUrl);
        }
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
    /// 取得輸出檔案名稱 (支援 Unicode)
    /// 如果是 jable.tv 網址，自動從 URL 提取檔名
    /// </summary>
    public static string GetOutputFileName(string? sourceUrl = null)
    {
        // 檢查是否為 jable.tv 網址，自動提取檔名
        if (!string.IsNullOrEmpty(sourceUrl) && sourceUrl.Contains("jable.tv"))
        {
            string? autoName = ExtractJableFileName(sourceUrl);
            if (!string.IsNullOrEmpty(autoName))
            {
                Console.WriteLine($"✅ 自動偵測檔名: {autoName}");
                return autoName;
            }
        }

        Console.Write("請輸入輸出檔案名稱 (不含副檔名，支援中文/日文/空格): ");
        
        // 使用支援 Unicode 的讀取方法
        string? outputName = ConsoleService.ReadLineUnicode();

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

    /// <summary>
    /// 從 jable.tv URL 提取檔名
    /// 範例: https://jable.tv/videos/ipx-811/ => ipx-811
    /// </summary>
    private static string? ExtractJableFileName(string url)
    {
        try
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            // jable.tv URL 格式: /videos/{video-id}/
            // 找到 videos 後面的那個 segment
            for (int i = 0; i < segments.Length - 1; i++)
            {
                if (segments[i].Equals("videos", StringComparison.OrdinalIgnoreCase))
                {
                    return segments[i + 1];
                }
            }
            
            // 如果沒有找到 videos，就取最後一個非空的 segment
            if (segments.Length > 0)
            {
                return segments[^1];
            }
        }
        catch
        {
            // 解析失敗
        }
        
        return null;
    }
}
