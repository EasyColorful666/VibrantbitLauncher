using System.Windows.Media;
using VibrantbitLauncher.Models;
using Wpf.Ui.Abstractions.Controls;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight;
using MinecraftLaunch.Launch;
using MinecraftLaunch.Base;
using MinecraftLaunch.Components;
using MinecraftLaunch.Base.Utilities;
using MinecraftLaunch.Base.Interfaces;
using VibrantbitLauncher.ViewModels.Windows;
using MinecraftLaunch.Components.Installer;
using VibrantbitLauncher.Views.Windows;
using VibrantbitLauncher.Views.Pages;
using GalaSoft.MvvmLight.Messaging;
using System.Windows;

namespace VibrantbitLauncher.ViewModels.Pages
{
    public class DownloadPageViewModel : ViewModelBase
    {
        public List<IInstallEntry> installEntries = new();
        private string mcFolder;


        List<McVersion> mcVersions = [];
        public List<McVersion> McVersions
        {
            get => mcVersions;
            set => Set(ref mcVersions, value);
        }
        public DownloadPageViewModel()
        {
            mcFolder = MainWindowViewModel.MainModel.MinecraftFolder;
        }
        



        public void Download(string McVersion)
        {
            var _mainWindow = Application.Current.Windows
.Cast<Window>()
.FirstOrDefault(window => window is MainWindow) as MainWindow;

            if (_mainWindow != null)
            {
                InstallPage installPage = new();
                _mainWindow.Navigate(installPage.GetType());
                Messenger.Default.Send<String>(McVersion, "McVersion");

            }
        }
    }
    
}
