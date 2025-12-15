using System;
using Microsoft.UI.Xaml.Media.Imaging;

namespace VideoPlayer2.Models;

/// <summary>
/// 影片項目模型
/// </summary>
public class VideoItem
{
    /// <summary>
    /// 影片檔案完整路徑
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 影片檔案名稱
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 影片標題（不含副檔名）
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 檔案大小（格式化後）
    /// </summary>
    public string FileSize { get; set; } = string.Empty;

    /// <summary>
    /// 檔案修改時間
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    /// <summary>
    /// 格式化的修改時間
    /// </summary>
    public string ModifiedDateString => ModifiedDate.ToString("yyyy/MM/dd HH:mm");

    /// <summary>
    /// 縮圖
    /// </summary>
    public BitmapImage? Thumbnail { get; set; }

    /// <summary>
    /// 所在資料夾名稱
    /// </summary>
    public string FolderName { get; set; } = string.Empty;
}
