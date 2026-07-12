using VibrantbitLauncher.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls; 

namespace VibrantbitLauncher.Views.Pages
{
    public partial class AccountPage : System.Windows.Controls.Page
    {

        
        public AccountPage()
        {
            this.DataContext = new AccountPageViewModel();

            InitializeComponent();
        }

        
    }
}
