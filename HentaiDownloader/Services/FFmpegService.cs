using System.Diagnostics;
using System.IO.Compression;
using FFMpegCore;
using FFMpegCore.Enums;

namespace HentaiDownloader.Services;

/// <summary>
/// FFmpeg 檢查和安裝相關的服務
/// </summary>
public static class FFmpegService
{
    /// <summary>
    /// 確保 FFmpeg 已安裝，如未安裝則詢問使用者是否自動安裝
    /// </summary>
    public static async Task EnsureFFmpegAsync()
    {
        if (CheckFFmpeg())
        {
            Console.WriteLine("✅ FFmpeg 已就緒");
            Console.WriteLine();
            return;
        }

        Console.WriteLine("⚠️  未偵測到 FFmpeg (M3U8 下載需要此工具)");
        Console.Write("是否要自動安裝 FFmpeg? (Y/N): ");
        string? installChoice = Console.ReadLine();

        if (installChoice?.Trim().ToUpper() == "Y")
        {
            bool installed = await InstallFFmpegAsync();
            if (!installed)
            {
                Console.WriteLine("FFmpeg 安裝失敗，M3U8 格式將無法下載");
                Console.WriteLine("您可以手動安裝: https://ffmpeg.org/download.html");
            }
        }
        else
        {
            Console.WriteLine("跳過 FFmpeg 安裝，M3U8 格式將無法下載");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// 檢查 FFmpeg 是否已安裝
    /// </summary>
    public static bool CheckFFmpeg()
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

    /// <summary>
    /// 安裝 FFmpeg
    /// </summary>
    public static async Task<bool> InstallFFmpegAsync()
    {
        Console.WriteLine("\n正在安裝 FFmpeg...");

        if (OperatingSystem.IsWindows())
        {
            return await InstallFFmpegWindowsAsync();
        }
        else if (OperatingSystem.IsLinux())
        {
            return await InstallFFmpegLinuxAsync();
        }
        else if (OperatingSystem.IsMacOS())
        {
            return await InstallFFmpegMacAsync();
        }
        else
        {
            Console.WriteLine("不支援的作業系統，請手動安裝 FFmpeg");
            return false;
        }
    }

    private static async Task<bool> InstallFFmpegWindowsAsync()
    {
        // 先嘗試使用 winget
        Console.WriteLine("嘗試使用 winget 安裝...");
        try
        {
            var wingetProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = "install Gyan.FFmpeg --accept-package-agreements --accept-source-agreements",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            wingetProcess.Start();

            // 顯示輸出
            var outputTask = Task.Run(async () =>
            {
                string? line;
                while ((line = await wingetProcess.StandardOutput.ReadLineAsync()) != null)
                {
                    Console.WriteLine(line);
                }
            });

            await wingetProcess.WaitForExitAsync();
            await outputTask;

            if (wingetProcess.ExitCode == 0)
            {
                Console.WriteLine("✅ FFmpeg 安裝成功！");
                Console.WriteLine("請重新開啟終端機以使 PATH 生效，或重新啟動程式。");
                return true;
            }
        }
        catch
        {
            Console.WriteLine("winget 不可用，嘗試其他方式...");
        }

        // 嘗試使用 choco
        Console.WriteLine("嘗試使用 Chocolatey 安裝...");
        try
        {
            var chocoProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "choco",
                    Arguments = "install ffmpeg -y",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            chocoProcess.Start();

            var outputTask = Task.Run(async () =>
            {
                string? line;
                while ((line = await chocoProcess.StandardOutput.ReadLineAsync()) != null)
                {
                    Console.WriteLine(line);
                }
            });

            await chocoProcess.WaitForExitAsync();
            await outputTask;

            if (chocoProcess.ExitCode == 0)
            {
                Console.WriteLine("✅ FFmpeg 安裝成功！");
                return true;
            }
        }
        catch
        {
            Console.WriteLine("Chocolatey 不可用...");
        }

        // 手動下載安裝
        Console.WriteLine("嘗試手動下載 FFmpeg...");
        return await DownloadFFmpegManuallyAsync();
    }

    private static async Task<bool> DownloadFFmpegManuallyAsync()
    {
        try
        {
            string ffmpegDir = Path.Combine(AppContext.BaseDirectory, "ffmpeg");
            string ffmpegExe = Path.Combine(ffmpegDir, "ffmpeg.exe");

            // 如果已經下載過，直接返回
            if (File.Exists(ffmpegExe))
            {
                Console.WriteLine("FFmpeg 已存在於本地目錄");
                AddToPath(ffmpegDir);
                return true;
            }

            Directory.CreateDirectory(ffmpegDir);

            // 下載 FFmpeg (使用 BtbN 的預編譯版本)
            string downloadUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";
            string zipPath = Path.Combine(ffmpegDir, "ffmpeg.zip");

            Console.WriteLine($"正在下載 FFmpeg...");
            Console.WriteLine($"來源: {downloadUrl}");

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(10);

            using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? 0;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[8192];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                totalRead += bytesRead;

                if (totalBytes > 0)
                {
                    double progress = (double)totalRead / totalBytes * 100;
                    Console.Write($"\r下載進度: {FormatBytes(totalRead)}/{FormatBytes(totalBytes)} ({progress:F1}%)   ");
                }
            }

            Console.WriteLine("\n下載完成，正在解壓縮...");
            fileStream.Close();

            // 解壓縮
            ZipFile.ExtractToDirectory(zipPath, ffmpegDir, true);

            // 找到 ffmpeg.exe 並移到正確位置
            var ffmpegFiles = Directory.GetFiles(ffmpegDir, "ffmpeg.exe", SearchOption.AllDirectories);
            if (ffmpegFiles.Length > 0)
            {
                string sourcePath = ffmpegFiles[0];
                string sourceDir = Path.GetDirectoryName(sourcePath)!;

                // 複製所有執行檔到 ffmpeg 目錄
                foreach (var file in Directory.GetFiles(sourceDir, "*.exe"))
                {
                    string destFile = Path.Combine(ffmpegDir, Path.GetFileName(file));
                    if (file != destFile)
                    {
                        File.Copy(file, destFile, true);
                    }
                }

                // 清理子資料夾
                foreach (var dir in Directory.GetDirectories(ffmpegDir))
                {
                    try { Directory.Delete(dir, true); } catch { }
                }
            }

            // 刪除 zip 檔案
            try { File.Delete(zipPath); } catch { }

            // 加入 PATH
            AddToPath(ffmpegDir);

            Console.WriteLine("✅ FFmpeg 安裝成功！");
            Console.WriteLine($"安裝位置: {ffmpegDir}");

            return File.Exists(ffmpegExe);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"下載 FFmpeg 失敗: {ex.Message}");
            return false;
        }
    }

    private static void AddToPath(string directory)
    {
        try
        {
            // 取得當前 PATH
            string? currentPath = Environment.GetEnvironmentVariable("PATH");

            if (currentPath != null && !currentPath.Contains(directory))
            {
                // 加入到當前程序的 PATH
                Environment.SetEnvironmentVariable("PATH", $"{directory};{currentPath}");
                Console.WriteLine($"已將 {directory} 加入 PATH");

                // 嘗試永久加入使用者 PATH (Windows)
                if (OperatingSystem.IsWindows())
                {
                    try
                    {
                        string? userPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
                        if (userPath != null && !userPath.Contains(directory))
                        {
                            Environment.SetEnvironmentVariable("PATH", $"{userPath};{directory}", EnvironmentVariableTarget.User);
                            Console.WriteLine("已永久加入使用者 PATH 環境變數");
                        }
                    }
                    catch
                    {
                        Console.WriteLine("無法永久修改 PATH，僅在本次執行有效");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加入 PATH 失敗: {ex.Message}");
        }
    }

    private static async Task<bool> InstallFFmpegLinuxAsync()
    {
        Console.WriteLine("請使用以下指令安裝 FFmpeg:");
        Console.WriteLine("  Ubuntu/Debian: sudo apt install ffmpeg");
        Console.WriteLine("  CentOS/RHEL:   sudo yum install ffmpeg");
        Console.WriteLine("  Arch Linux:    sudo pacman -S ffmpeg");

        Console.Write("\n是否嘗試使用 apt 安裝? (Y/N): ");
        string? choice = Console.ReadLine();

        if (choice?.Trim().ToUpper() == "Y")
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "sudo",
                        Arguments = "apt install -y ffmpeg",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
                };
                process.Start();
                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"安裝失敗: {ex.Message}");
            }
        }

        return false;
    }

    private static async Task<bool> InstallFFmpegMacAsync()
    {
        Console.WriteLine("請使用 Homebrew 安裝 FFmpeg:");
        Console.WriteLine("  brew install ffmpeg");

        Console.Write("\n是否嘗試使用 brew 安裝? (Y/N): ");
        string? choice = Console.ReadLine();

        if (choice?.Trim().ToUpper() == "Y")
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "brew",
                        Arguments = "install ffmpeg",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
                };
                process.Start();
                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"安裝失敗: {ex.Message}");
            }
        }

        return false;
    }

    /// <summary>
    /// 使用 FFmpeg 合併影片片段
    /// </summary>
    public static async Task MergeWithFFmpegAsync(string tempDir, string? initFile, List<string> segmentFiles, string outputFile)
    {
        try
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

            // 使用 FFMpegCore 合併影片
            // FFMpegCore 需要知道 FFmpeg 可執行檔的位置
            // 首先嘗試使用系統 PATH 中的 FFmpeg
            if (CheckFFmpeg())
            {
                // 如果系統已安裝 FFmpeg，設定全域 FFmpeg 路徑
                try
                {
                    var ffmpegPath = GetFFmpegPath();
                    if (!string.IsNullOrEmpty(ffmpegPath))
                    {
                        var binaryFolder = Path.GetDirectoryName(ffmpegPath);
                        if (!string.IsNullOrEmpty(binaryFolder))
                        {
                            GlobalFFOptions.Configure(new FFOptions { BinaryFolder = binaryFolder });
                        }
                    }
                }
                catch { }
            }

            // 使用 FFMpegCore 的 FFMpegArguments 來執行 concat
            await FFMpegArguments
                .FromConcatInput(segmentFiles, options => options
                    .WithCustomArgument("-safe 0"))
                .OutputToFile(outputFile, true, options => options
                    .CopyChannel())
                .ProcessAsynchronously();
        }
        catch (Exception ex)
        {
            // 如果 FFMpegCore 失敗，回退到直接呼叫 FFmpeg
            Console.WriteLine($"FFMpegCore 合併失敗，嘗試直接呼叫 FFmpeg: {ex.Message}");
            await MergeWithFFmpegDirectAsync(tempDir, initFile, segmentFiles, outputFile);
        }
    }

    /// <summary>
    /// 直接呼叫 FFmpeg 合併影片片段（備用方法）
    /// </summary>
    private static async Task MergeWithFFmpegDirectAsync(string tempDir, string? initFile, List<string> segmentFiles, string outputFile)
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

    /// <summary>
    /// 取得 FFmpeg 可執行檔路徑（公開方法）
    /// </summary>
    public static string? GetFFmpegExecutablePath()
    {
        return GetFFmpegPath();
    }

    /// <summary>
    /// 取得 FFmpeg 可執行檔路徑
    /// </summary>
    private static string? GetFFmpegPath()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = OperatingSystem.IsWindows() ? "where" : "which",
                    Arguments = "ffmpeg",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            
            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                return output.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            }
        }
        catch { }
        return null;
    }

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
}
