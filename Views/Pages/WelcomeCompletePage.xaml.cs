using System.Windows;
using System.Windows.Controls;

namespace VibrantbitLauncher.Views.Pages
{
    public partial class WelcomeCompletePage : Page
    {
        public WelcomeCompletePage()
        {
            InitializeComponent();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }
    }
}
