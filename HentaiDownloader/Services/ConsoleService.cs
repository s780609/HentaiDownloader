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

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    private const int STD_INPUT_HANDLE = -10;
    private const int STD_OUTPUT_HANDLE = -11;
    private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

    /// <summary>
    /// 初始化控制台環境 (UTF-8 支援)
    /// </summary>
    public static void InitializeConsole()
    {
        // 在 Windows 上啟用 UTF-8 支援
        if (OperatingSystem.IsWindows())
        {
            // 設定控制台程式碼頁為 UTF-8
            SetConsoleCP(65001);        // UTF-8 輸入
            SetConsoleOutputCP(65001);  // UTF-8 輸出

            // 嘗試啟用 Virtual Terminal Processing (支援更好的 Unicode 顯示)
            try
            {
                var handle = GetStdHandle(STD_OUTPUT_HANDLE);
                if (GetConsoleMode(handle, out uint mode))
                {
                    SetConsoleMode(handle, mode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
                }
            }
            catch
            {
                // 忽略錯誤，某些終端可能不支援
            }
        }

        // 設定 .NET 的控制台編碼
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;
    }

    /// <summary>
    /// 讀取一行輸入 (支援 Unicode)
    /// </summary>
    public static string? ReadLineUnicode()
    {
        // 使用 StreamReader 直接從標準輸入讀取，確保 UTF-8 編碼
        using var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
        return reader.ReadLine();
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
