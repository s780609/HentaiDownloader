using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Media.Imaging;

namespace VideoPlayer2.Models;

/// <summary>
/// 照片資料模型
/// </summary>
public class PhotoItem : INotifyPropertyChanged
{
    private BitmapImage? _thumbnail;
    private int _width;
    private int _height;

    public event PropertyChangedEventHandler? PropertyChanged;
    /// <summary>
    /// 照片檔案完整路徑
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// 照片檔案名稱
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 照片標題（不含副檔名）
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 檔案大小（格式化字串）
    /// </summary>
    public string FileSize { get; set; } = string.Empty;

    /// <summary>
    /// 檔案修改日期
    /// </summary>
    public DateTime ModifiedDate { get; set; }

    /// <summary>
    /// 格式化的修改日期
    /// </summary>
    public string ModifiedDateString => ModifiedDate.ToString("yyyy/MM/dd HH:mm");

    /// <summary>
    /// 縮圖
    /// </summary>
    public BitmapImage? Thumbnail
    {
        get => _thumbnail;
        set
        {
            if (_thumbnail != value)
            {
                _thumbnail = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 所在資料夾名稱
    /// </summary>
    public string FolderName { get; set; } = string.Empty;

    /// <summary>
    /// 圖片寬度（像素）
    /// </summary>
    public int Width
    {
        get => _width;
        set
        {
            if (_width != value)
            {
                _width = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DimensionsText));
            }
        }
    }

    /// <summary>
    /// 圖片高度（像素）
    /// </summary>
    public int Height
    {
        get => _height;
        set
        {
            if (_height != value)
            {
                _height = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DimensionsText));
            }
        }
    }

    /// <summary>
    /// 圖片尺寸顯示文字
    /// </summary>
    public string DimensionsText => Width > 0 && Height > 0 ? $"{Width}×{Height}" : string.Empty;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
