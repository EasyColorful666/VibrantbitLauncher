using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using VibrantbitLauncher.Helpers;
using VibrantbitLauncher.Models;
using VibrantbitLauncher.Services;

namespace VibrantbitLauncher.Views.Pages
{
    /// <summary>
    /// 下载中心：左侧分类单选栏 + 右侧内容区。
    ///
    /// 每个分类独占一个 Frame + 页面实例，切换只改 Visibility ——
    /// 避免 Frame.Content 反复赋值导致整棵视觉树被卸载重建（切换卡顿的根源）。
    /// </summary>
    public partial class DownloadHubPage : Page
    {
        /// <summary>
        /// 分类序号 → Modrinth 项目类型；0 是原版游戏，不走 Modrinth。
        /// 顺序必须与 XAML 里左侧 ListBox 的 Tag 一一对应（1=模组、2=整合包、3=数据包、4=资源包、5=光影）。
        /// </summary>
        private static readonly string[] ResourceTypes =
        {
            ModrinthSearchService.TypeMod,
            ModrinthSearchService.TypeModpack,
            ModrinthSearchService.TypeDatapack,
            ModrinthSearchService.TypeResourcePack,
            ModrinthSearchService.TypeShader
        };

        private readonly Dictionary<int, Frame> _resourceFrames = new();
        private DownloadPage? _downloadPage;

        public DownloadHubPage()
        {
            InitializeComponent();

            // InitializeComponent 期间 XAML 的 IsSelected="True" 会触发 SelectionChanged，
            // 那时容器还没建好，这里按当前选中项补一次。
            ApplyCurrentCategory();

            // 接收下载任务中心的分类切换请求（0 = 原版游戏，1 = Mod）
            WeakReferenceMessenger.Default.Register<DownloadHubTabMessage>(this, (_, message) => SelectCategory(message.Index));
        }

        private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => ApplyCurrentCategory();

        /// <summary>按左侧当前选中项切换右侧内容；分组标题（Tag 非数字）会被忽略。</summary>
        private void ApplyCurrentCategory()
        {
            if (VersionFrame == null)
                return;

            if (CategoryList.SelectedItem is not ListBoxItem item
                || item.Tag is not string tag
                || !int.TryParse(tag, out var index))
            {
                return;
            }

            var watch = Stopwatch.StartNew();

            if (index <= 0)
                ShowVersionPage();
            else
                ShowResourcePage(index);

            Serilog.Log.Information("[Hub] 切换分类 {Index}，耗时 {Ms}ms（首次含页面构造）", index, watch.ElapsedMilliseconds);
        }

        private void ShowVersionPage()
        {
            _downloadPage ??= App.Services.GetService(typeof(DownloadPage)) as DownloadPage;
            if (_downloadPage != null && VersionFrame.Content == null)
                VersionFrame.Content = _downloadPage;

            VersionFrame.Visibility = Visibility.Visible;
            PageTransitionHelper.PlayEnterTransition(VersionFrame);

            foreach (var frame in _resourceFrames.Values)
                frame.Visibility = Visibility.Collapsed;
        }

        private void ShowResourcePage(int index)
        {
            if (!_resourceFrames.TryGetValue(index, out var frame))
            {
                var projectType = index - 1 < ResourceTypes.Length ? ResourceTypes[index - 1] : ResourceTypes[0];

                var page = new DownloadResourcesPage();
                page.SetProjectType(projectType);

                frame = new Frame
                {
                    Content = page,
                    Visibility = Visibility.Collapsed,
                    NavigationUIVisibility = NavigationUIVisibility.Hidden,
                    JournalOwnership = JournalOwnership.OwnsJournal
                };

                HostGrid.Children.Add(frame);
                _resourceFrames[index] = frame;
            }

            VersionFrame.Visibility = Visibility.Collapsed;

            foreach (var pair in _resourceFrames)
                pair.Value.Visibility = pair.Key == index ? Visibility.Visible : Visibility.Collapsed;

            if (_resourceFrames.TryGetValue(index, out var shown))
                PageTransitionHelper.PlayEnterTransition(shown);
        }

        /// <summary>按分类序号选中左侧对应项；内容切换由 SelectionChanged 统一驱动。</summary>
        private void SelectCategory(int index)
        {
            var target = CategoryList.Items
                .OfType<ListBoxItem>()
                .FirstOrDefault(li => li.Tag is string tag && tag == index.ToString());

            if (target != null && !ReferenceEquals(CategoryList.SelectedItem, target))
                CategoryList.SelectedItem = target;
        }
    }
}