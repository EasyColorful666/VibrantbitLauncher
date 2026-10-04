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
        /// 选择本地图片作为窗口背景。
        /// </summary>
        private void OnChooseBackgroundImage(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择背景图片",
                Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.webp;*.gif|所有文件|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true && DataContext is SettingsPageViewModel vm)
            {
                vm.SetBackgroundImage(dialog.FileName);
            }
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
