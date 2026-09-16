using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using VibrantbitLauncher.Helpers;
using VibrantbitLauncher.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace VibrantbitLauncher.Views.Pages
{
    public partial class DashboardPage : System.Windows.Controls.Page
    {


        public DashboardPage()
        {
            var viewModel = App.Services.GetService(typeof(DashboardPageViewModel)) as DashboardPageViewModel;
            this.DataContext = viewModel;
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                if (viewModel != null && !viewModel.IsLoaded)
                {
                    await viewModel.LoadNewsAsync();
                    viewModel.IsLoaded = true;
                }
                listbox1.ItemsSource = viewModel?.News;
            };
        }

        private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            var newsList = await MojangNewsHelper.GetNewsListAsync();
            listbox1.ItemsSource = new ObservableCollection<NewsItem>(newsList);
        }
    }
}
