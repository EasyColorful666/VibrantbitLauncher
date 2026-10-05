using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace VibrantbitLauncher.Helpers
{
    /// <summary>
    /// 分栏页面的切换过渡：内容自下方轻微上浮 + 淡入。
    ///
    /// 下载中心（左侧分类）和设置页（左侧分类）共用同一套观感，
    /// 各页面不必再各写一份动画代码。
    /// </summary>
    public static class PageTransitionHelper
    {
        /// <summary>
        /// 让 <paramref name="element"/> 播一次入场动画（上浮 18px + 淡入，约 260ms）。
        /// 切换时旧内容应同时收起（改 Visibility），否则两层叠在一起会有重影。
        /// </summary>
        public static void PlayEnterTransition(UIElement? element)
        {
            if (element == null)
                return;

            if (element.RenderTransform is not TranslateTransform transform)
            {
                transform = new TranslateTransform();
                element.RenderTransform = transform;
            }

            // FillBehavior 用 Stop，并先把基准值写回终态：
            // 动画结束后属性不会被"钉"在动画值上，下一次切换能重新从起点播一遍。
            transform.Y = 0;
            element.Opacity = 1;

            var slide = new DoubleAnimation(18, 0, TimeSpan.FromMilliseconds(260))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.Stop
            };
            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.Stop
            };

            transform.BeginAnimation(TranslateTransform.YProperty, slide);
            element.BeginAnimation(UIElement.OpacityProperty, fade);
        }
    }
}
