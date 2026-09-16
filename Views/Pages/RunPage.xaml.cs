

using VibrantbitLauncher.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace VibrantbitLauncher.Views.Pages
{
    /// <summary>
    /// RunPage.xaml 的交互逻辑
    /// </summary>
    public partial class RunPage : System.Windows.Controls.Page
    {

       
        public RunPage()
        {
            var viewModel = App.Services.GetService(typeof(RunPageViewModel)) as RunPageViewModel;
            this.DataContext = viewModel;
            InitializeComponent();
            this.Loaded += async (s, e) =>
            {
                if (viewModel != null && !viewModel.IsLoaded)
                {
                    await viewModel.LoadAsync();
                    viewModel.IsLoaded = true;
                }
            };
        }


    }
}
