using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Media.Imaging;

namespace VideoPlayer2.Models;

/// <summary>
/// 影片項目模型
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public class VideoItem
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FileSize { get; set; } = string.Empty;
    public DateTime ModifiedDate { get; set; }
    public string ModifiedDateString => ModifiedDate.ToString("yyyy/MM/dd HH:mm");
    public BitmapImage? Thumbnail { get; set; }
    public string FolderName { get; set; } = string.Empty;
}
