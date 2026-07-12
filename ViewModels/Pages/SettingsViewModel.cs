using GalaSoft.MvvmLight;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;
using System.Threading.Tasks;
using System; // 补全String使用
using System.Reflection;
using System.Windows.Input;

namespace VibrantbitLauncher.ViewModels.Pages
{
    public partial class SettingsPageViewModel : ViewModelBase, INavigationAware
    {
        private bool _isInitialized = false;
        private string _appVersion = String.Empty;
        private ApplicationTheme _currentTheme = ApplicationTheme.Light;

        // 添加RelayCommand命令绑定
        public ICommand ChangeThemeCommand { get; private set; }

        public SettingsPageViewModel()
        {
            
            // 初始化切换主题命令
            ChangeThemeCommand = new RelayCommand<string>(OnChangeTheme);
        }

        // 实现INavigationAware接口
        Task INavigationAware.OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();
            return Task.CompletedTask;
        }

        Task INavigationAware.OnNavigatedFromAsync() => Task.CompletedTask;

        private void InitializeViewModel()
        {
            CurrentTheme = ApplicationThemeManager.GetAppTheme();
            AppVersion = $"ColorfulCraftLauncher - {GetAssemblyVersion()}";
            _isInitialized = true;
        }

        // 添加绑定属性
        public string AppVersion
        {
            get => _appVersion;
            set => Set(ref _appVersion, value);
        }

        // 添加主题绑定属性
        public ApplicationTheme CurrentTheme
        {
            get => _currentTheme;
            set => Set(ref _currentTheme, value);
        }

        private string GetAssemblyVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString() 
                ?? String.Empty;
        }

        private void OnChangeTheme(string parameter)
        {
            switch (parameter)
            {
                case "theme_light":
                    if (CurrentTheme == ApplicationTheme.Light)
                        return;
                    
                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                    CurrentTheme = ApplicationTheme.Light;
                    break;
                
                case "theme_dark": // 添加明确的dark处理
                default:
                    if (CurrentTheme == ApplicationTheme.Dark)
                        return;
                    
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                    CurrentTheme = ApplicationTheme.Dark;
                    break;
            }
        }
    }
}
