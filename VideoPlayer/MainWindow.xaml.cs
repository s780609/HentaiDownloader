using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VideoPlayer2.Pages;

namespace VideoPlayer2
{
    /// <summary>
    /// 主視窗，包含導覽功能
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 設定視窗大小
            var appWindow = this.AppWindow;
            appWindow.Resize(new Windows.Graphics.SizeInt32(1400, 900));

            // 預設導覽到影片管理頁面
            ContentFrame.Navigate(typeof(VideoManagerPage));
            NavView.SelectedItem = NavView.MenuItems[0];
        }

        /// <summary>
        /// 導覽選擇變更事件
        /// </summary>
        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItem is NavigationViewItem item)
            {
                var tag = item.Tag?.ToString();

                switch (tag)
                {
                    case "Videos":
                        ContentFrame.Navigate(typeof(VideoManagerPage));
                        break;
                    case "Photos":
                        ContentFrame.Navigate(typeof(PhotoManagerPage));
                        break;
                }
            }
        }
    }
}
