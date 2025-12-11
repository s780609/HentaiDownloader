using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using HentaiDownloader.Models;
using FFMpegCore;

namespace HentaiDownloader.Services;

/// <summary>
/// 影片下載相關的服務
/// </summary>
public static class DownloadService
{
    private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromHours(2) };
    private const int MaxConcurrentDownloads = 10; // 同時下載數量

    /// <summary>
    /// 下載影片（自動判斷類型）
    /// </summary>
    public static async Task DownloadVideoAsync(string videoUrl, string outputName)
    {
        if (videoUrl.Contains(".m3u8"))
        {
            // M3U8 需要 FFmpeg
            if (!FFmpegService.CheckFFmpeg())
            {
                Console.WriteLine("錯誤: 找不到 FFmpeg，請先安裝 FFmpeg 並加入 PATH 環境變數");
                Console.WriteLine("下載位址: https://ffmpeg.org/download.html");
                return;
            }

            await DownloadM3U8Async(videoUrl, outputName);
        }
        else
        {
            // 直接下載 MP4/TS 等檔案
            await DownloadDirectFileAsync(videoUrl, outputName);
        }
    }

    /// <summary>
    /// 直接下載檔案 (MP4/TS 等)
    /// </summary>
    public static async Task DownloadDirectFileAsync(string url, string outputName)
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

        // 使用指定的 影片/ 資料夾
        string videoDir = Path.Combine(Directory.GetCurrentDirectory(), "影片");
        Directory.CreateDirectory(videoDir); // 確保目錄存在
        string outputFile = Path.Combine(videoDir, $"{outputName}{extension}");

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
                await DownloadWithRangeRequestsAsync(url, outputFile, totalBytes.Value, stopwatch);
            }
            else
            {
                Console.WriteLine("使用串流下載模式...");
                await DownloadWithStreamAsync(url, outputFile, totalBytes, stopwatch);
            }

            stopwatch.Stop();
            Console.WriteLine();
            Console.WriteLine($"✅ 下載完成: {outputFile}");
            Console.WriteLine($"耗時: {stopwatch.Elapsed.TotalSeconds:F1} 秒");

            // 如果下載的不是 MP4 格式，轉換成 MP4
            if (!extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                await ConvertToMP4Async(outputFile, outputName);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n下載失敗: {ex.Message}");
        }
    }

    /// <summary>
    /// 將影片轉換為 MP4 格式
    /// </summary>
    private static async Task ConvertToMP4Async(string inputFile, string outputName)
    {
        try
        {
            if (!FFmpegService.CheckFFmpeg())
            {
                Console.WriteLine("⚠️  FFmpeg 未安裝，無法轉換為 MP4 格式");
                return;
            }

            Console.WriteLine("\n正在轉換為 MP4 格式...");
            
            string videoDir = Path.Combine(Directory.GetCurrentDirectory(), "影片");
            string outputFile = Path.Combine(videoDir, $"{outputName}.mp4");

            // 設定 FFmpeg 路徑
            var ffmpegPath = FFmpegService.GetFFmpegExecutablePath();
            if (!string.IsNullOrEmpty(ffmpegPath))
            {
                var binaryFolder = Path.GetDirectoryName(ffmpegPath);
                if (!string.IsNullOrEmpty(binaryFolder))
                {
                    GlobalFFOptions.Configure(new FFOptions { BinaryFolder = binaryFolder });
                }
            }

            // 使用 FFMpegCore 轉換
            var conversion = await FFMpegArguments
                .FromFileInput(inputFile)
                .OutputToFile(outputFile, true, options => options
                    .WithVideoCodec("libx264")
                    .WithAudioCodec("aac")
                    .WithFastStart())
                .ProcessAsynchronously();

            if (conversion)
            {
                Console.WriteLine($"✅ 轉換完成: {outputFile}");
                
                // 詢問是否刪除原始檔案
                Console.Write("是否刪除原始檔案? (Y/N): ");
                string? deleteChoice = Console.ReadLine();
                if (deleteChoice?.Trim().ToUpper() == "Y")
                {
                    try
                    {
                        File.Delete(inputFile);
                        Console.WriteLine("已刪除原始檔案");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"刪除原始檔案失敗: {ex.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine("❌ 轉換失敗");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"轉換失敗: {ex.Message}");
        }
    }

    /// <summary>
    /// 下載 M3U8 影片
    /// </summary>
    public static async Task DownloadM3U8Async(string url, string outputName)
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

        await DownloadAndConvertAsync(m3u8Content, baseUrl, outputName);
    }

    private static async Task DownloadWithRangeRequestsAsync(string url, string outputFile, long totalBytes, Stopwatch stopwatch)
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

    private static async Task DownloadWithStreamAsync(string url, string outputFile, long? totalBytes, Stopwatch stopwatch)
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

    private static async Task DownloadAndConvertAsync(string m3u8Content, string baseUrl, string outputName)
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
            // 使用指定的 影片/ 資料夾
            string videoDir = Path.Combine(Directory.GetCurrentDirectory(), "影片");
            Directory.CreateDirectory(videoDir); // 確保目錄存在
            string outputFile = Path.Combine(videoDir, $"{outputName}.mp4");

            await FFmpegService.MergeWithFFmpegAsync(tempDir, initFile, segmentFiles.ToList(), outputFile);

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

    #region 輔助方法

    private static string FormatBytes(long bytes)
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

    private static byte[] GetDefaultIV(int sequenceNumber)
    {
        byte[] iv = new byte[16];
        byte[] seqBytes = BitConverter.GetBytes(sequenceNumber);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(seqBytes);
        Array.Copy(seqBytes, 0, iv, 12, 4);
        return iv;
    }

    private static byte[] DecryptAes128(byte[] encryptedData, byte[] key, byte[] iv)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(encryptedData, 0, encryptedData.Length);
    }

    private static M3U8Info ParseM3U8(string content, string baseUrl)
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

    private static string ResolveUrl(string url, string baseUrl)
    {
        if (url.StartsWith("http://") || url.StartsWith("https://"))
        {
            return url;
        }
        return baseUrl + url;
    }

    private static byte[] HexStringToBytes(string hex)
    {
        int length = hex.Length;
        byte[] bytes = new byte[length / 2];
        for (int i = 0; i < length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }
        return bytes;
    }

    private static async Task DownloadFileAsync(string url, string filePath)
    {
        using var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        using var fs = new FileStream(filePath, FileMode.Create);
        await response.Content.CopyToAsync(fs);
    }

    #endregion
}
