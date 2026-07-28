using MinecraftLaunch.Base.Models.Network;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Windows.Threading;
using VibrantbitLauncher.ViewModels.Pages;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;

namespace VibrantbitLauncher.Views.Pages
{
    /// <summary>
    /// DownloadPage.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadPage : Page
    {
        private ICollectionView versionView;
        SnackbarService snackbarService = new();
        DownloadPageViewModel viewModel = new DownloadPageViewModel();
        private DispatcherTimer debounceTimer;
        public DownloadPage()
        {
            InitializeComponent();
            LoadMcVersions();
            this.DataContext = viewModel;
            debounceTimer = new DispatcherTimer();
            debounceTimer.Interval = TimeSpan.FromMilliseconds(260);
            debounceTimer.Tick += (s, e) =>
            {
                debounceTimer.Stop();
                versionView.Refresh();          // 真正执行过滤
            };
        }

        private void Page_Loaded(object sender, EventArgs e)
        {
            snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        }
        public async void LoadMcVersions()
        {
            ObservableCollection<McVersion> mcVersions = new ObservableCollection<McVersion>();
            await Task.Run(async () =>
            {
                var entries = await VanillaInstaller.EnumerableMinecraftAsync();
                foreach (var entry in entries)
                {
                    mcVersions.Add(new McVersion
                    {
                        Version = entry.McVersion,
                        Date = entry.ReleaseTime.ToString("yyyy-MM-dd"),
                        DownloadCommand = new RelayCommand<string>(viewModel.Download)

                    });
                }
                
            });

            App.Current.Dispatcher.Invoke(() =>
            {
                viewModel.McVersions.Clear();
                viewModel.McVersions = mcVersions;
                listBox.Items.Clear();
                versionView = CollectionViewSource.GetDefaultView(mcVersions);
                versionView.Filter = VersionFilter;
                listBox.ItemsSource = versionView;
                textBox.Text = "";
                versionView.Refresh();
            });

        }

        private bool VersionFilter(object item)
        {
            if (item is not McVersion ver)
                return false;

            // 搜索框为空 → 显示所有
            if (string.IsNullOrEmpty(textBox.Text))
                return true;

            // 只查 Version，忽略大小写
            return ver.Version.Contains(textBox.Text, StringComparison.OrdinalIgnoreCase);
        }

        // 搜索框内容变化时立即刷新视图（无防抖，实时过滤）
        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            debounceTimer.Stop();   // 停止之前的计时
            debounceTimer.Start();  // 重新开始计时
        }
    }

    public class McVersion
    {

        public string Version { get; set; }
        public string Date { get; set; }

        public RelayCommand<string> DownloadCommand { get; set; }
    }
}
