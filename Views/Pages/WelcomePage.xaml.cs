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
            NavigationService.Navigate(new HelloEffectPage());
        }
    }
}
