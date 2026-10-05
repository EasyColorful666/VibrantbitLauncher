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
using VibrantbitLauncher.Services;
using Wpf.Ui.Controls;

namespace VibrantbitLauncher.Views.Windows
{
    /// <summary>
    /// InstallEasyTierWindow.xaml 的交互逻辑
    /// </summary>
    public partial class InstallEasyTierWindow : FluentWindow
    {
        public InstallEasyTierWindow()
        {
            InitializeComponent();

            // 兜底：挂到主窗口上，CenterOwner 才会真的居中于主窗口
            Owner ??= Application.Current?.MainWindow;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            button1.IsEnabled = false;

            // 和 EasyTierService 用同一个基准目录，避免安装到这里、启动时去那里找。
            // 这里要写全 System.IO.Path：本文件同时 using 了 System.Windows.Shapes（里面有 Path 形状类型）。
            var vblDirectory = EasyTierService.VblDirectory;
            var zipPath = System.IO.Path.Combine(vblDirectory, "easytier-windows-x86_64-v2.6.4.zip");

            try
            {
                Directory.CreateDirectory(vblDirectory);

                statusText.Text = "正在下载 EasyTier 核心…";

                await DownloadWithProgressAsync(
                    "https://v4.gh-proxy.org/https://github.com/EasyTier/EasyTier/releases/download/v2.6.4/easytier-windows-x86_64-v2.6.4.zip",
                    zipPath,
                    progressBar
                );

                statusText.Text = "正在解压…";
                ZipFile.ExtractToDirectory(zipPath, vblDirectory, Encoding.UTF8, true);

                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                statusText.Text = "安装完成";
                Close();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "下载 / 解压 EasyTier 核心失败");
                statusText.Text = $"安装失败：{ex.Message}";
                button1.IsEnabled = true;
            }
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

                        // 报告进度（服务器没给 Content-Length 时跳过，否则会算成 NaN）
                        if (totalBytes > 0)
                            progress.Value = (float)bytesRead / totalBytes * 100;
                    }
                }
            }
        }
    }
}
