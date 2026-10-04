using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using VibrantbitLauncher.Services;
using VibrantbitLauncher.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace VibrantbitLauncher.Views.Pages
{
    /// <summary>
    /// 开机向导第 2 步：游戏目录 / Java / 主题。
    /// 直接复用 SettingsPageViewModel，保证与「设置」页的行为完全一致。
    /// </summary>
    public partial class WelcomeSetupPage : Page
    {
        private readonly SettingsPageViewModel _viewModel;

        public WelcomeSetupPage()
        {
            _viewModel = App.Services.GetService(typeof(SettingsPageViewModel)) as SettingsPageViewModel
                         ?? new SettingsPageViewModel();

            DataContext = _viewModel;
            InitializeComponent();

            Loaded += async (_, _) =>
            {
                if (_viewModel is INavigationAware aware)
                    await aware.OnNavigatedToAsync();
            };
        }

        private void OnSelectFolder(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "选择 Minecraft 文件夹",
                InitialDirectory = Environment.CurrentDirectory
            };

            if (dialog.ShowDialog() == true)
                _viewModel.MinecraftFolder = dialog.FolderName;
        }

        private void OnBrowseJava(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "选择 java.exe",
                Filter = "Java 可执行文件 (java.exe;javaw.exe)|java.exe;javaw.exe|所有文件 (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() != true)
                return;

            var entry = new MinecraftLaunch.Base.Models.Game.JavaEntry
            {
                JavaPath = dialog.FileName,
                Is64bit = true
            };

            _viewModel.JavaEntries.Add(entry);
            _viewModel.SelectedJava = entry;
        }

        private void OnBack(object sender, RoutedEventArgs e)
            => NavigationService?.Navigate(new WelcomePage());

        private void OnNext(object sender, RoutedEventArgs e)
        {
            SettingsService.Save();
            NavigationService?.Navigate(new WelcomeAccountPage());
        }
    }
}
