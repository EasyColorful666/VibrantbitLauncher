using VibrantbitLauncher.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace VibrantbitLauncher.Views.Pages
{
    public partial class SettingsPage : System.Windows.Controls.Page
    {
        

        public SettingsPage()
        {
            var ViewModel = new SettingsPageViewModel();
            DataContext = ViewModel;

            InitializeComponent();
        }
    }
}
