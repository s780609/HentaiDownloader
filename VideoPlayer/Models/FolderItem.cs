using System.Diagnostics.CodeAnalysis;

namespace VideoPlayer2.Models;

/// <summary>
/// 資料夾項目模型 - 用於資料夾導航顯示
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
public class FolderItem
{
    public string FolderPath { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ItemCountText { get; set; } = string.Empty;
    public int SubFolderCount { get; set; }
    public int VideoCount { get; set; }
}
