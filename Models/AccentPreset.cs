using System;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VibrantbitLauncher.Models
{
    /// <summary>
    /// 主题色预设。
    ///
    /// Primary 是主色调（决定按钮、选中项等所有控件的强调色）；
    /// GradientEnd 是渐变终点色，只有在「主题渐变色」开启时才参与显示。
    /// 两个颜色都存成 #AARRGGBB 字符串，与 settings.json 里的字段保持一致。
    ///
    /// IsAccentSelected / IsGradientSelected 是两块色板的「选中」状态。用完即弃的
    /// ListBox 选中态在 WPF 里会因为两块色板共用同一个集合（也就共用同一个
    /// CollectionView）而互相串味，所以这里改成由 ViewModel 驱动各自独立的标记位。
    /// </summary>
    public sealed class AccentPreset : ObservableObject
    {
        public AccentPreset(string name, string primary, string gradientEnd)
        {
            Name = name;
            Primary = primary;
            GradientEnd = gradientEnd;

            PrimaryColor = Parse(primary);
            GradientEndColor = Parse(gradientEnd);

            SwatchBrush = Freeze(new SolidColorBrush(PrimaryColor));
            GradientEndBrush = Freeze(new SolidColorBrush(GradientEndColor));
            GradientBrush = Freeze(new LinearGradientBrush(
                PrimaryColor, GradientEndColor, new Point(0, 0), new Point(1, 1)));
        }

        /// <summary>预设名，用于提示文本。</summary>
        public string Name { get; }

        /// <summary>主色（#AARRGGBB）。</summary>
        public string Primary { get; }

        /// <summary>渐变终点色（#AARRGGBB）。</summary>
        public string GradientEnd { get; }

        public Color PrimaryColor { get; }

        public Color GradientEndColor { get; }

        /// <summary>主色实心圆点，用于主题色色板。</summary>
        public SolidColorBrush SwatchBrush { get; }

        /// <summary>终点色实心圆点，用于渐变终点色色板。</summary>
        public SolidColorBrush GradientEndBrush { get; }

        /// <summary>该预设对应的「主色 → 终点色」渐变。</summary>
        public LinearGradientBrush GradientBrush { get; }

        private bool _isAccentSelected;
        private bool _isGradientSelected;

        /// <summary>是否被选为当前主题色。</summary>
        public bool IsAccentSelected
        {
            get => _isAccentSelected;
            set => SetProperty(ref _isAccentSelected, value);
        }

        /// <summary>是否被选为当前渐变终点色。</summary>
        public bool IsGradientSelected
        {
            get => _isGradientSelected;
            set => SetProperty(ref _isGradientSelected, value);
        }

        private static Color Parse(string hex)
        {
            try
            {
                return (Color)ColorConverter.ConvertFromString(hex);
            }
            catch
            {
                return Color.FromRgb(0x00, 0x78, 0xD4);
            }
        }

        private static T Freeze<T>(T freezable) where T : Freezable
        {
            if (freezable.CanFreeze)
                freezable.Freeze();
            return freezable;
        }
    }

    /// <summary>
    /// 内置壁纸选项。ImagePath 是 pack 资源 URI（形如 /Assets/bg1.png），可直接绑定 Image.Source。
    /// </summary>
    public sealed class WallpaperOption
    {
        public WallpaperOption(string name, string imagePath)
        {
            Name = name;
            ImagePath = imagePath;
        }

        public string Name { get; }

        public string ImagePath { get; }
    }
}
