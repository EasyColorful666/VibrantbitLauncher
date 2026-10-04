using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VibrantbitLauncher.Helpers
{
    /// <summary>
    /// 数值减法转换器。
    ///
    /// 用途：WPF-UI 的 NavigationView 把页面内容放在 ScrollViewer 里，页面自身高度不受限，
    /// 于是长内容（新闻 140 条）会把页面撑高、内部滚动失效。页面根容器需要主动把高度
    /// 钉在外层的可视高度上 —— 但要再减去页面自己的内边距，所以需要这个转换器。
    /// </summary>
    public sealed class SubtractConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not double source || double.IsNaN(source) || double.IsInfinity(source))
                return DependencyProperty.UnsetValue;

            var amount = 0d;
            if (parameter is string text)
                double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out amount);

            return Math.Max(0, source - amount);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
