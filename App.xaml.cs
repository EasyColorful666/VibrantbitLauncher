using System.IO;
using System.Reflection;
using System.Windows.Threading;
using VibrantbitLauncher.Services;
using VibrantbitLauncher.ViewModels.Pages;
using VibrantbitLauncher.ViewModels.Windows;
using VibrantbitLauncher.Views.Pages;
using VibrantbitLauncher.Views.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Wpf.Ui;
using Wpf.Ui.DependencyInjection;
using MinecraftLaunch;
namespace VibrantbitLauncher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
        // https://docs.microsoft.com/dotnet/core/extensions/generic-host
        // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
        // https://docs.microsoft.com/dotnet/core/extensions/configuration
        // https://docs.microsoft.com/dotnet/core/extensions/logging
        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => { c.SetBasePath(Path.GetDirectoryName(AppContext.BaseDirectory)); })
            .ConfigureServices((context, services) =>
            {
                services.AddNavigationViewPageProvider();

                services.AddHostedService<ApplicationHostService>();

                // Theme manipulation
                services.AddSingleton<IThemeService, ThemeService>();

                // TaskBar manipulation
                services.AddSingleton<ITaskBarService, TaskBarService>();

                // Service containing navigation, same as INavigationWindow... but without window
                services.AddSingleton<INavigationService, NavigationService>();

                // Main window with navigation
                services.AddSingleton<INavigationWindow,MainWindow>();
                services.AddSingleton<MainWindowViewModel>();

                services.AddSingleton<DashboardPage>();
                services.AddSingleton<DashboardPageViewModel>();
                services.AddSingleton<LaunchPage>();
                services.AddSingleton<NewsDetailPage>();
                services.AddSingleton<AccountPage>();
                services.AddSingleton<AccountPageViewModel>();
                services.AddSingleton<DownloadPage>();
                services.AddSingleton<DownloadPageViewModel>();
                services.AddSingleton<DownloadHubPage>();
                services.AddSingleton<DownloadCenterPage>();
                services.AddSingleton<DownloadCenterViewModel>();
                services.AddSingleton<DownloadTaskService>();
                services.AddSingleton<DownloadResourcesPage>();
                services.AddSingleton<DownloadResourcesViewModel>();
                services.AddSingleton<ModPage>();
                services.AddSingleton<MultiplayerPage>();
                services.AddSingleton<MultiplayerPageViewModel>();
                services.AddSingleton<InstallPage>();
                services.AddSingleton<InstallPageViewModel>();
                services.AddSingleton<RunPage>();
                services.AddSingleton<RunPageViewModel>();
                services.AddSingleton<VersionManagePage>();
                services.AddSingleton<VersionManageViewModel>();
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsPageViewModel>();
                // 欢迎流程页面：注册进 DI，避免被导航时 _pageService.GetPage 返回 null 抛异常
                services.AddSingleton<WelcomePage>();
                services.AddSingleton<WelcomeSetupPage>();
                services.AddSingleton<HelloEffectPage>();
                services.AddSingleton<WelcomeAccountPage>();
                services.AddSingleton<WelcomeCompletePage>();
            }).Build();

        /// <summary>
        /// Gets services.
        /// </summary>
        public static IServiceProvider Services
        {
            get { return _host.Services; }
        }

        /// <summary>
        /// Occurs when the application is loading.
        /// </summary>
        private async void OnStartup(object sender, StartupEventArgs e)
        { 
            InitLogging();
            Log.Information("VibrantbitLauncher 启动");

            await _host.StartAsync();

            // 后台预热版本清单，避免首次进入「下载中心」要等几秒才出列表
            MinecraftVersionCache.Preload();

            // 加载配置并应用主题/主题色（自动完成，用户不可见）
            SettingsService.Load();
            SettingsService.ApplyTheme();
            Log.Information("配置：Theme={Theme} IsFirstRun={First} MinecraftFolder={Folder} JavaPath={Java}",
                SettingsService.Current.Theme,
                SettingsService.Current.IsFirstRun,
                SettingsService.Current.MinecraftFolder,
                string.IsNullOrEmpty(SettingsService.Current.JavaPath) ? "(未设置)" : SettingsService.Current.JavaPath);

            InitializeHelper.Initialize(settings => {
                settings.MaxThread = 256; // 最大下载线程
                settings.MaxFragment = 128; // 最大文件分片数量
                settings.MaxRetryCount = 4; // 最大下载重试次数
                settings.IsEnableMirror = false; // 是否启用 Minecraft 国内下载镜像源（BMCLAPI）
                settings.IsEnableFragment = false; // 是否启用分片下载
            });

            // 修复所有版本 JSON 中 releaseTime 的时区格式（+0000 → +00:00）
            FixVersionJsonDateTimeFormat(SettingsService.Current.MinecraftFolder);

            // 首次运行显示欢迎窗口
            if (SettingsService.Current.IsFirstRun)
            {
                var mainWindow = Current.Windows.OfType<MainWindow>().FirstOrDefault();
                mainWindow?.Hide();

                var welcome = new WelcomeWindow();
                welcome.ShowDialog();

                SettingsService.Current.IsFirstRun = false;
                SettingsService.Save();

                mainWindow?.Show();
                mainWindow?.Activate();
            }


        }

        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();

            _host.Dispose();

            Log.CloseAndFlush();
        }

        /// <summary>
        /// 初始化日志：写入 logs/ 目录，并接管全局未处理异常。
        /// 原先未处理异常只被写入 Debug 输出后直接吞掉，出问题时无从排查。
        /// </summary>
        private static void InitLogging()
        {
            try
            {
                var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
                Directory.CreateDirectory(logDir);

                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.File(
                        Path.Combine(logDir, "vibrantbit-.log"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        shared: true)
                    .CreateLogger();

                AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                    Log.Fatal(args.ExceptionObject as Exception, "未处理的应用域异常");

                TaskScheduler.UnobservedTaskException += (_, args) =>
                {
                    Log.Error(args.Exception, "未观察到的任务异常");
                    args.SetObserved();
                };
            }
            catch
            {
                // 日志初始化失败不应影响启动
            }
        }

        /// <summary>
        /// Occurs when an exception is thrown by an application but not handled.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception, "未处理的 UI 线程异常");
            e.Handled = true;
        }

        /// <summary>
        /// 扫描 .minecraft/versions 下所有 JSON，将 releaseTime 等字段的 +HHMM 时区格式修正为 +HH:MM
        /// </summary>
        private static void FixVersionJsonDateTimeFormat(string mcFolder)
        {
            try
            {
                var versionsDir = Path.Combine(mcFolder, "versions");
                if (!Directory.Exists(versionsDir)) return;

                var regex = new System.Text.RegularExpressions.Regex(@"([+-]\d{2})(\d{2})""");
                int fixedCount = 0;

                foreach (var dir in Directory.GetDirectories(versionsDir))
                {
                    var dirName = Path.GetFileName(dir);
                    var jsonPath = Path.Combine(dir, dirName + ".json");
                    if (!File.Exists(jsonPath)) continue;

                    var text = File.ReadAllText(jsonPath);
                    if (regex.IsMatch(text))
                    {
                        var fixedText = regex.Replace(text, "$1:$2\"");
                        File.WriteAllText(jsonPath, fixedText);
                        fixedCount++;
                        System.Diagnostics.Debug.WriteLine($"[App] Fixed DateTime format in {jsonPath}");
                    }
                }

                if (fixedCount > 0)
                    System.Diagnostics.Debug.WriteLine($"[App] Fixed {fixedCount} version JSON files");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] FixVersionJsonDateTimeFormat failed: {ex.Message}");
            }
        }
    }
}
