using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Wpf.Ui.Controls;

namespace VibrantbitLauncher.Views.Windows
{
    /// <summary>
    /// InstallEasyTierWindow.xaml 的交互逻辑
    /// </summary>
    public partial class InstallEasyTierWindow : Window
    {
        public InstallEasyTierWindow()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            button1.IsEnabled = false;
            DirectoryInfo directoryInfo = new DirectoryInfo("./VBL/");
            if (!directoryInfo.Exists)
            {
                System.IO.Directory.CreateDirectory("./VBL/");
            }
            await DownloadWithProgressAsync(
                "https://v4.gh-proxy.org/https://github.com/EasyTier/EasyTier/releases/download/v2.6.4/easytier-windows-x86_64-v2.6.4.zip",
                ".\\VBL\\easytier-windows-x86_64-v2.6.4.zip",
                progressBar
            );

            ZipFile.ExtractToDirectory( ".\\VBL\\easytier-windows-x86_64-v2.6.4.zip", ".\\VBL\\", Encoding.UTF8, true);
            File.Delete(".\\VBL\\easytier-windows-x86_64-v2.6.4.zip");
            Close();
        }
        public static async Task DownloadWithProgressAsync(string url, string savePath, ProgressBar progress)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                HttpResponseMessage response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                using (FileStream fileStream = new FileStream(savePath, FileMode.Create))
                {
                    var totalBytes = response.Content.Headers.ContentLength.GetValueOrDefault();
                    var buffer = new byte[8192];
                    long bytesRead = 0;
                    int bytesReceived;

                    while ((bytesReceived = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesReceived);
                        bytesRead += bytesReceived;

                        // 报告进度（如果有）
                        progress.Value = ((float)bytesRead / totalBytes)*100;
                    }
                }
            }
        }
    }
}
