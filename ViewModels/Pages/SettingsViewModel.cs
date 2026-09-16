using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using MinecraftLaunch.Base.Models.Game;
using MinecraftLaunch.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using VibrantbitLauncher.Services;
using VibrantbitLauncher.ViewModels.Windows;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;

namespace VibrantbitLauncher.ViewModels.Pages
{
    public partial class SettingsPageViewModel : ViewModelBase, INavigationAware
    {
        private bool _isInitialized = false;
        private string _appVersion = string.Empty;
        private ApplicationTheme _currentTheme = ApplicationTheme.Light;

        // 游戏设置
        private string _minecraftFolder = "./.minecraft";
        private JavaEntry? _selectedJava;
        private ObservableCollection<JavaEntry> _javaEntries = new();

        public ICommand ChangeThemeCommand { get; private set; }
        public ICommand RefreshJavaCommand { get; private set; }

        public SettingsPageViewModel()
        {
            ChangeThemeCommand = new RelayCommand<string>(OnChangeTheme);
            RefreshJavaCommand = new RelayCommand(async () => await LoadJavaAsync());
        }

        Task INavigationAware.OnNavigatedToAsync()
        {
            if (!_isInitialized)
                InitializeViewModel();
            return Task.CompletedTask;
        }

        Task INavigationAware.OnNavigatedFromAsync() => Task.CompletedTask;

        private void InitializeViewModel()
        {
            CurrentTheme = SettingsService.Current.Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase)
                ? ApplicationTheme.Dark
                : ApplicationTheme.Light;
            AppVersion = $"ColorfulCraftLauncher - {GetAssemblyVersion()}";
            MinecraftFolder = SettingsService.Current.MinecraftFolder;
            _ = LoadJavaAsync();
            _isInitialized = true;
        }

        private async Task LoadJavaAsync()
        {
            try
            {
                var javas = new List<JavaEntry>();
                await foreach (var j in JavaUtil.EnumerableJavaAsync())
                    javas.Add(j);

                JavaEntries.Clear();
                foreach (var j in javas)
                    JavaEntries.Add(j);

                var saved = MainWindowViewModel.MainModel.JavaPath;
                SelectedJava = !string.IsNullOrEmpty(saved)
                    ? JavaEntries.FirstOrDefault(j => j.JavaPath == saved)
                    : JavaEntries.FirstOrDefault();
            }
            catch
            {
                // 枚举失败时静默
            }
        }

        // ===== 属性 =====

        public string AppVersion
        {
            get => _appVersion;
            set => Set(ref _appVersion, value);
        }

        public ApplicationTheme CurrentTheme
        {
            get => _currentTheme;
            set => Set(ref _currentTheme, value);
        }

        public string MinecraftFolder
        {
            get => _minecraftFolder;
            set
            {
                if (Set(ref _minecraftFolder, value))
                {
                    MainWindowViewModel.MainModel.MinecraftFolder = value;
                    SettingsService.Current.MinecraftFolder = value;
                    SettingsService.Save();
                }
            }
        }

        public ObservableCollection<JavaEntry> JavaEntries => _javaEntries;

        public JavaEntry? SelectedJava
        {
            get => _selectedJava;
            set
            {
                if (Set(ref _selectedJava, value) && value != null)
                {
                    MainWindowViewModel.MainModel.JavaPath = value.JavaPath;
                    SettingsService.Current.JavaPath = value.JavaPath;
                    SettingsService.Save();
                }
            }
        }

        // ===== 方法 =====

        private string GetAssemblyVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
        }

        private void OnChangeTheme(string parameter)
        {
            var theme = parameter == "theme_light" ? ApplicationTheme.Light : ApplicationTheme.Dark;
            if (CurrentTheme == theme)
                return;

            ApplicationThemeManager.Apply(theme);
            CurrentTheme = theme;
            SettingsService.Current.Theme = theme.ToString();
            SettingsService.Save();
        }
    }
}
