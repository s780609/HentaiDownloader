using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VideoPlayer.Views;

namespace VideoPlayer;

/// <summary>
/// Main window for the Video Player application
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        
        // Set window size
        this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1280, 720));
        
        // Navigate to gallery page by default
        ContentFrame.Navigate(typeof(VideoGalleryPage));
        
        // Handle navigation
        NavView.SelectionChanged += NavView_SelectionChanged;
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            string tag = item.Tag?.ToString() ?? string.Empty;
            
            switch (tag)
            {
                case "Gallery":
                    ContentFrame.Navigate(typeof(VideoGalleryPage));
                    break;
            }
        }
    }
}
