using System.Windows.Controls;
using VibrantbitLauncher.ViewModels.Pages;

namespace VibrantbitLauncher.Views.Pages
{
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            var viewModel = App.Services.GetService(typeof(DashboardPageViewModel)) as DashboardPageViewModel;
            DataContext = viewModel;
            InitializeComponent();

            Loaded += async (s, e) =>
            {
                if (viewModel != null && !viewModel.IsLoaded)
                {
                    await viewModel.LoadNewsAsync();
                    viewModel.IsLoaded = true;
                }
            };
        }
    }
}
