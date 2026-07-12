using VibrantbitLauncher.Models;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace VibrantbitLauncher.ViewModels.Windows
{
    public class MainWindowViewModel : ViewModelBase
    {
        static MainModel _mainModel = new();
        static public MainModel MainModel {  get { return _mainModel; } set { _mainModel = value; } } 
        private readonly INavigationService _navigationService;

        public MainWindowViewModel(INavigationService navigationService)
        {

            _navigationService = navigationService;

            // 初始化命令
            ShowMainWindowCommand = new RelayCommand(ShowMainWindow);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            ApplicationExitCommand = new RelayCommand(ApplicationExit);

            // 初始化菜单项
            InitializeMenuItems();
        }

        #region 属性

        private ObservableCollection<MenuItemViewModel> _trayMenuItems;
        public ObservableCollection<MenuItemViewModel> TrayMenuItems
        {
            get => _trayMenuItems;
            set => Set(ref _trayMenuItems, value);
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
            


            // 系统托盘菜单项（使用 ViewModel 替代直接创建 UI 控件）
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

    /// <summary>
    /// 托盘菜单项 ViewModel（纯数据对象）
    /// </summary>
    public class MenuItemViewModel : ViewModelBase
    {
        public string Header { get; set; }
        public RelayCommand Command { get; set; }
        public bool IsSeparator { get; set; }
    }
}
