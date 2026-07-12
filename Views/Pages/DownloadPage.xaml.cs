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
            var entries = await VanillaInstaller.EnumerableMinecraftAsync();
            foreach (var entry in entries)
            {
                if (listBox.Items.Contains(new McVersion
                {
                    Version = entry.McVersion,
                    Date = entry.ReleaseTime.ToString("yyyy-MM-dd"),
                    DownloadCommand = new RelayCommand<string>(viewModel.Download)

                }))
                {
                    break;
                }
                else
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        listBox.Items.Add(new McVersion
                        {
                            Version = entry.McVersion,
                            Date = entry.ReleaseTime.ToString("yyyy-MM-dd"),
                            DownloadCommand = new RelayCommand<string>(viewModel.Download)

                        });
                    });
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
