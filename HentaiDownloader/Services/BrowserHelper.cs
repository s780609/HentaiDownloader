using PuppeteerSharp;

namespace HentaiDownloader.Services;

/// <summary>
/// 瀏覽器相關的共用輔助方法（修正單一檔案發布時找不到 Chrome 的問題）
/// </summary>
public static class BrowserHelper
{
    /// <summary>
    /// 取得 PuppeteerSharp 瀏覽器快取目錄。
    /// 單一檔案發布時 Assembly.Location 為空，BrowserFetcher 預設路徑會失效，
    /// 因此使用 AppContext.BaseDirectory（在 SingleFile 模式下仍正確指向 exe 所在目錄）。
    /// </summary>
    public static string GetBrowserCachePath()
    {
        return Path.Combine(AppContext.BaseDirectory, ".puppeteer");
    }

    /// <summary>
    /// 建立已指定快取路徑的 BrowserFetcher
    /// </summary>
    public static BrowserFetcher CreateBrowserFetcher()
    {
        return new BrowserFetcher(new BrowserFetcherOptions
        {
            Path = GetBrowserCachePath()
        });
    }
}
