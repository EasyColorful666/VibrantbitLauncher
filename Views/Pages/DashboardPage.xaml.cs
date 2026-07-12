using System.Diagnostics;
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
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Task.Run(() => {
                App.Current.Dispatcher.Invoke(() =>
                {
                    t1.Text = $"今天是 {DateTime.Today.ToString("yyyy-MM-dd")}";
                });
            } );
            Task.Run(() => {
                while (true)
                {
                    var ramCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        t2.Text = ($"内存占用率: {ramCounter.NextValue():F2}%");
                        p2.Value = ramCounter.NextValue();
                    });
                    System.Threading.Thread.Sleep(500);
                }
            } );


        }
    }
}
