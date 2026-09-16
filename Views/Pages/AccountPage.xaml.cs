using VibrantbitLauncher.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls; 

namespace VibrantbitLauncher.Views.Pages
{
    public partial class AccountPage : System.Windows.Controls.Page
    {

        
        public AccountPage()
        {
            var viewModel = App.Services.GetService(typeof(AccountPageViewModel)) as AccountPageViewModel;
            this.DataContext = viewModel;
            InitializeComponent();
            this.Loaded += (s, e) =>
            {
                if (viewModel != null && !viewModel.IsLoaded)
                {
                    viewModel.Load(SnackbarPresenter);
                    viewModel.IsLoaded = true;
                }
            };
        }

        
    }
}
