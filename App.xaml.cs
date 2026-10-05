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
                // 联机内核：驱动外挂的 easytier-core.exe，只有点了「启动服务」才会拉起进程
                services.AddSingleton<EasyTierService>();
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
            // ① 先把日志系统立起来：用内置默认值也无所谓，关键是让后面每一步失败都留得下证据
            LogService.Initialize(
                SettingsService.Current.LogLevel,
                SettingsService.Current.LogRetentionDays,
                SettingsService.Current.LogToDebugOutput);

            // ② 先读配置再建窗口：主窗口在 _host.StartAsync() 期间就会构造页面，
            //    页面初始化会读写 SettingsService.Current。若此时配置尚未加载，
            //    页面保存出去的默认值会把磁盘上的真实配置覆盖掉。
            SettingsService.Load();

            // ③ 按用户配置重建日志（等级 / 保留天数 / 是否输出到调试器）
            LogService.Reinitialize();
            // 用 InformationalVersion（如 "1.0.4.1"），并去掉 SourceLink 附加的 "+<commit>" 构建元数据；
            // 不用 GetName().Version（程序集版本，末尾会补 0 成四段）。
            // 项目版本号规则：小更新在末尾加一段（1.0.4 → 1.0.4.1），大更新第三段 +1（1.0.4 → 1.0.5）。
            LogService.WriteStartupBanner(GetDisplayVersion());

            await _host.StartAsync();

            // 后台预热版本清单，避免首次进入「下载中心」要等几秒才出列表
            MinecraftVersionCache.Preload();

            // 外观必须在窗口建好之后再应用：MainWindow 的 SystemThemeWatcher 会在创建时
            // 套用系统主题，这里随后覆盖成用户选择的「明暗主题 + 主题色/渐变 + 背景图」。
            SettingsService.ApplyAppearance();
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

            // 修复所有版本 JSON 中 releaseTime 的时区格式（+0000 → +00:00）。
            // 装了几十个版本时这是几十次文件读写，放到后台做，别拖住首帧渲染。
            await Task.Run(() => FixVersionJsonDateTimeFormat(SettingsService.Current.MinecraftFolder));

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
        /// 取用于展示的版本号（形如 "1.0.4" 或 "1.0.4.1"）。优先 <see cref="AssemblyInformationalVersionAttribute"/>，
        /// 并剥掉 SourceLink 附加的 "+&lt;commit&gt;" 构建元数据。
        /// </summary>
        /// <remarks>
        /// 项目版本号规则：小更新在末尾加一段（1.0.4 → 1.0.4.1），大更新第三段 +1（1.0.4 → 1.0.5）。
        /// 这里原样返回 csproj 的 &lt;Version&gt;，不做段数裁剪，四段号也能完整显示。
        /// </remarks>
        public static string GetDisplayVersion()
        {
            var informational = Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;
            if (!string.IsNullOrWhiteSpace(informational))
                return informational.Split('+')[0];

            return Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";
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
        /// Occurs when an exception is thrown by an application but not handled.
        /// 日志系统（LogService）已接管 AppDomain / TaskScheduler 级别的兜底，
        /// 这里只处理 UI 线程异常 —— 它是唯一能「记一笔然后继续跑」的。
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
                        Log.Debug("修正版本 JSON 的时区格式：{Path}", jsonPath);
                    }
                }

                if (fixedCount > 0)
                    Log.Information("已修正 {Count} 个版本 JSON 的时区格式", fixedCount);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "修正版本 JSON 时区格式失败：{Folder}", mcFolder);
            }
        }
    }
}
