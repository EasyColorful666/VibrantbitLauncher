using CommunityToolkit.Mvvm.Messaging;
using System.Windows;
using System.Windows.Controls;
using VibrantbitLauncher.Helpers;
using VibrantbitLauncher.ViewModels.Pages;
using VibrantbitLauncher.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace VibrantbitLauncher.Views.Pages
{
    /// <summary>
    /// 启动页：左侧选版本 + 启动，右侧官方新闻（纯文字）。
    /// 版本列表与启动逻辑复用 RunPageViewModel。
    /// </summary>
    public partial class LaunchPage : Page
    {
        private readonly RunPageViewModel? _launchViewModel;
        private readonly SnackbarService _snackbar = new();

        public LaunchPage()
        {
            _launchViewModel = App.Services.GetService(typeof(RunPageViewModel)) as RunPageViewModel;
            DataContext = _launchViewModel;
            InitializeComponent();

            _snackbar.SetSnackbarPresenter(SnackbarPresenter);

            // 新闻区用独立的数据上下文，避免和启动逻辑混在一起
            var newsViewModel = App.Services.GetService(typeof(DashboardPageViewModel)) as DashboardPageViewModel;
            NewsHost.DataContext = newsViewModel;

            Loaded += async (_, _) =>
            {
                // Refresh 内部有并发保护，反复导航不会累积任务
                _launchViewModel?.LoadCommand.Execute(SnackbarPresenter);

                if (newsViewModel is { IsLoaded: false })
                    await newsViewModel.LoadNewsAsync();
            };
        }

        private void OnLaunchClick(object sender, RoutedEventArgs e)
        {
            if (listBox.SelectedItem is LocalVersion version)
                version.RunCommand?.Execute(version.Version);
            else
                ShowHint("请先选择一个版本");
        }

        private void OnVersionSettingsClick(object sender, RoutedEventArgs e)
        {
            if (listBox.SelectedItem is LocalVersion version)
            {
                WeakReferenceMessenger.Default.Send<string, string>(version.Version, "McVersionForManage");
                WeakReferenceMessenger.Default.Send<Type, string>(typeof(VersionManagePage), "NavigateTo");
            }
            else
            {
                ShowHint("请先选择一个版本");
            }
        }

        private void ShowHint(string message)
            => _snackbar.Show("提示", message, ControlAppearance.Info, null, _snackbar.DefaultTimeOut);

        /// <summary>点击新闻卡片 → 在主窗口内打开详情页。</summary>
        private void OnNewsItemClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is not FrameworkElement { DataContext: NewsItem news })
            {
                Serilog.Log.Warning("新闻卡片点击：拿不到 NewsItem（sender={Sender}）", sender?.GetType().Name);
                return;
            }

            bool navigated = false;
            var navService = App.Services.GetService(typeof(Wpf.Ui.INavigationService)) as Wpf.Ui.INavigationService;
            if (navService != null)
                navigated = navService.Navigate(typeof(NewsDetailPage));

            if (!navigated)
            {
                var mainWindow = Application.Current.Windows.OfType<Views.Windows.MainWindow>().FirstOrDefault();
                if (mainWindow != null)
                    navigated = mainWindow.Navigate(typeof(NewsDetailPage));
            }

            Serilog.Log.Information("新闻卡片点击：导航到详情页 = {Ok}", navigated);
            if (!navigated)
                return;

            (App.Services.GetService(typeof(NewsDetailPage)) as NewsDetailPage)?.ShowNews(news);
        }
    }
}
