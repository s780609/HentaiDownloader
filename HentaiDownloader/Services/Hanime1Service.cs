using System.Text.RegularExpressions;
using System.Web;
using PuppeteerSharp;

namespace HentaiDownloader.Services;

/// <summary>
/// Hanime1.me 影片清單服務
/// </summary>
public static class Hanime1Service
{
    /// <summary>
    /// 影片資訊
    /// </summary>
    public class VideoInfo
    {
        public string Url { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    /// <summary>
    /// 取得指定年月的裏番清單
    /// </summary>
    /// <param name="year">年份 (可為 null 代表不限制)</param>
    /// <param name="month">月份 (可為 null 代表不限制)</param>
    /// <param name="query">搜尋關鍵字 (可為 null 或空字串)</param>
    public static async Task<List<VideoInfo>> GetVideosAsync(int? year, int? month, string? query = null)
    {
        // 建立搜尋 URL
        string encodedQuery = string.IsNullOrWhiteSpace(query) ? "" : HttpUtility.UrlEncode(query);
        string encodedDate = "";
        
        if (year.HasValue && month.HasValue)
        {
            // 日期格式: "2025 年 11 月" -> URL encoded
            string dateParam = $"{year.Value} 年 {month.Value} 月";
            encodedDate = HttpUtility.UrlEncode(dateParam);
        }
        
        string searchUrl = $"https://hanime1.me/search?query={encodedQuery}&type=&genre=%E8%A3%8F%E7%95%AA&sort=&date={encodedDate}&duration=";

        if (!string.IsNullOrWhiteSpace(query))
        {
            Console.WriteLine($"🔍 搜尋關鍵字: {query}");
        }
        if (year.HasValue && month.HasValue)
        {
            Console.WriteLine($"📅 正在取得 {year.Value} 年 {month.Value} 月 的裏番清單...");
        }
        else
        {
            Console.WriteLine($"📅 正在取得裏番清單...");
        }
        Console.WriteLine($"🔗 網址: {searchUrl}");

        var videos = new List<VideoInfo>();

        try
        {
            // 使用 PuppeteerSharp 取得頁面內容
            Console.WriteLine("🌐 正在載入頁面...");

            // 確保瀏覽器已下載（使用明確快取路徑，避免單一檔案發布時找不到瀏覽器）
            var browserFetcher = BrowserHelper.CreateBrowserFetcher();
            var installedBrowser = browserFetcher.GetInstalledBrowsers().FirstOrDefault();
            if (installedBrowser == null)
            {
                Console.WriteLine("📥 首次使用，正在下載瀏覽器...");
                await browserFetcher.DownloadAsync();
            }

            var launchOptions = new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            };

            await using var browser = await Puppeteer.LaunchAsync(launchOptions);
            await using var page = await browser.NewPageAsync();

            // 設定 User-Agent
            await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            // 載入頁面
            await page.GoToAsync(searchUrl, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle2 },
                Timeout = 60000
            });

            // 等待影片清單載入
            await page.WaitForSelectorAsync(".home-rows-videos-wrapper", new WaitForSelectorOptions { Timeout = 30000 });

            // 取得 HTML 內容
            string html = await page.GetContentAsync();

            // 解析影片清單
            videos = ParseVideoList(html);

            Console.WriteLine($"✅ 找到 {videos.Count} 部影片");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 取得清單失敗: {ex.Message}");
        }

        return videos;
    }

    /// <summary>
    /// 解析影片清單 HTML
    /// </summary>
    private static List<VideoInfo> ParseVideoList(string html)
    {
        var videos = new List<VideoInfo>();

        // 找到 home-rows-videos-wrapper 區塊
        var wrapperMatch = Regex.Match(html, @"<div class=""home-rows-videos-wrapper""[^>]*>(.*?)</div>\s*</div>\s*</div>", RegexOptions.Singleline);
        if (!wrapperMatch.Success)
        {
            // 嘗試另一種方式
            wrapperMatch = Regex.Match(html, @"class=""home-rows-videos-wrapper""[^>]*>(.+)", RegexOptions.Singleline);
        }

        string content = wrapperMatch.Success ? wrapperMatch.Groups[1].Value : html;

        // 解析每個影片連結 - 只抓取 hanime1.me/watch 的連結
        var linkPattern = @"<a[^>]*href=""(https://hanime1\.me/watch\?v=\d+)""[^>]*>.*?<div class=""home-rows-videos-title""[^>]*>([^<]+)</div>";
        var matches = Regex.Matches(content, linkPattern, RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            string url = match.Groups[1].Value;
            string title = match.Groups[2].Value.Trim();

            // 清理標題中的 HTML entities
            title = HttpUtility.HtmlDecode(title);

            videos.Add(new VideoInfo
            {
                Url = url,
                Title = title
            });
        }

        return videos;
    }

    /// <summary>
    /// 顯示影片清單並讓使用者選擇
    /// </summary>
    public static List<VideoInfo> SelectVideos(List<VideoInfo> videos)
    {
        Console.WriteLine();
        Console.WriteLine("========== 影片清單 ==========");
        for (int i = 0; i < videos.Count; i++)
        {
            Console.WriteLine($"[{i + 1}] {videos[i].Title}");
        }
        Console.WriteLine("==============================");
        Console.WriteLine();
        Console.WriteLine("請選擇要下載的影片:");
        Console.WriteLine("  - 輸入數字選擇單一影片 (例如: 1)");
        Console.WriteLine("  - 輸入範圍選擇多部影片 (例如: 1-5)");
        Console.WriteLine("  - 輸入多個數字用逗號分隔 (例如: 1,3,5)");
        Console.WriteLine("  - 輸入 'all' 下載全部");
        Console.WriteLine("  - 輸入 'q' 取消");
        Console.Write("您的選擇: ");

        string? input = Console.ReadLine()?.Trim().ToLower();

        if (string.IsNullOrEmpty(input) || input == "q")
        {
            return new List<VideoInfo>();
        }

        var selectedVideos = new List<VideoInfo>();

        if (input == "all")
        {
            return videos;
        }

        // 解析選擇
        try
        {
            // 處理逗號分隔
            var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var trimmedPart = part.Trim();

                // 處理範圍 (例如: 1-5)
                if (trimmedPart.Contains('-'))
                {
                    var range = trimmedPart.Split('-');
                    if (range.Length == 2 && int.TryParse(range[0], out int start) && int.TryParse(range[1], out int end))
                    {
                        for (int i = start; i <= end && i <= videos.Count; i++)
                        {
                            if (i > 0 && !selectedVideos.Contains(videos[i - 1]))
                            {
                                selectedVideos.Add(videos[i - 1]);
                            }
                        }
                    }
                }
                // 處理單一數字
                else if (int.TryParse(trimmedPart, out int index))
                {
                    if (index > 0 && index <= videos.Count && !selectedVideos.Contains(videos[index - 1]))
                    {
                        selectedVideos.Add(videos[index - 1]);
                    }
                }
            }
        }
        catch
        {
            Console.WriteLine("輸入格式錯誤");
        }

        return selectedVideos;
    }

    /// <summary>
    /// 從影片標題生成檔名
    /// </summary>
    public static string GenerateFileName(string title)
    {
        // 移除 Windows 檔名中的非法字元
        char[] invalidChars = Path.GetInvalidFileNameChars();
        string sanitizedName = title;
        foreach (char c in invalidChars)
        {
            sanitizedName = sanitizedName.Replace(c.ToString(), "");
        }

        // 移除前後空白
        sanitizedName = sanitizedName.Trim();

        // 如果為空，使用時間戳
        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            sanitizedName = $"video_{DateTime.Now:yyyyMMdd_HHmmss}";
        }

        return sanitizedName;
    }
}
