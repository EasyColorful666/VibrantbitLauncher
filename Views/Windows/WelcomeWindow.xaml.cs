using VibrantbitLauncher.Views.Pages;

namespace VibrantbitLauncher.Views.Windows
{
    public partial class WelcomeWindow
    {
        public WelcomeWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new WelcomePage());
        }
    }
}
