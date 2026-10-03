using System.Windows;
using VibrantbitLauncher.Views.Pages;
using Wpf.Ui;
using Wpf.Ui.Appearance;

namespace VibrantbitLauncher.Views.Windows
{
    public partial class WelcomeWindow
    {
        public WelcomeWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.Apply(ApplicationTheme.Light);
            MainFrame.Navigate(new WelcomePage());
        }
    }
}
