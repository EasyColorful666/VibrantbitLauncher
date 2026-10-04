using VibrantbitLauncher.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

        private ImageSource? backgroundImage;
        private double backgroundImageOpacity = 0.85;

        /// <summary>窗口背景图；为 null 时窗口保持纯 Mica 底。</summary>
        public ImageSource? BackgroundImage
        {
            get => backgroundImage;
            private set
            {
                if (SetProperty(ref backgroundImage, value))
                    OnPropertyChanged(nameof(HasBackgroundImage));
            }
        }

        /// <summary>是否已设置可用背景图（决定遮罩层是否显示）。</summary>
        public bool HasBackgroundImage => backgroundImage != null;

        public double BackgroundImageOpacity
        {
            get => backgroundImageOpacity;
            set => SetProperty(ref backgroundImageOpacity, value);
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
            WeakReferenceMessenger.Default.Register<BackgroundChangedMessage>(this, (_, m) => ApplyBackground(m));
            InitializeMenuItems();
        }

        /// <summary>
        /// 响应背景图变更：安全加载图片（失败则退回 Mica），并同步不透明度。
        /// </summary>
        private void ApplyBackground(BackgroundChangedMessage message)
        {
            BackgroundImage = LoadBackgroundImage(message.ImagePath);
            BackgroundImageOpacity = Math.Clamp(message.Opacity, 0.05, 1.0);
        }

        /// <summary>
        /// 加载背景图。支持本地绝对路径与内置壁纸的 pack URI；
        /// 用 OnLoad + Freeze 避免占用文件句柄，并限制解码宽度防止大图吃内存。
        /// </summary>
        private static ImageSource? LoadBackgroundImage(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;

                if (path.StartsWith("/", StringComparison.Ordinal))
                {
                    // 内置壁纸：代码里必须用绝对 pack URI —— 相对 URI 只在 XAML 解析上下文
                    // 里才会被解析成 pack 资源，直接赋值给 BitmapImage 会被当成文件路径。
                    bitmap.UriSource = new Uri("pack://application:,,," + path, UriKind.Absolute);
                }
                else if (File.Exists(path))
                {
                    bitmap.UriSource = new Uri(path, UriKind.Absolute);
                }
                else
                {
                    Serilog.Log.Warning("背景图不存在，已忽略：{Path}", path);
                    return null;
                }

                bitmap.DecodePixelWidth = 2560;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "背景图加载失败：{Path}", path);
                return null;
            }
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
