using VibrantbitLauncher.ViewModels.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Wpf.Ui.Abstractions.Controls;

namespace VibrantbitLauncher.Views.Pages
{
    /// <summary>
    /// DownloadPage.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadPage : Page
    {

        DownloadPageViewModel viewModel = new DownloadPageViewModel();
        public DownloadPage()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                LoadMcVersions();
            }) ;
            this.DataContext = viewModel;
        }
        public async void LoadMcVersions()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                listBox.Items.Clear();
            });
            var entries = await VanillaInstaller.EnumerableMinecraftAsync();
            foreach (var entry in entries)
            {
                McVersion mc = new McVersion
                {
                    Version = entry.McVersion,
                    Date = entry.ReleaseTime.ToString("yyyy-MM-dd"),
                    DownloadCommand = new RelayCommand<string>(viewModel.Download)

                };
                if (!listBox.Items.Cast<McVersion>().Any(x => x == mc))
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        listBox.Items.Add(mc);
                    });
                }
                else
                {
                    break;
                }
                
                  
            }

        }
    }
    public class McVersion
    {
        public string Version { get; set; }
        public string Date { get; set; }

        public RelayCommand<string> DownloadCommand { get; set; }
    }
}
