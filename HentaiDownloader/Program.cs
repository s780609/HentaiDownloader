using HentaiDownloader.Services;

class Program
{
    static async Task Main(string[] args)
    {
        // Step 1: 初始化控制台環境
        ConsoleService.InitializeConsole();

        // Step 2: 顯示歡迎訊息
        ConsoleService.ShowWelcomeMessage();

        // Step 3: 載入應用程式設定
        AppSettings.Initialize();
        Console.WriteLine();

        // Step 4: 檢查並安裝 FFmpeg
        await FFmpegService.EnsureFFmpegAsync();

        // Step 5: 選擇模式
        int mode = SelectMode();

        if (mode == 1)
        {
            // 模式 1: 手動輸入 URL 下載
            await ManualDownloadModeAsync();
        }
        else if (mode == 2)
        {
            // 模式 2: 批量下載上個月裏番
            await BatchDownloadModeAsync();
        }
    }

    /// <summary>
    /// 選擇下載模式
    /// </summary>
    static int SelectMode()
    {
        Console.WriteLine();
        Console.WriteLine("========== 請選擇下載模式 ==========");
        Console.WriteLine("[1] 手動輸入 URL 下載");
        Console.WriteLine("[2] 批量下載 Hanime1 上個月裏番");
        Console.WriteLine("=====================================");
        Console.Write("請選擇 (1 或 2): ");

        string? input = Console.ReadLine()?.Trim();

        if (input == "2")
        {
            return 2;
        }

        return 1; // 預設模式 1
    }

    /// <summary>
    /// 手動輸入 URL 下載模式
    /// </summary>
    static async Task ManualDownloadModeAsync()
    {
        // 主迴圈：持續下載直到使用者選擇結束
        while (true)
        {
            // 取得影片 URL
            var (videoUrl, inputUrl) = await UserInputService.GetVideoUrlAsync();
            if (string.IsNullOrEmpty(videoUrl))
            {
                continue;
            }

            // 取得輸出檔名 (jable.tv 自動從原始輸入 URL 取得)
            string outputName = UserInputService.GetOutputFileName(inputUrl);

            // 下載影片
            await DownloadService.DownloadVideoAsync(videoUrl, outputName);

            // 詢問是否繼續
            Console.WriteLine();
            Console.Write("是否繼續下載下一個影片? (Y/N，直接按 Enter 繼續): ");
            string? choice = Console.ReadLine()?.Trim().ToUpper();
            
            if (choice == "N")
            {
                Console.WriteLine("感謝使用，再見！");
                break;
            }
            
            Console.WriteLine();
        }
    }

    /// <summary>
    /// 批量下載 Hanime1 裏番
    /// </summary>
    static async Task BatchDownloadModeAsync()
    {
        // 輸入搜尋關鍵字 (可選)
        Console.WriteLine();
        Console.Write("請輸入搜尋關鍵字 (直接按 Enter 跳過): ");
        string? query = Console.ReadLine()?.Trim();

        // 選擇年月 (預設為上個月，如果有輸入 query 則可選擇不限制日期)
        var (year, month) = SelectYearMonth(!string.IsNullOrWhiteSpace(query));

        // 取得指定年月的影片清單
        var videos = await Hanime1Service.GetVideosAsync(year, month, query);

        if (videos.Count == 0)
        {
            Console.WriteLine("找不到任何影片");
            return;
        }

        // 讓使用者選擇要下載的影片
        var selectedVideos = Hanime1Service.SelectVideos(videos);

        if (selectedVideos.Count == 0)
        {
            Console.WriteLine("未選擇任何影片");
            return;
        }

        Console.WriteLine();
        Console.WriteLine($"即將下載 {selectedVideos.Count} 部影片...");
        Console.WriteLine();

        int successCount = 0;
        int failCount = 0;

        for (int i = 0; i < selectedVideos.Count; i++)
        {
            var video = selectedVideos[i];
            Console.WriteLine($"========== [{i + 1}/{selectedVideos.Count}] ==========");
            Console.WriteLine($"標題: {video.Title}");
            Console.WriteLine($"網址: {video.Url}");

            try
            {
                // 從網頁提取影片 URL
                string? videoUrl = await VideoExtractorService.ExtractVideoUrlFromPageAsync(video.Url);

                if (string.IsNullOrEmpty(videoUrl))
                {
                    Console.WriteLine("❌ 無法提取影片連結");
                    failCount++;
                    continue;
                }

                // 生成檔名
                string outputName = Hanime1Service.GenerateFileName(video.Title);

                // 下載影片
                await DownloadService.DownloadVideoAsync(videoUrl, outputName);
                successCount++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 下載失敗: {ex.Message}");
                failCount++;
            }

            Console.WriteLine();
        }

        Console.WriteLine("========== 下載完成 ==========");
        Console.WriteLine($"✅ 成功: {successCount} 部");
        Console.WriteLine($"❌ 失敗: {failCount} 部");
        Console.WriteLine("==============================");
    }

    /// <summary>
    /// 選擇年份和月份 (預設為上個月)
    /// </summary>
    /// <param name="allowSkipDate">是否允許跳過日期選擇 (不限制日期)</param>
    static (int? year, int? month) SelectYearMonth(bool allowSkipDate = false)
    {
        var lastMonth = DateTime.Now.AddMonths(-1);
        int defaultYear = lastMonth.Year;
        int defaultMonth = lastMonth.Month;

        Console.WriteLine();
        
        if (allowSkipDate)
        {
            Console.WriteLine($"預設: {defaultYear} 年 {defaultMonth} 月 (輸入 'skip' 可跳過日期限制)");
        }
        else
        {
            Console.WriteLine($"預設: {defaultYear} 年 {defaultMonth} 月");
        }
        
        Console.Write($"請輸入年份 (直接按 Enter 使用 {defaultYear}): ");
        string? yearInput = Console.ReadLine()?.Trim();

        // 如果輸入 skip，則不限制日期
        if (allowSkipDate && yearInput?.ToLower() == "skip")
        {
            Console.WriteLine("📅 不限制日期");
            return (null, null);
        }
        
        int year = defaultYear;
        if (!string.IsNullOrEmpty(yearInput) && int.TryParse(yearInput, out int parsedYear))
        {
            if (parsedYear >= 2000 && parsedYear <= DateTime.Now.Year)
            {
                year = parsedYear;
            }
            else
            {
                Console.WriteLine($"年份無效，使用預設值 {defaultYear}");
            }
        }

        Console.Write($"請輸入月份 (1-12，直接按 Enter 使用 {defaultMonth}): ");
        string? monthInput = Console.ReadLine()?.Trim();
        
        int month = defaultMonth;
        if (!string.IsNullOrEmpty(monthInput) && int.TryParse(monthInput, out int parsedMonth))
        {
            if (parsedMonth >= 1 && parsedMonth <= 12)
            {
                month = parsedMonth;
            }
            else
            {
                Console.WriteLine($"月份無效，使用預設值 {defaultMonth}");
            }
        }

        Console.WriteLine($"📅 已選擇: {year} 年 {month} 月");
        return (year, month);
    }
}
