

using VibrantbitLauncher.ViewModels.Pages;
using VibrantbitLauncher.ViewModels.Windows;
using System.Windows.Controls;
using System;
using GalaSoft.MvvmLight.Messaging;
using Wpf.Ui;
using Wpf.Ui.Controls;
using VibrantbitLauncher.Views.Windows;
using System.Windows.Media;

namespace VibrantbitLauncher.Views.Pages
{
    /// <summary>
    /// InstallPage.xaml 的交互逻辑
    /// </summary>
    public partial class InstallPage : Page
    {
        SnackbarService snackbarService = new();
        string McVersion;
        public InstallPage()
        {
            InitializeComponent();
            Messenger.Default.Register<String>(this, "McVersion", SetMcVersion);
            var _mainWindow = Application.Current.Windows
                                .Cast<Window>()
                                .FirstOrDefault(window => window is MainWindow) as MainWindow;
            snackbarService.SetSnackbarPresenter(SnackbarPresenter);
            
        }

        private void SetMcVersion(string msg)
        {
            McVersion = msg;
            snackbarService.Show("提示", $"正在安装版本{McVersion}", ControlAppearance.Info, null, snackbarService.DefaultTimeOut);
            this.DataContext = new InstallPageViewModel(msg);
        }

        private async void checkBox1_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var x in await ForgeInstaller.EnumerableForgeAsync(McVersion))
                {
                    listBox.Items.Add(x.DisplayVersion);
                }
            }
            catch (Exception ex)
            {
                
                snackbarService.Show("Error", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);

            }
            checkBox2.IsEnabled = false;
            checkBox4.IsEnabled = false;
            
        }

        private void checkBox1_Unchecked(object sender, RoutedEventArgs e)
        {
            // 清空comboBox2的items以避免重复添加
            listBox.Items.Clear();

            // 恢复其他checkBox的状态
            checkBox2.IsEnabled = true;
            checkBox4.IsEnabled = true;
            
        }
        private async void checkBox2_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var x in await FabricInstaller.EnumerableFabricAsync(McVersion))
                {
                    listBox1.Items.Add(x.DisplayVersion);
                }
            }
            catch (Exception ex)
            {
                
                
                snackbarService.Show("Error", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);

            }
            checkBox3.IsEnabled = false;
            checkBox4.IsEnabled = false;
            checkBox1.IsEnabled = false;
        }

        private void checkBox2_Unchecked(object sender, RoutedEventArgs e)
        {
            // 清空comboBox2的items以避免重复添加
            listBox1.Items.Clear();

            // 恢复其他checkBox的状态
            checkBox3.IsEnabled = true;
            checkBox4.IsEnabled = true;
            checkBox1.IsEnabled = true;
        }

        private async void checkBox3_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var x in await OptifineInstaller.EnumerableOptifineAsync(McVersion))
                {
                    listBox2.Items.Add(x.DisplayVersion);
                }
            }
            catch (Exception ex)
            {
                
                
                snackbarService.Show("Error", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);

            }
            checkBox4.IsEnabled = false;

            checkBox2.IsEnabled = false;
        }

        private void checkBox3_Unchecked(object sender, RoutedEventArgs e)
        {
            // 清空comboBox4的items以避免重复添加
            listBox2.Items.Clear();

            // 恢复其他checkBox和comboBox的状态
            checkBox4.IsEnabled = true;
            checkBox2.IsEnabled = true;
        }

        private async void checkBox4_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var x in await QuiltInstaller.EnumerableQuiltAsync(McVersion))
                {
                    listBox3.Items.Add(x.DisplayVersion);
                }
            }
            catch (Exception ex)
            {
                
                
                snackbarService.Show("Error", ex.Message, Wpf.Ui.Controls.ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);

            }
            checkBox3.IsEnabled = false;
            checkBox1.IsEnabled = false;
            checkBox2.IsEnabled = false;
        }

        private void checkBox4_Unchecked(object sender, RoutedEventArgs e)
        {
            // 清空comboBox5的items以避免重复添加
            listBox3.Items.Clear();

            // 恢复其他checkBox和comboBox的状态
            checkBox3.IsEnabled = true;
            checkBox1.IsEnabled = true;
            checkBox2.IsEnabled = true;

        }


    }
}
