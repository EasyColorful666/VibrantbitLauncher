using MinecraftLaunch.Base.Models.Network;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using VibrantbitLauncher.Services;
using VibrantbitLauncher.ViewModels.Pages;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;

namespace VibrantbitLauncher.Views.Pages
{
    /// <summary>
    /// DownloadPage.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadPage : Page
    {
        private ICollectionView versionView;
        private SnackbarService snackbarService = new();
        private DownloadPageViewModel viewModel = App.Services.GetService(typeof(DownloadPageViewModel)) as DownloadPageViewModel;
        private DispatcherTimer debounceTimer;

        public DownloadPage()
        {
            InitializeComponent();
            this.DataContext = viewModel;

            // 预先创建空的 CollectionView 并绑定到 ListBox
            versionView = new ListCollectionView(viewModel.McVersions);
            versionView.Filter = VersionFilter;
            listBox.ItemsSource = versionView;

            // 初始化防抖定时器
            debounceTimer = new DispatcherTimer();
            debounceTimer.Interval = TimeSpan.FromMilliseconds(260);
            debounceTimer.Tick += (s, e) =>
            {
                debounceTimer.Stop();
                versionView.Refresh();
            };

            // 页面加载时开始异步加载数据，不阻塞 UI
            this.Loaded += (s, e) =>
            {
                snackbarService.SetSnackbarPresenter(SnackbarPresenter);
                if (!viewModel.IsLoaded)
                {
                    _ = LoadMcVersionsAsync();
                    viewModel.IsLoaded = true;
                }
            };
        }

        /// <summary>
        /// 异步加载 Minecraft 版本列表：后台线程获取数据，批量推回 UI，最后只刷新一次。
        /// </summary>
        private async Task LoadMcVersionsAsync()
        {
            viewModel.McVersions.Clear();
            const int batchSize = 100;

            try
            {
                // 后台线程获取所有版本数据
                var allVersions = await Task.Run(async () =>
                {
                    var entries = await MinecraftVersionCache.GetAsync();
                    return entries.Select(entry => new McVersion
                    {
                        Version = entry.McVersion,
                        Date = entry.ReleaseTime.ToString("yyyy-MM-dd")
                    }).ToList();
                });

                // 分批添加到 UI 集合，用 InvokeAsync 异步调度，不阻塞后台线程
                for (int i = 0; i < allVersions.Count; i += batchSize)
                {
                    var batch = allVersions.Skip(i).Take(batchSize).ToArray();
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        foreach (var v in batch)
                            viewModel.McVersions.Add(v);
                    }, DispatcherPriority.Background);
                }

                // 全部添加完成后只刷新一次过滤视图
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    versionView.Refresh();
                }, DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                await App.Current.Dispatcher.InvokeAsync(() =>
                    snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, snackbarService.DefaultTimeOut));
            }
        }

        private bool VersionFilter(object item)
        {
            if (item is not McVersion ver) return false;
            if (string.IsNullOrEmpty(textBox.Text)) return true;
            return ver.Version.Contains(textBox.Text, StringComparison.OrdinalIgnoreCase);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            debounceTimer.Stop();
            debounceTimer.Start();
        }
    }

    public class McVersion
    {
        public string Version { get; set; }
        public string Date { get; set; }
    }
}
