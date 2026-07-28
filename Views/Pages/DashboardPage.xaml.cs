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
            this.DataContext = new DashboardPageViewModel();
            InitializeComponent();
            Loaded += DashboardPage_Loaded;
        }

        private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            var newsList = await MojangNewsHelper.GetNewsListAsync();
            listbox1.ItemsSource = new ObservableCollection<NewsItem>(newsList);
        }
    }
}
