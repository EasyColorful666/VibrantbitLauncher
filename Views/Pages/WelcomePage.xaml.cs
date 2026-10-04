using System.Windows;
using System.Windows.Controls;

namespace VibrantbitLauncher.Views.Pages
{
    public partial class WelcomePage : Page
    {
        public WelcomePage()
        {
            InitializeComponent();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            // 向导第 2 步改为实际配置（游戏目录 / Java / 主题）。
            // 原来的 HelloEffectPage（hello 手写动画）保留在项目中，如需恢复把这个目标改回去即可。
            NavigationService.Navigate(new WelcomeSetupPage());
        }
    }
}
