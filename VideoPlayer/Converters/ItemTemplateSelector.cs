using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VideoPlayer2.Models;

namespace VideoPlayer2.Converters;

/// <summary>
/// 根據項目類型選擇不同的 DataTemplate (資料夾 vs 影片)
/// </summary>
public class ItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? FolderTemplate { get; set; }
    public DataTemplate? VideoTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        return item switch
        {
            FolderItem when FolderTemplate != null => FolderTemplate,
            VideoItem when VideoTemplate != null => VideoTemplate,
            _ => base.SelectTemplateCore(item)
        };
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            FolderItem when FolderTemplate != null => FolderTemplate,
            VideoItem when VideoTemplate != null => VideoTemplate,
            _ => base.SelectTemplateCore(item, container)
        };
    }
}
