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
    public partial class InstallPage : Page
    {
        SnackbarService snackbarService = new();
        string McVersion;

        public InstallPage()
        {
            InitializeComponent();
            Messenger.Default.Register<string>(this, "McVersion", SetMcVersion);
            snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        }

        private async void SetMcVersion(string msg)
        {
            McVersion = msg;
            snackbarService.Show("提示", $"正在安装版本{McVersion}", ControlAppearance.Info, null, snackbarService.DefaultTimeOut);
            var vm = new InstallPageViewModel(msg);
            this.DataContext = vm;

            // 加载前重置 CheckBox 状态
            checkBox1.IsEnabled = checkBox2.IsEnabled = checkBox3.IsEnabled = checkBox4.IsEnabled = true;
            checkBox1.IsChecked = checkBox2.IsChecked = checkBox3.IsChecked = checkBox4.IsChecked = false;

            await vm.Load(SnackbarPresenter);
            ApplyLoaderAvailability();
        }

        /// <summary>
        /// 根据 ViewModel 中的 Available 状态禁用 404 的加载器选项。
        /// </summary>
        private void ApplyLoaderAvailability()
        {
            if (DataContext is not InstallPageViewModel vm) return;
            if (!vm.ForgeAvailable) checkBox1.IsEnabled = false;
            if (!vm.FabricAvailable) checkBox2.IsEnabled = false;
            if (!vm.NeoforgeAvailable) checkBox3.IsEnabled = false;
            if (!vm.QuiltAvailable) checkBox4.IsEnabled = false;
        }

        // checkBox1 = Forge
        // 勾选 Forge → 禁用 Fabric、Quilt（NeoForge 可与 Forge 共存）
        private void checkBox1_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            checkBox2.IsEnabled = false;
            checkBox4.IsEnabled = false;
        }

        private void checkBox1_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not InstallPageViewModel vm) return;
            checkBox2.IsEnabled = vm.FabricAvailable;
            checkBox4.IsEnabled = vm.QuiltAvailable;
            vm.SelectForgeVersion = string.Empty;
        }

        // checkBox2 = Fabric
        // 勾选 Fabric → 禁用 Forge、Quilt、NeoForge
        private void checkBox2_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            checkBox1.IsEnabled = false;
            checkBox3.IsEnabled = false;
            checkBox4.IsEnabled = false;
        }

        private void checkBox2_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not InstallPageViewModel vm) return;
            checkBox1.IsEnabled = vm.ForgeAvailable;
            checkBox3.IsEnabled = vm.NeoforgeAvailable;
            checkBox4.IsEnabled = vm.QuiltAvailable;
            vm.SelectFabricVersion = string.Empty;
        }

        // checkBox3 = NeoForge
        // 勾选 NeoForge → 禁用 Fabric、Quilt
        private void checkBox3_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            checkBox2.IsEnabled = false;
            checkBox4.IsEnabled = false;
        }

        private void checkBox3_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not InstallPageViewModel vm) return;
            checkBox2.IsEnabled = vm.FabricAvailable;
            checkBox4.IsEnabled = vm.QuiltAvailable;
            vm.SelectNeoforgeVersion = string.Empty;
        }

        // checkBox4 = Quilt
        // 勾选 Quilt → 禁用所有其他（Forge、Fabric、NeoForge）
        private void checkBox4_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            checkBox1.IsEnabled = false;
            checkBox2.IsEnabled = false;
            checkBox3.IsEnabled = false;
        }

        private void checkBox4_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not InstallPageViewModel vm) return;
            checkBox1.IsEnabled = vm.ForgeAvailable;
            checkBox2.IsEnabled = vm.FabricAvailable;
            checkBox3.IsEnabled = vm.NeoforgeAvailable;
            vm.SelectQuiltVersion = string.Empty;
        }
    }
}
