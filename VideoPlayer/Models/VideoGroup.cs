using System.Collections.ObjectModel;

namespace VideoPlayer2.Models;

/// <summary>
/// 影片群組模型 - 用於將名稱相似的影片分組顯示
/// </summary>
public class VideoGroup
{
    /// <summary>
    /// 群組顯示標題（使用第一個影片的標題）
    /// </summary>
    public string GroupTitle { get; set; } = string.Empty;

    /// <summary>
    /// 群組中的影片列表
    /// </summary>
    public ObservableCollection<VideoItem> Videos { get; set; } = new();

    /// <summary>
    /// 群組中影片數量
    /// </summary>
    public int Count => Videos.Count;

    /// <summary>
    /// 是否為群組（包含多個影片）
    /// </summary>
    public bool IsGroup => Videos.Count > 1;

    /// <summary>
    /// 代表影片（群組中的第一個影片）
    /// </summary>
    public VideoItem? RepresentativeVideo => Videos.Count > 0 ? Videos[0] : null;

    /// <summary>
    /// 群組的副標題，顯示包含的影片數量
    /// </summary>
    public string SubTitle => IsGroup ? $"包含 {Count} 個相似影片" : string.Empty;
}
