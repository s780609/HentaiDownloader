using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Text;
using System.Runtime.InteropServices;
using PuppeteerSharp;

class VideoDownloader
{
    // Windows Console UTF-8 支援
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleCP(uint wCodePageID);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleOutputCP(uint wCodePageID);
    
    private static readonly HttpClient _httpClient = new HttpClient();
    private const int MaxConcurrentDownloads = 10; // 同時下載數量
    
    static async Task Main(string[] args)
    {
        // 在 Windows 上啟用 UTF-8 支援 (必須在任何 Console 操作之前)
        if (OperatingSystem.IsWindows())
        {
            // 使用 P/Invoke 設定控制台程式碼頁為 UTF-8
            SetConsoleCP(65001);        // UTF-8 輸入
            SetConsoleOutputCP(65001);  // UTF-8 輸出
        }
        
        // 設定控制台使用 UTF-8 編碼 (支援中文、日文等多國語言)
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;
        
        Console.WriteLine("=== 影片下載器 / ビデオダウンローダー ===");
        Console.WriteLine("支援 / サポート: M3U8, MP4, TS 等格式");
        Console.WriteLine("支援從網頁自動提取影片連結");
        Console.WriteLine();
        
        // 設定 HttpClient
        _httpClient.Timeout = TimeSpan.FromHours(2); // 大檔案需要更長時間
        
        Console.WriteLine("請選擇模式:");
        Console.WriteLine("1. 直接輸入影片 URL (M3U8/MP4/TS)");
        Console.WriteLine("2. 輸入網頁 URL，自動提取影片連結");
        Console.Write("請選擇 (1 或 2): ");
        string? modeInput = Console.ReadLine();
        
        string? videoUrl = null;
        
        if (modeInput == "2")
        {
            // 網頁提取模式
            Console.Write("請輸入網頁 URL: ");
            string? pageUrl = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(pageUrl) || !pageUrl.StartsWith("http"))
            {
                Console.WriteLine("請輸入有效的 URL");
                return;
            }
            
            videoUrl = await ExtractVideoUrlFromPage(pageUrl);
            
            if (string.IsNullOrEmpty(videoUrl))
            {
                Console.WriteLine("無法從網頁中提取影片連結");
                return;
            }
            
            Console.WriteLine($"\n找到影片連結: {videoUrl}");
        }
        else
        {
            // 直接輸入模式
            Console.Write("請輸入影片 URL: ");
            videoUrl = Console.ReadLine();
        }
        
        if (string.IsNullOrWhiteSpace(videoUrl))
        {
            Console.WriteLine("輸入不能為空");
            return;
        }
        
        if (!videoUrl.StartsWith("http"))
        {
            Console.WriteLine("請輸入有效的 URL");
            return;
        }
        
        Console.Write("請輸入輸出檔案名稱 (不含副檔名，支援中文/日文/空格): ");
        string? outputName = Console.ReadLine();
        
        // 顯示原始輸入以便除錯
        if (!string.IsNullOrEmpty(outputName))
        {
            Console.WriteLine($"[Debug] 原始輸入: '{outputName}' (長度: {outputName.Length})");
            Console.WriteLine($"[Debug] 字元: {string.Join(", ", outputName.Select(c => $"U+{(int)c:X4}"))}");
        }
        
        if (string.IsNullOrWhiteSpace(outputName))
        {
            outputName = $"output_{DateTime.Now:yyyyMMdd_HHmmss}";
        }
        
        // 只移除 Windows 檔名中真正的非法字元
        // 這些字元是: \ / : * ? " < > |
        char[] invalidChars = Path.GetInvalidFileNameChars();
        string originalName = outputName;
        foreach (char c in invalidChars)
        {
            outputName = outputName.Replace(c.ToString(), "");
        }
        
        if (outputName != originalName)
        {
            Console.WriteLine($"[Debug] 移除非法字元後: '{outputName}'");
        }
        
        if (string.IsNullOrWhiteSpace(outputName))
        {
            outputName = $"output_{DateTime.Now:yyyyMMdd_HHmmss}";
        }
        
        // 判斷是 M3U8 還是直接下載的檔案
        if (videoUrl.Contains(".m3u8"))
        {
            // 檢查 FFmpeg 是否存在 (M3U8 需要)
            if (!CheckFFmpeg())
            {
                Console.WriteLine("錯誤: 找不到 FFmpeg，請先安裝 FFmpeg 並加入 PATH 環境變數");
                Console.WriteLine("下載位址: https://ffmpeg.org/download.html");
                return;
            }
            
            await DownloadM3U8(videoUrl, outputName);
        }
        else
        {
            // 直接下載 MP4/TS 等檔案
            await DownloadDirectFile(videoUrl, outputName);
        }
    }
    
    static async Task<string?> ExtractVideoUrlFromPage(string pageUrl)
    {
        Console.WriteLine("正在下載瀏覽器 (首次執行需要較長時間)...");
        
        // 下載 Chromium 瀏覽器
        var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();
        
        Console.WriteLine("正在啟動瀏覽器...");
        
        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true, // 無頭模式
            Args = new[] 
            { 
                "--no-sandbox",
                "--disable-setuid-sandbox",
                "--disable-dev-shm-usage",
                "--disable-web-security",
                "--autoplay-policy=no-user-gesture-required" // 允許自動播放
            }
        });
        
        await using var page = await browser.NewPageAsync();
        
        // 設定 User-Agent 避免被偵測
        await page.SetUserAgentAsync("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        
        // 用於收集網路請求中的影片 URL
        var videoUrls = new List<string>();
        var mediaSourceUrls = new List<string>(); // 用於存放從 MediaSource 攔截到的 URL
        
        // 監聽網路請求，捕捉 m3u8 或影片檔案
        page.Request += (sender, e) =>
        {
            var url = e.Request.Url;
            // 擴展匹配範圍，包含更多可能的影片格式和 HLS/DASH 相關請求
            if (url.Contains(".m3u8") || 
                url.Contains(".mp4") || 
                url.Contains(".ts") ||
                url.Contains(".mpd") ||
                url.Contains(".webm") ||
                url.Contains(".flv") ||
                url.Contains("/playlist") ||
                url.Contains("/manifest") ||
                url.Contains("/master") ||
                url.Contains("video") && (url.Contains(".m3u8") || url.Contains("m3u8")))
            {
                if (!videoUrls.Contains(url))
                {
                    videoUrls.Add(url);
                    Console.WriteLine($"[網路請求] 發現影片: {url}");
                }
            }
        };
        
        // 監聽回應，檢查 Content-Type 來發現影片
        page.Response += (sender, e) =>
        {
            var contentType = e.Response.Headers.GetValueOrDefault("content-type", "");
            var url = e.Response.Url;
            
            if ((contentType.Contains("mpegurl") || 
                 contentType.Contains("application/vnd.apple.mpegurl") ||
                 contentType.Contains("video/") ||
                 contentType.Contains("application/dash+xml")) &&
                !videoUrls.Contains(url))
            {
                videoUrls.Add(url);
                Console.WriteLine($"[回應攔截] 發現影片 (Content-Type: {contentType}): {url}");
            }
        };
        
        try
        {
            // 注入腳本來攔截 MediaSource API (處理 blob URL)
            await page.EvaluateFunctionOnNewDocumentAsync(@"() => {
                // 儲存攔截到的 URL
                window.__interceptedVideoUrls = [];
                
                // 攔截 XMLHttpRequest
                const originalXHROpen = XMLHttpRequest.prototype.open;
                XMLHttpRequest.prototype.open = function(method, url) {
                    if (url && typeof url === 'string') {
                        if (url.includes('.m3u8') || url.includes('.mpd') || url.includes('.mp4') || 
                            url.includes('playlist') || url.includes('manifest') || url.includes('master')) {
                            window.__interceptedVideoUrls.push(url);
                            console.log('[XHR攔截] ' + url);
                        }
                    }
                    return originalXHROpen.apply(this, arguments);
                };
                
                // 攔截 fetch
                const originalFetch = window.fetch;
                window.fetch = function(url, options) {
                    if (url && typeof url === 'string') {
                        if (url.includes('.m3u8') || url.includes('.mpd') || url.includes('.mp4') ||
                            url.includes('playlist') || url.includes('manifest') || url.includes('master')) {
                            window.__interceptedVideoUrls.push(url);
                            console.log('[Fetch攔截] ' + url);
                        }
                    }
                    return originalFetch.apply(this, arguments);
                };
                
                // 攔截 MediaSource (用於 blob URL)
                const originalMediaSource = window.MediaSource;
                if (originalMediaSource) {
                    window.MediaSource = function() {
                        const ms = new originalMediaSource();
                        const originalAddSourceBuffer = ms.addSourceBuffer.bind(ms);
                        ms.addSourceBuffer = function(mimeType) {
                            console.log('[MediaSource] 添加 SourceBuffer: ' + mimeType);
                            window.__mediaSourceMimeType = mimeType;
                            return originalAddSourceBuffer(mimeType);
                        };
                        return ms;
                    };
                    window.MediaSource.isTypeSupported = originalMediaSource.isTypeSupported;
                }
                
                // 攔截 URL.createObjectURL
                const originalCreateObjectURL = URL.createObjectURL;
                URL.createObjectURL = function(obj) {
                    const url = originalCreateObjectURL.call(URL, obj);
                    if (obj instanceof MediaSource) {
                        console.log('[Blob URL 創建] MediaSource blob: ' + url);
                        window.__blobMediaSourceUrl = url;
                    }
                    return url;
                };
                
                // 攔截 video.src 設置
                const originalVideoSrcDescriptor = Object.getOwnPropertyDescriptor(HTMLMediaElement.prototype, 'src');
                Object.defineProperty(HTMLMediaElement.prototype, 'src', {
                    set: function(value) {
                        if (value && typeof value === 'string' && !value.startsWith('blob:')) {
                            window.__interceptedVideoUrls.push(value);
                            console.log('[Video.src 攔截] ' + value);
                        }
                        return originalVideoSrcDescriptor.set.call(this, value);
                    },
                    get: originalVideoSrcDescriptor.get
                });
            }");
            
            Console.WriteLine($"正在載入網頁: {pageUrl}");
            
            // 導航到頁面
            await page.GoToAsync(pageUrl, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle2 },
                Timeout = 60000 // 60 秒超時
            });
            
            // 等待頁面完全載入
            Console.WriteLine("等待頁面渲染...");
            await Task.Delay(3000);
            
            // 嘗試點擊播放按鈕來觸發影片載入
            Console.WriteLine("嘗試觸發影片播放...");
            try
            {
                await page.EvaluateFunctionAsync(@"() => {
                    // 嘗試點擊常見的播放按鈕
                    const playButtons = document.querySelectorAll('[class*=""play""], [id*=""play""], .vjs-big-play-button, .play-button, [aria-label*=""play""], button[class*=""play""]');
                    playButtons.forEach(btn => {
                        try { btn.click(); } catch(e) {}
                    });
                    
                    // 嘗試播放所有 video 元素
                    const videos = document.querySelectorAll('video');
                    videos.forEach(video => {
                        try { 
                            video.muted = true;
                            video.play(); 
                        } catch(e) {}
                    });
                }");
                
                // 等待影片開始載入
                await Task.Delay(3000);
            }
            catch { }
            
            // 獲取從 JavaScript 攔截到的 URL
            var interceptedUrls = await page.EvaluateFunctionAsync<string[]>(@"() => {
                return window.__interceptedVideoUrls || [];
            }");
            
            if (interceptedUrls != null && interceptedUrls.Length > 0)
            {
                Console.WriteLine($"\n從 JavaScript 攔截到 {interceptedUrls.Length} 個 URL:");
                foreach (var url in interceptedUrls)
                {
                    Console.WriteLine($"  [JS攔截] {url}");
                    if (!videoUrls.Contains(url))
                    {
                        videoUrls.Add(url);
                    }
                }
            }
            
            // 嘗試從 video 標籤提取 src
            Console.WriteLine("正在搜尋 video 標籤...");
            
            var videoSources = await page.EvaluateFunctionAsync<string[]>(@"() => {
                const sources = [];
                
                // 從 video 標籤取得 src
                const videos = document.querySelectorAll('video');
                videos.forEach(video => {
                    if (video.src) sources.push(video.src);
                    if (video.currentSrc) sources.push(video.currentSrc);
                    
                    // 檢查 data 屬性
                    if (video.dataset.src) sources.push(video.dataset.src);
                    if (video.dataset.source) sources.push(video.dataset.source);
                    for (let attr of video.attributes) {
                        if (attr.value && (attr.value.includes('.m3u8') || attr.value.includes('.mp4'))) {
                            sources.push(attr.value);
                        }
                    }
                });
                
                // 從 source 標籤取得 src
                const sourceTags = document.querySelectorAll('video source');
                sourceTags.forEach(source => {
                    if (source.src) sources.push(source.src);
                });
                
                // 從 iframe 中嘗試取得 (某些網站會用 iframe 嵌入影片)
                const iframes = document.querySelectorAll('iframe');
                iframes.forEach(iframe => {
                    if (iframe.src && (iframe.src.includes('player') || iframe.src.includes('embed') || iframe.src.includes('video'))) {
                        sources.push('iframe:' + iframe.src);
                    }
                });
                
                // 搜尋頁面中的 script 標籤，尋找可能的影片 URL
                const scripts = document.querySelectorAll('script');
                scripts.forEach(script => {
                    const content = script.textContent || '';
                    // 使用正則表達式尋找 m3u8 URL
                    const m3u8Matches = content.match(/https?:\/\/[^\s\""'<>]+\.m3u8[^\s\""'<>]*/g);
                    if (m3u8Matches) {
                        m3u8Matches.forEach(url => sources.push(url));
                    }
                    // 尋找 mp4 URL
                    const mp4Matches = content.match(/https?:\/\/[^\s\""'<>]+\.mp4[^\s\""'<>]*/g);
                    if (mp4Matches) {
                        mp4Matches.forEach(url => sources.push(url));
                    }
                });
                
                return [...new Set(sources)]; // 去重
            }");
            
            // 顯示找到的來源
            if (videoSources.Length > 0)
            {
                Console.WriteLine($"\n從 HTML 中找到 {videoSources.Length} 個影片來源:");
                for (int i = 0; i < videoSources.Length; i++)
                {
                    string label = videoSources[i].StartsWith("blob:") ? "[Blob URL - 需特殊處理]" : "";
                    Console.WriteLine($"  [{i + 1}] {videoSources[i]} {label}");
                }
            }
            
            // 合併所有找到的 URL
            var allUrls = new List<string>();
            
            // 優先處理直接的影片 URL (非 blob)
            foreach (var src in videoSources)
            {
                if (!src.StartsWith("iframe:") && !src.StartsWith("blob:"))
                {
                    allUrls.Add(src);
                }
            }
            
            // 加入網路請求中找到的 URL
            allUrls.AddRange(videoUrls);
            
            // 如果只有 blob URL，嘗試更長時間等待網路請求
            var blobUrls = videoSources.Where(s => s.StartsWith("blob:")).ToList();
            if (allUrls.Count == 0 && blobUrls.Count > 0)
            {
                Console.WriteLine("\n偵測到 Blob URL，正在等待實際影片來源...");
                Console.WriteLine("(Blob URL 是瀏覽器內部資源，需要攔截其來源)");
                
                // 再等待一段時間讓網路請求完成
                for (int i = 0; i < 5; i++)
                {
                    await Task.Delay(2000);
                    Console.Write($"\r等待中... {(i + 1) * 2} 秒");
                    
                    // 再次檢查攔截到的 URL
                    var moreUrls = await page.EvaluateFunctionAsync<string[]>(@"() => {
                        return window.__interceptedVideoUrls || [];
                    }");
                    
                    if (moreUrls != null)
                    {
                        foreach (var url in moreUrls)
                        {
                            if (!videoUrls.Contains(url) && !url.StartsWith("blob:"))
                            {
                                videoUrls.Add(url);
                                Console.WriteLine($"\n  [新發現] {url}");
                            }
                        }
                    }
                    
                    if (videoUrls.Count > 0) break;
                }
                Console.WriteLine();
                
                allUrls.AddRange(videoUrls);
            }
            
            // 處理 iframe 中的影片
            var iframeSources = videoSources.Where(s => s.StartsWith("iframe:")).ToList();
            if (iframeSources.Count > 0 && allUrls.Count == 0)
            {
                Console.WriteLine("\n發現 iframe 嵌入，嘗試進入 iframe...");
                foreach (var iframeSrc in iframeSources)
                {
                    var iframeUrl = iframeSrc.Substring(7); // 移除 "iframe:" 前綴
                    Console.WriteLine($"正在載入 iframe: {iframeUrl}");
                    
                    try
                    {
                        await page.GoToAsync(iframeUrl, new NavigationOptions
                        {
                            WaitUntil = new[] { WaitUntilNavigation.Networkidle2 },
                            Timeout = 30000
                        });
                        
                        await Task.Delay(2000);
                        
                        // 再次搜尋 video 標籤
                        var iframeVideoSources = await page.EvaluateFunctionAsync<string[]>(@"() => {
                            const sources = [];
                            const videos = document.querySelectorAll('video');
                            videos.forEach(video => {
                                if (video.src) sources.push(video.src);
                                if (video.currentSrc) sources.push(video.currentSrc);
                            });
                            const sourceTags = document.querySelectorAll('video source');
                            sourceTags.forEach(source => {
                                if (source.src) sources.push(source.src);
                            });
                            return [...new Set(sources)];
                        }");
                        
                        foreach (var src in iframeVideoSources)
                        {
                            if (!src.StartsWith("blob:"))
                            {
                                allUrls.Add(src);
                            }
                        }
                        
                        // 也加入網路請求中新發現的 URL
                        allUrls.AddRange(videoUrls.Where(u => !allUrls.Contains(u)));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"載入 iframe 失敗: {ex.Message}");
                    }
                }
            }
            
            // 去重
            allUrls = allUrls.Distinct().ToList();
            
            if (allUrls.Count == 0)
            {
                Console.WriteLine("\n未找到任何影片連結");
                return null;
            }
            
            // 優先選擇 m3u8
            var m3u8Urls = allUrls.Where(u => u.Contains(".m3u8")).ToList();
            if (m3u8Urls.Count > 0)
            {
                if (m3u8Urls.Count == 1)
                {
                    return m3u8Urls[0];
                }
                
                Console.WriteLine("\n找到多個 M3U8 連結，請選擇:");
                for (int i = 0; i < m3u8Urls.Count; i++)
                {
                    Console.WriteLine($"  [{i + 1}] {m3u8Urls[i]}");
                }
                Console.Write($"請選擇 (1-{m3u8Urls.Count}): ");
                if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= m3u8Urls.Count)
                {
                    return m3u8Urls[choice - 1];
                }
                return m3u8Urls[0];
            }
            
            // 如果沒有 m3u8，選擇其他格式
            if (allUrls.Count == 1)
            {
                return allUrls[0];
            }
            
            Console.WriteLine("\n找到多個影片連結，請選擇:");
            for (int i = 0; i < allUrls.Count; i++)
            {
                Console.WriteLine($"  [{i + 1}] {allUrls[i]}");
            }
            Console.Write($"請選擇 (1-{allUrls.Count}): ");
            if (int.TryParse(Console.ReadLine(), out int selected) && selected >= 1 && selected <= allUrls.Count)
            {
                return allUrls[selected - 1];
            }
            return allUrls[0];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"提取影片連結時發生錯誤: {ex.Message}");
            
            // 如果頁面載入失敗，但網路請求中有找到影片 URL
            if (videoUrls.Count > 0)
            {
                Console.WriteLine("\n從網路請求中找到的影片連結:");
                for (int i = 0; i < videoUrls.Count; i++)
                {
                    Console.WriteLine($"  [{i + 1}] {videoUrls[i]}");
                }
                
                if (videoUrls.Count == 1)
                {
                    return videoUrls[0];
                }
                
                Console.Write($"請選擇 (1-{videoUrls.Count}): ");
                if (int.TryParse(Console.ReadLine(), out int choice) && choice >= 1 && choice <= videoUrls.Count)
                {
                    return videoUrls[choice - 1];
                }
                return videoUrls[0];
            }
            
            return null;
        }
    }
    
    static async Task DownloadDirectFile(string url, string outputName)
    {
        Console.WriteLine("偵測到直接下載連結...");
        
        // 從 URL 取得副檔名
        string extension = ".mp4";
        try
        {
            var uri = new Uri(url);
            string path = uri.AbsolutePath;
            if (path.Contains('.'))
            {
                string pathWithoutQuery = path.Split('?')[0];
                string ext = Path.GetExtension(pathWithoutQuery);
                if (!string.IsNullOrEmpty(ext) && ext.Length <= 5)
                {
                    extension = ext;
                }
            }
        }
        catch
        {
            // 解析失敗就用預設 .mp4
        }
        
        string outputFile = Path.Combine(Directory.GetCurrentDirectory(), $"{outputName}{extension}");
        
        Console.WriteLine($"正在下載...");
        Console.WriteLine($"輸出檔案: {outputFile}");
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // 先取得檔案大小
            using var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
            using var headResponse = await _httpClient.SendAsync(headRequest);
            
            long? totalBytes = headResponse.Content.Headers.ContentLength;
            bool supportsRange = headResponse.Headers.AcceptRanges.Contains("bytes");
            
            if (totalBytes.HasValue)
            {
                Console.WriteLine($"檔案大小: {FormatBytes(totalBytes.Value)}");
            }
            
            if (supportsRange && totalBytes.HasValue)
            {
                Console.WriteLine("使用分段下載模式...");
                await DownloadWithRangeRequests(url, outputFile, totalBytes.Value, stopwatch);
            }
            else
            {
                Console.WriteLine("使用串流下載模式...");
                await DownloadWithStream(url, outputFile, totalBytes, stopwatch);
            }
            
            stopwatch.Stop();
            Console.WriteLine();
            Console.WriteLine($"✅ 下載完成: {outputFile}");
            Console.WriteLine($"耗時: {stopwatch.Elapsed.TotalSeconds:F1} 秒");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n下載失敗: {ex.Message}");
        }
    }
    
    static async Task DownloadWithRangeRequests(string url, string outputFile, long totalBytes, Stopwatch stopwatch)
    {
        const int chunkSize = 2 * 1024 * 1024; // 2MB per chunk
        int totalChunks = (int)Math.Ceiling((double)totalBytes / chunkSize);
        
        Console.WriteLine($"分成 {totalChunks} 個區塊下載 (每塊 {FormatBytes(chunkSize)})");
        Console.WriteLine($"使用 {MaxConcurrentDownloads} 個並行下載");
        
        // 預先分配檔案大小
        using var fileStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.Write);
        fileStream.SetLength(totalBytes);
        fileStream.Close();
        
        long downloaded = 0;
        int completedChunks = 0;
        var lockObj = new object();
        
        using var semaphore = new SemaphoreSlim(MaxConcurrentDownloads);
        var downloadTasks = new List<Task>();
        
        for (int i = 0; i < totalChunks; i++)
        {
            int chunkIndex = i;
            long start = (long)chunkIndex * chunkSize;
            long end = Math.Min(start + chunkSize - 1, totalBytes - 1);
            long chunkLength = end - start + 1;
            
            var task = Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, url);
                    request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);
                    
                    using var response = await _httpClient.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    
                    byte[] data = await response.Content.ReadAsByteArrayAsync();
                    
                    // 寫入到正確的位置
                    using var fs = new FileStream(outputFile, FileMode.Open, FileAccess.Write, FileShare.Write);
                    fs.Seek(start, SeekOrigin.Begin);
                    await fs.WriteAsync(data, 0, data.Length);
                    
                    lock (lockObj)
                    {
                        downloaded += data.Length;
                        completedChunks++;
                        
                        double progress = (double)downloaded / totalBytes * 100;
                        double elapsed = stopwatch.Elapsed.TotalSeconds;
                        double speed = downloaded / elapsed;
                        double eta = (totalBytes - downloaded) / speed;
                        
                        Console.Write($"\r下載進度: {FormatBytes(downloaded)}/{FormatBytes(totalBytes)} ({progress:F1}%) | 區塊: {completedChunks}/{totalChunks} | 速度: {FormatBytes((long)speed)}/s | 剩餘: {eta:F0} 秒   ");
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            });
            
            downloadTasks.Add(task);
        }
        
        await Task.WhenAll(downloadTasks);
    }
    
    static async Task DownloadWithStream(string url, string outputFile, long? totalBytes, Stopwatch stopwatch)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        
        if (!totalBytes.HasValue)
        {
            totalBytes = response.Content.Headers.ContentLength;
        }
        
        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
        
        var buffer = new byte[8192];
        long totalRead = 0;
        int bytesRead;
        
        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead);
            totalRead += bytesRead;
            
            if (totalBytes.HasValue)
            {
                double progress = (double)totalRead / totalBytes.Value * 100;
                double elapsed = stopwatch.Elapsed.TotalSeconds;
                double speed = totalRead / elapsed;
                double eta = (totalBytes.Value - totalRead) / speed;
                
                Console.Write($"\r下載進度: {FormatBytes(totalRead)}/{FormatBytes(totalBytes.Value)} ({progress:F1}%) | 速度: {FormatBytes((long)speed)}/s | 剩餘: {eta:F0} 秒   ");
            }
            else
            {
                double elapsed = stopwatch.Elapsed.TotalSeconds;
                double speed = totalRead / elapsed;
                Console.Write($"\r已下載: {FormatBytes(totalRead)} | 速度: {FormatBytes((long)speed)}/s   ");
            }
        }
    }
    
    static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:F2} {sizes[order]}";
    }
    
    static async Task DownloadM3U8(string url, string outputName)
    {
        Console.WriteLine("偵測到 M3U8 連結...");
        
        // 從 URL 下載 M3U8
        Console.WriteLine("正在下載 M3U8...");
        string m3u8Content;
        try
        {
            m3u8Content = await _httpClient.GetStringAsync(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"下載 M3U8 失敗: {ex.Message}");
            return;
        }
        
        // 取得 baseUrl (去掉檔名)
        string baseUrl = url.Substring(0, url.LastIndexOf('/') + 1);
        
        Console.WriteLine("--- M3U8 內容預覽 ---");
        var previewLines = m3u8Content.Split('\n').Take(15);
        foreach (var line in previewLines)
        {
            Console.WriteLine(line);
        }
        Console.WriteLine("...");
        Console.WriteLine("--- 預覽結束 ---\n");
        
        await DownloadAndConvert(m3u8Content, baseUrl, outputName);
    }
    
    static bool CheckFFmpeg()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
    
    static async Task DownloadAndConvert(string m3u8Content, string baseUrl, string outputName)
    {
        string tempDir = Path.Combine(AppContext.BaseDirectory, "temp", $"m3u8_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        Console.WriteLine($"暫存目錄: {tempDir}");
        
        try
        {
            // 解析 M3U8
            var m3u8Info = ParseM3U8(m3u8Content, baseUrl);
            
            Console.WriteLine($"找到 {m3u8Info.Segments.Count} 個片段");
            Console.WriteLine($"使用 {MaxConcurrentDownloads} 個並行下載");
            
            // 下載並處理加密金鑰 (如果有)
            byte[]? aesKey = null;
            byte[]? aesIV = null;
            
            if (m3u8Info.KeyInfo != null)
            {
                Console.WriteLine("偵測到 AES-128 加密，正在下載金鑰...");
                aesKey = await _httpClient.GetByteArrayAsync(m3u8Info.KeyInfo.KeyUrl);
                aesIV = m3u8Info.KeyInfo.IV;
                Console.WriteLine($"金鑰下載完成 ({aesKey.Length} bytes)");
            }
            
            // 下載初始化片段 (如果有)
            string? initFile = null;
            if (!string.IsNullOrEmpty(m3u8Info.InitUrl))
            {
                Console.WriteLine("正在下載初始化片段...");
                initFile = Path.Combine(tempDir, "init.mp4");
                await DownloadFileAsync(m3u8Info.InitUrl, initFile);
            }
            
            // 並行下載所有片段
            var segmentFiles = new string[m3u8Info.Segments.Count];
            int totalSegments = m3u8Info.Segments.Count;
            int completedCount = 0;
            var stopwatch = Stopwatch.StartNew();
            
            using var semaphore = new SemaphoreSlim(MaxConcurrentDownloads);
            var downloadTasks = new List<Task>();
            
            for (int i = 0; i < totalSegments; i++)
            {
                int index = i; // 捕獲變數
                string segmentFile = Path.Combine(tempDir, $"segment_{index:D5}.ts");
                segmentFiles[index] = segmentFile;
                
                var task = Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        byte[] data = await _httpClient.GetByteArrayAsync(m3u8Info.Segments[index]);
                        
                        // 如果有加密，解密片段
                        if (aesKey != null)
                        {
                            byte[] iv = aesIV ?? GetDefaultIV(index);
                            data = DecryptAes128(data, aesKey, iv);
                        }
                        
                        await File.WriteAllBytesAsync(segmentFile, data);
                        
                        int completed = Interlocked.Increment(ref completedCount);
                        double progress = (double)completed / totalSegments * 100;
                        double elapsed = stopwatch.Elapsed.TotalSeconds;
                        double speed = completed / elapsed;
                        double eta = (totalSegments - completed) / speed;
                        
                        Console.Write($"\r下載進度: {completed}/{totalSegments} ({progress:F1}%) | 速度: {speed:F1} 片段/秒 | 剩餘: {eta:F0} 秒   ");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
                
                downloadTasks.Add(task);
            }
            
            await Task.WhenAll(downloadTasks);
            stopwatch.Stop();
            
            Console.WriteLine();
            Console.WriteLine($"下載完成! 耗時: {stopwatch.Elapsed.TotalSeconds:F1} 秒");
            
            // 使用 FFmpeg 合併
            Console.WriteLine("正在合併片段並轉換為 MP4...");
            string outputFile = Path.Combine(Directory.GetCurrentDirectory(), $"{outputName}.mp4");
            
            await MergeWithFFmpeg(tempDir, initFile, segmentFiles.ToList(), outputFile);
            
            Console.WriteLine($"✅ 下載完成: {outputFile}");
        }
        finally
        {
            // 清理暫存檔案
            Console.WriteLine("正在清理暫存檔案...");
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch { }
        }
    }
    
    static byte[] GetDefaultIV(int sequenceNumber)
    {
        byte[] iv = new byte[16];
        byte[] seqBytes = BitConverter.GetBytes(sequenceNumber);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(seqBytes);
        Array.Copy(seqBytes, 0, iv, 12, 4);
        return iv;
    }
    
    static byte[] DecryptAes128(byte[] encryptedData, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        
        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
    }
    
    static M3U8Info ParseM3U8(string content, string baseUrl)
    {
        var info = new M3U8Info();
        
        // 解析 EXT-X-MAP (初始化片段)
        var mapMatch = Regex.Match(content, @"#EXT-X-MAP:URI=""([^""]+)""");
        if (mapMatch.Success)
        {
            info.InitUrl = ResolveUrl(mapMatch.Groups[1].Value, baseUrl);
        }
        
        // 解析 EXT-X-KEY (加密資訊)
        var keyMatch = Regex.Match(content, @"#EXT-X-KEY:METHOD=AES-128,URI=""([^""]+)""(?:,IV=0x([a-fA-F0-9]+))?");
        if (keyMatch.Success)
        {
            string keyUrl = ResolveUrl(keyMatch.Groups[1].Value, baseUrl);
            byte[]? iv = null;
            
            if (keyMatch.Groups[2].Success)
            {
                iv = HexStringToBytes(keyMatch.Groups[2].Value);
            }
            
            info.KeyInfo = new KeyInfo { KeyUrl = keyUrl, IV = iv };
        }
        
        // 解析片段 URL
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            string trimmed = line.Trim();
            if (!trimmed.StartsWith("#") && !string.IsNullOrEmpty(trimmed))
            {
                string url = ResolveUrl(trimmed, baseUrl);
                info.Segments.Add(url);
            }
        }
        
        return info;
    }
    
    static string ResolveUrl(string url, string baseUrl)
    {
        if (url.StartsWith("http://") || url.StartsWith("https://"))
        {
            return url;
        }
        return baseUrl + url;
    }
    
    static byte[] HexStringToBytes(string hex)
    {
        int length = hex.Length;
        byte[] bytes = new byte[length / 2];
        for (int i = 0; i < length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }
        return bytes;
    }
    
    static async Task DownloadFileAsync(string url, string filePath)
    {
        using var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        using var fs = new FileStream(filePath, FileMode.Create);
        await response.Content.CopyToAsync(fs);
    }
    
    static async Task MergeWithFFmpeg(string tempDir, string? initFile, List<string> segmentFiles, string outputFile)
    {
        // 建立 concat 檔案列表
        string concatFile = Path.Combine(tempDir, "concat.txt");
        var concatContent = new List<string>();
        
        if (initFile != null && File.Exists(initFile))
        {
            concatContent.Add($"file '{initFile.Replace("\\", "/").Replace("'", "'\\''")}'");
        }
        
        foreach (var seg in segmentFiles)
        {
            concatContent.Add($"file '{seg.Replace("\\", "/").Replace("'", "'\\''")}'");
        }
        
        await File.WriteAllLinesAsync(concatFile, concatContent);
        
        // 使用 FFmpeg 合併
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-y -f concat -safe 0 -i \"{concatFile}\" -c copy \"{outputFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        
        process.Start();
        
        // 讀取錯誤輸出 (FFmpeg 進度會輸出到 stderr)
        string stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        if (process.ExitCode != 0)
        {
            Console.WriteLine($"FFmpeg 錯誤: {stderr}");
            throw new Exception("FFmpeg 合併失敗");
        }
    }
}

class M3U8Info
{
    public string? InitUrl { get; set; }
    public KeyInfo? KeyInfo { get; set; }
    public List<string> Segments { get; set; } = new List<string>();
}

class KeyInfo
{
    public required string KeyUrl { get; set; }
    public byte[]? IV { get; set; }
}
