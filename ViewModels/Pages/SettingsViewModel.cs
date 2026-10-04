using CommunityToolkit.Mvvm.ComponentModel;
using VibrantbitLauncher.Helpers;
using CommunityToolkit.Mvvm.Input;
using MinecraftLaunch.Base.Models.Game;
using MinecraftLaunch.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using VibrantbitLauncher.Models;
using VibrantbitLauncher.Services;
using VibrantbitLauncher.ViewModels.Windows;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Appearance;

namespace VibrantbitLauncher.ViewModels.Pages
{
    public partial class SettingsPageViewModel : ObservableObject, INavigationAware
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
            PickWallpaperCommand = new RelayCommand<WallpaperOption>(PickWallpaper);
            ClearBackgroundCommand = new RelayCommand(ClearBackground);
            ResetAppearanceCommand = new RelayCommand(ResetAppearance);
            SelectAccentCommand = new RelayCommand<AccentPreset>(SelectAccent);
            SelectGradientEndCommand = new RelayCommand<AccentPreset>(SelectGradientEnd);
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
            // 回填控件状态期间不要触发「应用外观」，否则会把还没读完的配置写回去
            _suppressAppearanceApply = true;
            try
            {
                CurrentTheme = SettingsService.Current.Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase)
                    ? ApplicationTheme.Dark
                    : ApplicationTheme.Light;
                AppVersion = $"VibrantbitLauncher {GetAssemblyVersion()}";
                MinecraftFolder = SettingsService.Current.MinecraftFolder;

                UseAccentGradient = SettingsService.Current.UseAccentGradient;

                _selectedAccent = AccentPresets.FirstOrDefault(p =>
                    string.Equals(p.Primary, SettingsService.Current.AccentColor, StringComparison.OrdinalIgnoreCase))
                    ?? AccentPresets[0];
                _selectedGradientEnd = AccentPresets.FirstOrDefault(p =>
                    string.Equals(p.GradientEnd, SettingsService.Current.AccentColorEnd, StringComparison.OrdinalIgnoreCase))
                    ?? AccentPresets[0];

                OnPropertyChanged(nameof(SelectedAccent));
                OnPropertyChanged(nameof(SelectedGradientEnd));
                UpdatePresetSelection();

                BackgroundImagePath = SettingsService.Current.BackgroundImagePath ?? string.Empty;
                BackgroundImageOpacity = SettingsService.Current.BackgroundImageOpacity;

                _ = LoadJavaAsync();
            }
            finally
            {
                _suppressAppearanceApply = false;
            }

            _isInitialized = true;
        }

        private async Task LoadJavaAsync()
        {
            try
            {
                // 用自带实现而非 JavaUtil.EnumerableJavaAsync()：后者遇到空路径候选会整体抛异常
                var javas = await JavaHelper.FindJavasAsync();

                JavaEntries.Clear();
                foreach (var j in javas)
                    JavaEntries.Add(j);

                Serilog.Log.Information("Java 查找：{Count} 个 → {List}", javas.Count, string.Join(" | ", javas.Select(j => $"{j.JavaVersion} ({j.JavaPath})")));

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
            set => SetProperty(ref _appVersion, value);
        }

        public ApplicationTheme CurrentTheme
        {
            get => _currentTheme;
            set => SetProperty(ref _currentTheme, value);
        }

        public string MinecraftFolder
        {
            get => _minecraftFolder;
            set
            {
                if (SetProperty(ref _minecraftFolder, value))
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
                if (SetProperty(ref _selectedJava, value) && value != null)
                {
                    MainWindowViewModel.MainModel.JavaPath = value.JavaPath;
                    MainWindowViewModel.MainModel.Java = value;
                    SettingsService.Current.JavaPath = value.JavaPath;
                    SettingsService.Save();
                }
            }
        }

        // ===== 个性化：主题色 / 渐变色 / 背景图 =====

        private readonly ObservableCollection<AccentPreset> _accentPresets = new()
        {
            new AccentPreset("默认蓝", "#FF0078D4", "#FF00B7C3"),
            new AccentPreset("海洋青", "#FF0F6E56", "#FF5DCAA5"),
            new AccentPreset("森林绿", "#FF3B6D11", "#FF97C459"),
            new AccentPreset("紫罗兰", "#FF534AB7", "#FFAFA9EC"),
            new AccentPreset("玫瑰粉", "#FF993556", "#FFED93B1"),
            new AccentPreset("落日橙", "#FFBA7517", "#FFEF9F27"),
            new AccentPreset("石榴红", "#FFA32D2D", "#FFE24B4A"),
            new AccentPreset("靛青蓝", "#FF185FA5", "#FF85B7EB"),
        };

        private readonly ObservableCollection<WallpaperOption> _wallpapers = new()
        {
            new WallpaperOption("草地", "/Assets/bg1.png"),
            new WallpaperOption("暮色", "/Assets/bg2.png"),
            new WallpaperOption("峡谷", "/Assets/bg3.png"),
            new WallpaperOption("雪原", "/Assets/bg4.png"),
        };

        private AccentPreset? _selectedAccent;
        private AccentPreset? _selectedGradientEnd;
        private bool _useAccentGradient;
        private bool _suppressAppearanceApply;
        private string _backgroundImagePath = string.Empty;
        private double _backgroundImageOpacity = 0.85;

        /// <summary>可选主题色。</summary>
        public ObservableCollection<AccentPreset> AccentPresets => _accentPresets;

        /// <summary>内置壁纸。</summary>
        public ObservableCollection<WallpaperOption> Wallpapers => _wallpapers;

        /// <summary>当前主色调。</summary>
        public AccentPreset? SelectedAccent => _selectedAccent;

        /// <summary>当前渐变终点色，仅在开启「主题渐变色」时参与显示。</summary>
        public AccentPreset? SelectedGradientEnd => _selectedGradientEnd;

        /// <summary>是否启用主题渐变色。</summary>
        public bool UseAccentGradient
        {
            get => _useAccentGradient;
            set
            {
                if (SetProperty(ref _useAccentGradient, value) && !_suppressAppearanceApply)
                    ApplyAppearanceSettings();
            }
        }

        /// <summary>当前背景图路径（本地绝对路径或内置壁纸 pack URI），空表示未设置。</summary>
        public string BackgroundImagePath
        {
            get => _backgroundImagePath;
            private set
            {
                if (SetProperty(ref _backgroundImagePath, value))
                {
                    OnPropertyChanged(nameof(HasBackgroundImage));
                    OnPropertyChanged(nameof(BackgroundImageDisplay));
                }
            }
        }

        public bool HasBackgroundImage => !string.IsNullOrWhiteSpace(_backgroundImagePath);

        /// <summary>背景图在界面上显示的名字。</summary>
        public string BackgroundImageDisplay
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_backgroundImagePath))
                    return "未设置（使用系统 Mica 质感底）";

                var builtin = _wallpapers.FirstOrDefault(w =>
                    string.Equals(w.ImagePath, _backgroundImagePath, StringComparison.OrdinalIgnoreCase));

                return builtin != null ? $"内置壁纸 · {builtin.Name}" : Path.GetFileName(_backgroundImagePath);
            }
        }

        /// <summary>背景图不透明度，拖动即时预览。</summary>
        public double BackgroundImageOpacity
        {
            get => _backgroundImageOpacity;
            set
            {
                if (SetProperty(ref _backgroundImageOpacity, value) && !_suppressAppearanceApply)
                {
                    SettingsService.Current.BackgroundImageOpacity = value;
                    SettingsService.ApplyBackground();
                    SettingsService.Save();
                }
            }
        }

        public RelayCommand<WallpaperOption> PickWallpaperCommand { get; }
        public RelayCommand ClearBackgroundCommand { get; }
        public RelayCommand ResetAppearanceCommand { get; }
        public RelayCommand<AccentPreset> SelectAccentCommand { get; }
        public RelayCommand<AccentPreset> SelectGradientEndCommand { get; }

        /// <summary>点选主色调。</summary>
        private void SelectAccent(AccentPreset? preset)
        {
            if (preset == null || ReferenceEquals(preset, _selectedAccent))
                return;

            _selectedAccent = preset;
            OnPropertyChanged(nameof(SelectedAccent));
            UpdatePresetSelection();
            ApplyAppearanceSettings();
        }

        /// <summary>点选渐变终点色。</summary>
        private void SelectGradientEnd(AccentPreset? preset)
        {
            if (preset == null || ReferenceEquals(preset, _selectedGradientEnd))
                return;

            _selectedGradientEnd = preset;
            OnPropertyChanged(nameof(SelectedGradientEnd));
            UpdatePresetSelection();
            ApplyAppearanceSettings();
        }

        /// <summary>把选中状态同步到各预设，供色板画出选中环。</summary>
        private void UpdatePresetSelection()
        {
            foreach (var preset in _accentPresets)
            {
                preset.IsAccentSelected = ReferenceEquals(preset, _selectedAccent);
                preset.IsGradientSelected = ReferenceEquals(preset, _selectedGradientEnd);
            }
        }

        /// <summary>把当前的主题色 / 渐变配置写回设置并立即应用。</summary>
        private void ApplyAppearanceSettings()
        {
            if (SelectedAccent != null)
                SettingsService.Current.AccentColor = SelectedAccent.Primary;
            if (SelectedGradientEnd != null)
                SettingsService.Current.AccentColorEnd = SelectedGradientEnd.GradientEnd;
            SettingsService.Current.UseAccentGradient = UseAccentGradient;

            // 走「刷新主题」入口：只改 SystemAccentColor 不会刷新主题词典里固化的强调色资源
            SettingsService.ApplyAccentChange();
            SettingsService.Save();
        }

        private void PickWallpaper(WallpaperOption? option)
        {
            if (option != null)
                SetBackgroundImage(option.ImagePath);
        }

        /// <summary>设置背景图（本地绝对路径或内置壁纸 pack URI）；传空字符串表示清除。</summary>
        public void SetBackgroundImage(string? path)
        {
            BackgroundImagePath = path ?? string.Empty;
            SettingsService.Current.BackgroundImagePath = BackgroundImagePath;
            SettingsService.ApplyBackground();
            SettingsService.Save();
        }

        private void ClearBackground() => SetBackgroundImage(string.Empty);

        /// <summary>恢复默认外观：默认蓝 + 关闭渐变 + 移除背景图。</summary>
        private void ResetAppearance()
        {
            _suppressAppearanceApply = true;
            try
            {
                _useAccentGradient = false;
                OnPropertyChanged(nameof(UseAccentGradient));
                _selectedAccent = _accentPresets[0];
                OnPropertyChanged(nameof(SelectedAccent));
                _selectedGradientEnd = _accentPresets[0];
                OnPropertyChanged(nameof(SelectedGradientEnd));
                BackgroundImagePath = string.Empty;
                _backgroundImageOpacity = 0.85;
                OnPropertyChanged(nameof(BackgroundImageOpacity));
                UpdatePresetSelection();
            }
            finally
            {
                _suppressAppearanceApply = false;
            }

            SettingsService.Current.UseAccentGradient = false;
            SettingsService.Current.AccentColor = _accentPresets[0].Primary;
            SettingsService.Current.AccentColorEnd = _accentPresets[0].GradientEnd;
            SettingsService.Current.BackgroundImagePath = string.Empty;
            SettingsService.Current.BackgroundImageOpacity = 0.85;

            SettingsService.ApplyAccentChange();
            SettingsService.ApplyBackground();
            SettingsService.Save();
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

            SettingsService.Current.Theme = theme.ToString();
            // 走统一入口：切主题的同时一并刷新背景遮罩与主题色
            SettingsService.ApplyTheme();
            CurrentTheme = theme;
            SettingsService.Save();
        }
    }
}
