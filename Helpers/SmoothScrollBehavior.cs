using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace VibrantbitLauncher.Helpers
{
    /// <summary>
    /// 给列表的滚轮滚动加上缓动，替代 WPF 默认那种"整项跳"的观感。
    ///
    /// 用法：在 ListBox / ScrollViewer 上写 <c>helpers:SmoothScrollBehavior.IsEnabled="True"</c>。
    ///
    /// 注意：列表要按**像素**滚动（<c>VirtualizingPanel.ScrollUnit="Pixel"</c>），
    /// 否则 ScrollViewer.VerticalOffset 的单位是"项"而不是像素，一次滚动会变成跳若干项。
    /// </summary>
    public static class SmoothScrollBehavior
    {
        /// <summary>一格滚轮滚动的像素数（Windows 默认 3 行 ≈ 96px）。</summary>
        private const double PixelsPerNotch = 96;

        /// <summary>一次滚轮的动画时长。太长会"拖"，太短会生硬。</summary>
        private const double DurationMs = 220;

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(SmoothScrollBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        /// <summary>
        /// 滚动动画的载体。
        /// ScrollViewer.VerticalOffset 是只读依赖属性，不能直接当动画的 TargetProperty，
        /// 这里用一个可写的附加属性接住动画值，再由它的变更回调驱动 ScrollToVerticalOffset。
        /// </summary>
        private static readonly DependencyProperty ScrollTargetProperty =
            DependencyProperty.RegisterAttached(
                "ScrollTarget",
                typeof(double),
                typeof(SmoothScrollBehavior),
                new PropertyMetadata(0d, OnScrollTargetChanged));

        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement element)
                return;

            element.PreviewMouseWheel -= OnPreviewMouseWheel;

            if (e.NewValue is true)
                element.PreviewMouseWheel += OnPreviewMouseWheel;
        }

        private static void OnScrollTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollViewer viewer)
                viewer.ScrollToVerticalOffset((double)e.NewValue);
        }

        private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not UIElement element || e.Handled)
                return;

            var viewer = FindScrollViewer(element);

            // 内容不足一屏时不接管，让事件继续冒泡（交给外层可能的滚动容器）
            if (viewer == null || viewer.ScrollableHeight <= 0)
                return;

            e.Handled = true;

            var target = Math.Clamp(
                viewer.VerticalOffset - e.Delta / 120d * PixelsPerNotch,
                0,
                viewer.ScrollableHeight);

            var animation = new DoubleAnimation(viewer.VerticalOffset, target, TimeSpan.FromMilliseconds(DurationMs))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            viewer.BeginAnimation(ScrollTargetProperty, animation);

            // 动画结束后停在目标位置（HoldEnd），这一句同时把基准值对齐到目标
            viewer.ScrollToVerticalOffset(target);
        }

        private static ScrollViewer? FindScrollViewer(DependencyObject root)
        {
            if (root is ScrollViewer viewer)
                return viewer;

            var count = VisualTreeHelper.GetChildrenCount(root);
            for (var i = 0; i < count; i++)
            {
                if (FindScrollViewer(VisualTreeHelper.GetChild(root, i)) is { } found)
                    return found;
            }

            return null;
        }
    }
}
