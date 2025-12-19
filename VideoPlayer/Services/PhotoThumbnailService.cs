using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using VideoPlayer2.Models;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace VideoPlayer2.Services;

/// <summary>
/// 照片縮圖服務，負責載入照片縮圖和取得圖片尺寸
/// </summary>
public class PhotoThumbnailService
{
    /// <summary>
    /// 載入照片縮圖
    /// </summary>
    public async Task LoadThumbnailAsync(PhotoItem photo)
    {
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(photo.FilePath);
            
            // 取得縮圖
            var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.PicturesView, 300);

            if (thumbnail != null)
            {
                var bitmapImage = new BitmapImage();
                await bitmapImage.SetSourceAsync(thumbnail);
                photo.Thumbnail = bitmapImage;
            }

            // 取得圖片尺寸
            var imageProperties = await file.Properties.GetImagePropertiesAsync();
            photo.Width = (int)imageProperties.Width;
            photo.Height = (int)imageProperties.Height;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"載入縮圖失敗: {photo.FileName} - {ex.Message}");
        }
    }

    /// <summary>
    /// 載入完整圖片用於預覽
    /// </summary>
    public async Task<BitmapImage?> LoadFullImageAsync(string filePath)
    {
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(filePath);
            var stream = await file.OpenReadAsync();

            var bitmapImage = new BitmapImage();
            await bitmapImage.SetSourceAsync(stream);

            return bitmapImage;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"載入完整圖片失敗: {filePath} - {ex.Message}");
            return null;
        }
    }
}
