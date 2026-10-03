using VibrantbitLauncher.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace VibrantbitLauncher.Views.Pages
{
    public partial class RunPage : System.Windows.Controls.Page
    {
        public RunPage()
        {
            var viewModel = App.Services.GetService(typeof(RunPageViewModel)) as RunPageViewModel;
            this.DataContext = viewModel;
            InitializeComponent();

            this.Loaded += (s, e) =>
            {
                if (viewModel != null)
                {
                    viewModel.LoadCommand.Execute(SnackbarPresenter);
                }
            };
        }
    }
}
