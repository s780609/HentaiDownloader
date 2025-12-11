using PuppeteerSharp;

namespace HentaiDownloader.Services;

/// <summary>
/// 從網頁提取影片 URL 的服務
/// </summary>
public static class VideoExtractorService
{
    /// <summary>
    /// 從網頁中提取影片 URL
    /// </summary>
    public static async Task<string?> ExtractVideoUrlFromPageAsync(string pageUrl)
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
}
