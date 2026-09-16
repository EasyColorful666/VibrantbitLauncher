using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using VibrantbitLauncher.ViewModels.Pages;
using VibrantbitLauncher.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace VibrantbitLauncher.Views.Pages
{
    /// <summary>
    /// MultiplayerPage.xaml 的交互逻辑
    /// </summary>
    public partial class MultiplayerPage : System.Windows.Controls.Page
    {
        private SnackbarService snackbarService = new();
        public string uid = Guid.NewGuid().ToString();
        public MultiplayerPage()
        {
            InitializeComponent();
            this.DataContext = new MultiplayerPageViewModel();
            r1.IsChecked = true;
            this.snackbarService.SetSnackbarPresenter(SnackbarPresenter);
            this.Loaded += (s, e) =>
            {
                DirectoryInfo directoryInfo = new DirectoryInfo("./VBL/");
                if (!directoryInfo.Exists)
                {
                    System.IO.Directory.CreateDirectory("./VBL/");
                }
                if (!System.IO.File.Exists("./VBL/easytier-windows-x86_64/easytier-core.exe"))
                {
                    snackbarService.Show("未找到 EasyTier 核心文件，请确保已正确安装 EasyTier", "错误", ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);
                    new InstallEasyTierWindow().ShowDialog();
                }
            };
            textBox1.Text = uid;
        }
        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (!System.IO.File.Exists("./VBL/easytier-windows-x86_64/easytier-core.exe"))
            {
                snackbarService.Show("未找到 EasyTier 核心文件，请确保已正确安装 EasyTier", "错误", ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);
                new InstallEasyTierWindow().ShowDialog();
                return;
            }
            Process p = new();
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = "./VBL/easytier-windows-x86_64/easytier-core.exe";
            processStartInfo.Arguments = $" --network-name {uid} --network-secret {uid} -p tcp://easytier.weiai.org.cn:11010";
            processStartInfo.UseShellExecute = false;
            //processStartInfo.RedirectStandardOutput = true;

            p = Process.Start(processStartInfo);
            snackbarService.Show("提示", "EasyTier 启动成功",ControlAppearance.Success,null,snackbarService.DefaultTimeOut);
            //p.BeginOutputReadLine();
            //p.BeginErrorReadLine();
            //p.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
            //{
            //    if (!String.IsNullOrEmpty(e.Data))
            //    {
            //        Application.Current.Dispatcher.Invoke(() =>
            //        {
            //            try
            //            {
            //                Regex IPAd = new Regex(@"\b10(\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){3}\b");
            //                MatchCollection MatchResult = IPAd.Matches(e.Data);
            //                textBox3.Text = MatchResult[0].Value;
            //            }
            //            catch (Exception ex)
            //            {

            //            }
            //        });
            //    }
            //});

        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            // 按进程名获取并结束
            Process[] processes = Process.GetProcessesByName("easytier-core.exe");
            foreach (Process p in processes)
            {
                p.Kill(); // 强制结束
                snackbarService.Show("提示", "EasyTier 已关闭", ControlAppearance.Info, null, snackbarService.DefaultTimeOut);
            }
            
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            if (!System.IO.File.Exists("./VBL/easytier-windows-x86_64/easytier-core.exe"))
            {
                snackbarService.Show("未找到 EasyTier 核心文件，请确保已正确安装 EasyTier", "错误", ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);
                new InstallEasyTierWindow().ShowDialog();
                return;
            }
            Process p = new();
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = "./VBL/easytier-windows-x86_64/easytier-core.exe";
            processStartInfo.Arguments = $" --network-name {textBox3.Text} --network-secret {textBox3.Text} -p tcp://easytier.weiai.org.cn:11010";
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.CreateNoWindow = true;
            p = Process.Start(processStartInfo);
            snackbarService.Show("提示", "EasyTier 启动成功", ControlAppearance.Success, null, snackbarService.DefaultTimeOut);
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            grid1.IsEnabled = true;
            grid2.IsEnabled = false;
        }

        private void RadioButton_Checked_1(object sender, RoutedEventArgs e)
        {
            grid1.IsEnabled = false;
            grid2.IsEnabled = true;
        }
    }
}
