namespace HentaiDownloader.Models;

/// <summary>
/// M3U8 播放清單資訊
/// </summary>
public class M3U8Info
{
    /// <summary>
    /// 初始化片段 URL
    /// </summary>
    public string? InitUrl { get; set; }

    /// <summary>
    /// 加密金鑰資訊
    /// </summary>
    public KeyInfo? KeyInfo { get; set; }

    /// <summary>
    /// 影片片段 URL 列表
    /// </summary>
    public List<string> Segments { get; set; } = new List<string>();
}

/// <summary>
/// 加密金鑰資訊
/// </summary>
public class KeyInfo
{
    /// <summary>
    /// 金鑰 URL
    /// </summary>
    public required string KeyUrl { get; set; }

    /// <summary>
    /// 初始化向量 (IV)
    /// </summary>
    public byte[]? IV { get; set; }
}
