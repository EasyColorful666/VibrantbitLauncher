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
                services.AddSingleton<AccountPage>();
                services.AddSingleton<AccountPageViewModel>();
                services.AddSingleton<DownloadPage>();
                services.AddSingleton<DownloadPageViewModel>();
                services.AddSingleton<DownloadResourcesPage>();
                services.AddSingleton<DownloadResourcesViewModel>();
                services.AddSingleton<ModPage>();
                services.AddSingleton<MultiplayerPage>();
                services.AddSingleton<MultiplayerPageViewModel>();
                services.AddSingleton<InstallPage>();
                services.AddSingleton<InstallPageViewModel>();
                services.AddSingleton<RunPage>();
                services.AddSingleton<RunPageViewModel>();
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsPageViewModel>();
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
            await _host.StartAsync();

            // 加载配置并应用主题/主题色（自动完成，用户不可见）
            SettingsService.Load();
            SettingsService.ApplyTheme();

            InitializeHelper.Initialize(settings => {
                settings.MaxThread = 256; // 最大下载线程
                settings.MaxFragment = 128; // 最大文件分片数量
                settings.MaxRetryCount = 4; // 最大下载重试次数
                settings.IsEnableMirror = false; // 是否启用 Minecraft 国内下载镜像源（BMCLAPI）
                settings.IsEnableFragment = false; // 是否启用分片下载
            });


        }

        /// <summary>
        /// Occurs when the application is closing.
        /// </summary>
        private async void OnExit(object sender, ExitEventArgs e)
        {
            await _host.StopAsync();

            _host.Dispose();
        }

        /// <summary>
        /// Occurs when an exception is thrown by an application but not handled.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
        }
        

            
       


    }
}
