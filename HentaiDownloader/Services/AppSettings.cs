using Microsoft.Extensions.Configuration;

namespace HentaiDownloader.Services;

/// <summary>
/// 應用程式設定服務
/// </summary>
public static class AppSettings
{
    private static IConfiguration? _configuration;

    /// <summary>
    /// 下載輸出路徑
    /// </summary>
    public static string OutputPath { get; private set; } = Path.Combine(Directory.GetCurrentDirectory(), "影片");

    /// <summary>
    /// 初始化設定
    /// </summary>
    public static void Initialize()
    {
        try
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            _configuration = builder.Build();

            // 讀取下載路徑設定
            var outputPath = _configuration["DownloadSettings:OutputPath"];
            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                // 確保目錄存在
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                    Console.WriteLine($"已建立下載目錄: {outputPath}");
                }
                OutputPath = outputPath;
            }

            Console.WriteLine($"下載路徑: {OutputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"讀取設定檔失敗: {ex.Message}");
            Console.WriteLine($"使用預設路徑: {OutputPath}");
        }
    }
}
