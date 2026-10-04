using System.Windows.Media;
using VibrantbitLauncher.Models;
using Wpf.Ui.Abstractions.Controls;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MinecraftLaunch.Launch;
using MinecraftLaunch.Base;
using MinecraftLaunch.Components;
using MinecraftLaunch.Base.Utilities;
using MinecraftLaunch.Base.Interfaces;
using VibrantbitLauncher.ViewModels.Windows;
using MinecraftLaunch.Components.Installer;
using VibrantbitLauncher.Views.Windows;
using VibrantbitLauncher.Views.Pages;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows;
using System.Collections.ObjectModel;

namespace VibrantbitLauncher.ViewModels.Pages
{
    public class DownloadPageViewModel : ObservableObject
    {
        public List<IInstallEntry> installEntries = new();
        private string mcFolder;


        ObservableCollection<McVersion> mcVersions = new ObservableCollection<McVersion>();
        public ObservableCollection<McVersion> McVersions
        {
            get => mcVersions;
            set => SetProperty(ref mcVersions, value);
        }
        public RelayCommand<string> DownloadCommand { get; }
        public bool IsLoaded { get; set; }
        public DownloadPageViewModel()
        {
            DownloadCommand = new RelayCommand<string>(Download);
            IsLoaded = false;
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
                WeakReferenceMessenger.Default.Send<string, string>(McVersion, "McVersion");

            }
        }
    }
    
}
