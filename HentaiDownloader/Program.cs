using HentaiDownloader.Services;

class Program
{
    static async Task Main(string[] args)
    {
        // Step 1: 初始化控制台環境
        ConsoleService.InitializeConsole();

        // Step 2: 顯示歡迎訊息
        ConsoleService.ShowWelcomeMessage();

        // Step 3: 檢查並安裝 FFmpeg
        await FFmpegService.EnsureFFmpegAsync();

        // Step 4: 取得影片 URL
        string? videoUrl = await UserInputService.GetVideoUrlAsync();
        if (string.IsNullOrEmpty(videoUrl))
        {
            return;
        }

        // Step 5: 取得輸出檔名
        string outputName = UserInputService.GetOutputFileName();

        // Step 6: 下載影片
        await DownloadService.DownloadVideoAsync(videoUrl, outputName);
    }
}
