using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System.Globalization;
using VideoPlayer.Models;
using VideoPlayer.ViewModels;
using WinRT.Interop;

namespace VideoPlayer.Views;

/// <summary>
/// Video gallery page showing grid of videos
/// </summary>
public sealed partial class VideoGalleryPage : Page
{
    public VideoGalleryViewModel ViewModel { get; }

    public VideoGalleryPage()
    {
        this.InitializeComponent();
        ViewModel = new VideoGalleryViewModel();
        
        // Set window handle for file picker
        this.Loaded += VideoGalleryPage_Loaded;
    }

    private void VideoGalleryPage_Loaded(object sender, RoutedEventArgs e)
    {
        var window = WindowHelper.GetWindowForElement(this);
        if (window != null)
        {
            var windowHandle = WindowNative.GetWindowHandle(window);
            ViewModel.SetWindowHandle(windowHandle);
        }
    }

    private void VideoGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is VideoItem video)
        {
            // Navigate to player page
            Frame.Navigate(typeof(VideoPlayerPage), video);
        }
    }
}

/// <summary>
/// Helper class to get window from element
/// </summary>
public static class WindowHelper
{
    public static Window? GetWindowForElement(UIElement element)
    {
        if (element.XamlRoot != null)
        {
            foreach (Window window in Microsoft.UI.Xaml.Window.Current != null 
                ? new[] { Microsoft.UI.Xaml.Window.Current }
                : GetOpenWindows())
            {
                if (element.XamlRoot == window.Content?.XamlRoot)
                {
                    return window;
                }
            }
        }
        return null;
    }

    private static IEnumerable<Window> GetOpenWindows()
    {
        // In WinUI 3, we need to track windows ourselves
        // This is a simplified version - in production, implement proper window tracking
        yield break;
    }
}

/// <summary>
/// Converts string path to URI
/// </summary>
public class StringToUriConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string path && !string.IsNullOrEmpty(path))
        {
            return new Uri(path);
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts null to Visibility
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value == null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts TimeSpan to Visibility (shows if duration > 0)
/// </summary>
public class TimeSpanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TimeSpan duration)
        {
            return duration.TotalSeconds > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
