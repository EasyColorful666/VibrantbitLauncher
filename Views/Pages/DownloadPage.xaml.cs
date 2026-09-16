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

            // 预先创建空的 CollectionView 并绑定到 ListBox，这样 UI 能尽快响应并显示占位（虚拟化生效）
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

            // 页面加载时开始增量加载数据（后台线程），不阻塞 UI
            this.Loaded += (s, e) =>
            {
                snackbarService.SetSnackbarPresenter(SnackbarPresenter);
                if (!viewModel.IsLoaded)
                {
                    _ = LoadMcVersionsAsyncIncremental();
                    viewModel.IsLoaded = true;
                }
            };
        }

        private async Task LoadMcVersionsAsyncIncremental()
        {
            // 增量加载实现：在后台线程执行可能的网络/IO 操作，并按批次将数据推回 UI，减少一次性分配带来的短时卡顿。
            viewModel.McVersions.Clear();
            const int batchSize = 50;
            try
            {
                await Task.Run(async () =>
                {
                    var entries = await VanillaInstaller.EnumerableMinecraftAsync();
                    var batch = new List<McVersion>(batchSize);
                    foreach (var entry in entries)
                    {
                        batch.Add(new McVersion
                        {
                            Version = entry.McVersion,
                            Date = entry.ReleaseTime.ToString("yyyy-MM-dd")
                        });

                        if (batch.Count >= batchSize)
                        {
                            var toAdd = batch.ToArray();
                            batch.Clear();
                            // 使用 Dispatcher 将一批数据一次性添加到 UI 集合，减少 UI 线程切换次数。
                            App.Current.Dispatcher.Invoke(() =>
                            {
                                foreach (var v in toAdd) viewModel.McVersions.Add(v);
                                // 刷新过滤视图以便立即显示新增项（如果需要）
                                versionView.Refresh();
                            });
                            // 给调度器一些时间处理 UI 操作，避免长时间占用后台线程
                            await Task.Delay(10);
                        }
                    }

                    if (batch.Count > 0)
                    {
                        var toAdd = batch.ToArray();
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (var v in toAdd) viewModel.McVersions.Add(v);
                            versionView.Refresh();
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                App.Current.Dispatcher.Invoke(() => snackbarService.Show("错误", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, snackbarService.DefaultTimeOut));
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
