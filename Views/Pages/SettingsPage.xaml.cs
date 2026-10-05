using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using VibrantbitLauncher.Helpers;
using VibrantbitLauncher.ViewModels.Pages;

namespace VibrantbitLauncher.Views.Pages
{
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            InitializeComponent();

            // InitializeComponent 期间 XAML 的 IsSelected="True" 会触发 SelectionChanged，
            // 那时容器还没建好，这里按当前选中项补一次。
            ApplyCurrentCategory();
        }

        private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => ApplyCurrentCategory();

        /// <summary>按左侧当前选中项切换右侧面板；只改 Visibility，不重建内容。</summary>
        private void ApplyCurrentCategory()
        {
            if (PanelHost == null)
                return;

            if (CategoryList.SelectedItem is not ListBoxItem item
                || item.Tag is not string tag
                || !int.TryParse(tag, out var index))
            {
                return;
            }

            var panels = new[] { HomePanel, PersonalizationPanel, OtherPanel };
            if (index < 0 || index >= panels.Length)
                return;

            for (var i = 0; i < panels.Length; i++)
                panels[i].Visibility = i == index ? Visibility.Visible : Visibility.Collapsed;

            PageTransitionHelper.PlayEnterTransition(panels[index]);
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
        /// 把最新一个日志文件另存到用户选定的位置。
        /// </summary>
        private void OnExportLog(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "导出日志",
                FileName = $"vibrantbit-{DateTime.Now:yyyyMMdd}.log",
                DefaultExt = ".log",
                Filter = "日志文件|*.log|文本文件|*.txt|所有文件|*.*"
            };

            if (dialog.ShowDialog() == true && DataContext is SettingsPageViewModel vm)
            {
                vm.ExportLog(dialog.FileName);
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
