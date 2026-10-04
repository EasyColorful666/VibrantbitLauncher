using VibrantbitLauncher.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui;
using Wpf.Ui.Controls;
using CommunityToolkit.Mvvm.Messaging;

namespace VibrantbitLauncher.ViewModels.Windows
{
    public class MainWindowViewModel : ObservableObject
    {
        static MainModel _mainModel = new();
        static public MainModel MainModel {  get { return _mainModel; } set { _mainModel = value; } } 
        private readonly INavigationService _navigationService;
        private string imagePath;

        public string ImagePath
        {
            get => imagePath;
            set => SetProperty(ref imagePath, value);
        }
        public MainWindowViewModel(INavigationService navigationService)
        {
            imagePath = @"\Assets\Blank.jpg";
            _navigationService = navigationService;

            ShowMainWindowCommand = new RelayCommand(ShowMainWindow);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            ApplicationExitCommand = new RelayCommand(ApplicationExit);

            WeakReferenceMessenger.Default.Register<string, string>(this, "UpdateImagePath", (_, m) => UpdateImagePath(m));
            WeakReferenceMessenger.Default.Register<Type, string>(this, "NavigateTo", (_, m) => NavigateToPage(m));
            InitializeMenuItems();
        }

        private void NavigateToPage(Type pageType)
        {
            _navigationService.Navigate(pageType);
        }

        private void UpdateImagePath(string obj)
        {
            ImagePath = obj;
        }

        #region 属性

        private ObservableCollection<MenuItemViewModel> _trayMenuItems;
        public ObservableCollection<MenuItemViewModel> TrayMenuItems
        {
            get => _trayMenuItems;
            set => SetProperty(ref _trayMenuItems, value);
        }
        #endregion

        #region 命令
        public RelayCommand ShowMainWindowCommand { get; private set; }
        public RelayCommand OpenSettingsCommand { get; private set; }
        public RelayCommand ApplicationExitCommand { get; private set; }
        #endregion

        #region 私有方法
        private void InitializeMenuItems()
        {
            TrayMenuItems = new ObservableCollection<MenuItemViewModel>
            {
                new MenuItemViewModel { Header = "Home", Command = ShowMainWindowCommand },
                new MenuItemViewModel { Header = "Settings", Command = OpenSettingsCommand },
                new MenuItemViewModel { IsSeparator = true },
                new MenuItemViewModel { Header = "Exit", Command = ApplicationExitCommand }
            };
        }

        private void ShowMainWindow()
        {
            Application.Current.MainWindow?.Show();
            Application.Current.MainWindow?.Activate();
        }

        private void OpenSettings()
        {
            _navigationService.Navigate(typeof(Views.Pages.SettingsPage));
        }

        private void ApplicationExit()
        {
            Application.Current.Shutdown();
        }
        #endregion
    }

    public class MenuItemViewModel : ObservableObject
    {
        public string Header { get; set; }
        public RelayCommand Command { get; set; }
        public bool IsSeparator { get; set; }
    }
}
