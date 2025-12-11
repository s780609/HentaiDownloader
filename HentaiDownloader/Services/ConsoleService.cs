using System.Runtime.InteropServices;
using System.Text;

namespace HentaiDownloader.Services;

/// <summary>
/// 處理控制台初始化和訊息顯示的服務
/// </summary>
public static class ConsoleService
{
    // Windows Console UTF-8 支援
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleCP(uint wCodePageID);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleOutputCP(uint wCodePageID);

    /// <summary>
    /// 初始化控制台環境 (UTF-8 支援)
    /// </summary>
    public static void InitializeConsole()
    {
        // 在 Windows 上啟用 UTF-8 支援 (必須在任何 Console 操作之前)
        if (OperatingSystem.IsWindows())
        {
            SetConsoleCP(65001);        // UTF-8 輸入
            SetConsoleOutputCP(65001);  // UTF-8 輸出
        }

        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;
    }

    /// <summary>
    /// 顯示歡迎訊息
    /// </summary>
    public static void ShowWelcomeMessage()
    {
        Console.WriteLine("=== 影片下載器 / ビデオダウンローダー ===");
        Console.WriteLine("支援 / サポート: M3U8, MP4, TS 等格式");
        Console.WriteLine("支援從網頁自動提取影片連結");
        Console.WriteLine();
    }
}
