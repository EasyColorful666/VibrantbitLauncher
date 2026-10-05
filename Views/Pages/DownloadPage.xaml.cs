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
using System.Windows.Media.Animation;
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
                UpdateCountText();
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
                    UpdateCountText();

                    // 打开一小段"入场动画窗口"：接下来这批新建的列表项会依次淡入。
                    // 窗口过期后（用户滚动才创建的容器）直接显示，避免滚动时条目"迟到"。
                    _entranceUntilUtc = DateTime.UtcNow.AddMilliseconds(1200);
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

        /// <summary>
        /// 刷新搜索框右侧的计数文案。
        /// 无搜索词时显示「共 N 个版本」；有搜索词时显示「匹配 M / 共 N 个版本」，
        /// 让用户一眼看出筛掉了多少。
        /// </summary>
        private void UpdateCountText()
        {
            if (countText == null)
                return;

            var total = viewModel?.McVersions.Count ?? 0;
            var matched = versionView?.Cast<object>().Count() ?? total;

            countText.Text = string.IsNullOrEmpty(textBox.Text)
                ? $"共 {total} 个版本"
                : $"匹配 {matched} / 共 {total} 个版本";

            emptyText.Visibility = matched == 0 && total > 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        /// <summary>
        /// 整行卡片可点击：点一下直接进入安装页并带上该版本号。
        /// 之前这里是一个独立的「下载」按钮，现在按钮已去掉，改由整张卡片承担点击。
        /// </summary>
        private void OnVersionCardClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: McVersion version }
                && viewModel?.DownloadCommand.CanExecute(version.Version) == true)
            {
                viewModel.DownloadCommand.Execute(version.Version);
            }
        }

        /// <summary>入场动画窗口的截止时间；只有在这个时间点之前新建的列表项才播淡入。</summary>
        private DateTime _entranceUntilUtc = DateTime.MinValue;

        /// <summary>
        /// 列表项进入视觉树时依次淡入：按索引错开（最多错开 9 项 ≈ 270ms），
        /// 长列表刷出来时不会"啪"地整屏出现。
        ///
        /// 动画用 HoldEnd，结束后自然停在终态（Opacity=1 / Y=0）；基准值先写成起始态，
        /// 这样 BeginTime 那段延迟里条目是"尚未出现"，而不是先亮一下再淡入。
        /// 容器被虚拟化复用时不会再触发 Loaded，所以同一项不会重播。
        /// </summary>
        private void VersionItem_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is not ListBoxItem item)
                return;

            // 不在入场窗口内（例如用户滚动时才创建的容器）：直接显示，并清掉可能残留的动画
            if (DateTime.UtcNow > _entranceUntilUtc)
            {
                item.BeginAnimation(OpacityProperty, null);
                item.Opacity = 1;
                return;
            }

            var index = listBox.ItemContainerGenerator.IndexFromContainer(item);
            var delay = TimeSpan.FromMilliseconds(index <= 0 ? 0 : Math.Min(index, 9) * 30);

            if (item.RenderTransform is not TranslateTransform transform)
            {
                transform = new TranslateTransform();
                item.RenderTransform = transform;
            }

            item.Opacity = 0;
            transform.Y = 14;

            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(240))
            {
                BeginTime = delay,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            var slide = new DoubleAnimation(14, 0, TimeSpan.FromMilliseconds(300))
            {
                BeginTime = delay,
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            item.BeginAnimation(OpacityProperty, fade);
            transform.BeginAnimation(TranslateTransform.YProperty, slide);
        }
    }

    public class McVersion
    {
        public string Version { get; set; }
        public string Date { get; set; }
    }
}
