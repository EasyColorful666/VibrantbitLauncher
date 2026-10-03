using GalaSoft.MvvmLight.Messaging;
using System.Windows.Controls;

namespace VibrantbitLauncher.Views.Pages
{
    public partial class DownloadHubPage : Page
    {
        private readonly DownloadPage _downloadPage;
        private readonly DownloadResourcesPage _downloadResourcesPage;
        private bool _modTabLoaded;

        public DownloadHubPage(DownloadPage downloadPage, DownloadResourcesPage downloadResourcesPage)
        {
            InitializeComponent();
            _downloadPage = downloadPage;
            _downloadResourcesPage = downloadResourcesPage;

            // 默认只加载第一个 Tab（游戏版本）
            VersionFrame.Content = _downloadPage;

            MainTabControl.SelectionChanged += OnTabChanged;

            // 接收下载任务中心的 Tab 切换请求
            Messenger.Default.Register<int>(this, "DownloadHubTab", index =>
            {
                if (index >= 0 && index < MainTabControl.Items.Count)
                    MainTabControl.SelectedIndex = index;
            });
        }

        private void OnTabChanged(object sender, SelectionChangedEventArgs e)
        {
            // 懒加载：第一次切换到"模组资源"时才创建 Frame 内容
            if (MainTabControl.SelectedIndex == 1 && !_modTabLoaded)
            {
                ModFrame.Content = _downloadResourcesPage;
                _modTabLoaded = true;
            }
        }
    }
}
