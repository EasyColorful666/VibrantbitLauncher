using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using VibrantbitLauncher.ViewModels.Pages;

namespace VibrantbitLauncher.Views.Pages
{
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 选择 Minecraft 游戏文件夹。
        /// </summary>
        private void OnSelectMinecraftFolder(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "选择 Minecraft 文件夹",
                InitialDirectory = Environment.CurrentDirectory
            };

            if (dialog.ShowDialog() == true && DataContext is SettingsPageViewModel vm)
            {
                vm.MinecraftFolder = dialog.FolderName;
            }
        }
    }
}
