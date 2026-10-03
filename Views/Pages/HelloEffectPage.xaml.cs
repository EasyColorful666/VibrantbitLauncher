using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace VibrantbitLauncher.Views.Pages
{
    public partial class HelloEffectPage : Page
    {
        public HelloEffectPage()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 获取路径实际长度用于 Dash 动画
            double hLen = PathH.Data.Bounds.Width * 4;
            double elloLen = PathEllo.Data.Bounds.Width * 4;

            PathH.StrokeDashArray = new DoubleCollection { hLen, hLen };
            PathH.StrokeDashOffset = hLen;
            PathEllo.StrokeDashArray = new DoubleCollection { elloLen, elloLen };
            PathEllo.StrokeDashOffset = elloLen;

            // h 笔画: 0.8s
            var hOpacity = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.4));
            var hDash = new DoubleAnimation(hLen, 0, TimeSpan.FromSeconds(0.8))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            PathH.BeginAnimation(OpacityProperty, hOpacity);
            PathH.BeginAnimation(Shape.StrokeDashOffsetProperty, hDash);

            // ello 笔画: 2.8s, 延迟 0.7s
            var elloOpacity = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.7))
            {
                BeginTime = TimeSpan.FromSeconds(0.7)
            };
            var elloDash = new DoubleAnimation(elloLen, 0, TimeSpan.FromSeconds(2.8))
            {
                BeginTime = TimeSpan.FromSeconds(0.7),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            PathEllo.BeginAnimation(OpacityProperty, elloOpacity);
            PathEllo.BeginAnimation(Shape.StrokeDashOffsetProperty, elloDash);

            // 动画完成后启用下一步按钮 (0.7 + 2.8 = 3.5s)
            var enableNext = new ObjectAnimationUsingKeyFrames();
            enableNext.KeyFrames.Add(new DiscreteObjectKeyFrame(true, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(3.5))));
            NextButton.BeginAnimation(IsEnabledProperty, enableNext);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new WelcomeCompletePage());
        }
    }
}
