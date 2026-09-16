using GalaSoft.MvvmLight.Messaging;
using MinecraftLaunch.Base.Models.Game;
using MinecraftLaunch.Extensions;
using MinecraftLaunch.Utilities;
using System.Collections.ObjectModel;
using System.IO;
using VibrantbitLauncher.ViewModels.Windows;
using VibrantbitLauncher.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Xunit.Internal;
using System.Threading.Tasks;
namespace VibrantbitLauncher.ViewModels.Pages
{
    public partial class RunPageViewModel : ViewModelBase
    {
        private MinecraftParser minecraftParser = new(".\\.minecraft");
        private List<JavaEntry> asyncJavas = [.. JavaUtil.EnumerableJavaAsync().ToBlockingEnumerable().ToList()];
        private ObservableCollection<MinecraftEntry> minecrafts = new ObservableCollection<MinecraftEntry>();
        private ObservableCollection<LocalVersion> minecraftVersions = new ObservableCollection<LocalVersion>();
        private ObservableCollection<string> javaVersions = new ObservableCollection<string>();
        private MinecraftRunner runner;
        private SnackbarService snackbarService = new();
        string selectedVersion = "";
        private string accountName;

        public bool IsLoaded { get; set; }
        public RelayCommand<SnackbarPresenter> LoadCommand { get; set; }
        public RelayCommand RefreshCommand { get; set; }

        public ObservableCollection<LocalVersion> MinecraftVersions
        {
            get => minecraftVersions;
            set => Set(ref minecraftVersions, value);
        }
        public string SelectedVersion
        {
            get => selectedVersion;
            set => Set(ref selectedVersion, value);
        }
        public string AccountName
        {
            get => accountName;
            set => Set(ref accountName, value);
        }

        public RunPageViewModel()
        {
            IsLoaded = false;
            LoadCommand = new RelayCommand<SnackbarPresenter>(Load);
            RefreshCommand = new RelayCommand(Refresh);
        }

        public async Task LoadAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    var javas = JavaUtil.EnumerableJavaAsync().ToBlockingEnumerable().ToList();
                    asyncJavas = javas;
                }
                catch { }

                try
                {
                    var localMinecrafts = minecraftParser.GetMinecrafts();
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        minecraftVersions.Clear();
                        foreach (var x in localMinecrafts)
                        {
                            minecraftVersions.Add(new LocalVersion { Version = x.Id, RunCommand = new(RunMinecraft), SettingsCommand = new(Settings) });
                        }
                    });

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        javaVersions.Clear();
                        foreach (var j in asyncJavas)
                        {
                            javaVersions.Add(j.JavaVersion);
                        }
                    });
                }
                catch (Exception ex)
                {
                    App.Current.Dispatcher.Invoke(() => snackbarService.Show("错误", $"无法获取本地版本: {ex.Message}", ControlAppearance.Danger, null, snackbarService.DefaultTimeOut));
                }
            });

            IsLoaded = true;
        }

        private void Refresh()
        {
            Task.Run(async () =>
            {
                try
                {
                    var localMinecrafts = minecraftParser.GetMinecrafts();
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        minecraftVersions.Clear();
                        foreach (var x in localMinecrafts)
                        {
                            minecraftVersions.Add(new LocalVersion { Version = x.Id, RunCommand = new(RunMinecraft), SettingsCommand = new(Settings) });
                        }
                    });
                }
                catch (Exception ex)
                {
                    App.Current.Dispatcher.Invoke(() => snackbarService.Show("错误", $"无法刷新本地版本: {ex.Message}", ControlAppearance.Danger, null, snackbarService.DefaultTimeOut));
                }
            });
        }

        private void Load(SnackbarPresenter snackbarPresenter)
        {
            this.snackbarService.SetSnackbarPresenter(snackbarPresenter);
            Refresh();
        }

        void Settings(string McVersion)
        {
            if (!string.IsNullOrEmpty(McVersion))
            {
                var window = new McSettingsWindow();
                Messenger.Default.Send(McVersion, "McVersionForSetting");
                window.ShowDialog();
            }
            else
            {
                snackbarService.Show("错误", "请选择一个版本", ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);
            }
        }

        async void RunMinecraft(string McVersion)
        {
            if (!string.IsNullOrEmpty(McVersion))
            {
                MinecraftEntry selectedMinecraftEntry = minecraftParser.GetMinecraft(McVersion);
                JavaEntry javaPath = null;
                try
                {
                    javaPath = selectedMinecraftEntry.GetAppropriateJava(asyncJavas);
                }
                catch (InvalidOperationException)
                {
                    snackbarService.Show("错误", "未找到匹配的 Java 版本，请安装 Java 或刷新 Java 列表", ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);
                    return;
                }
                runner = new(new LaunchConfig
                {
                    Account = MainWindowViewModel.MainModel.Account,
                    MaxMemorySize = 2048,
                    MinMemorySize = 512,
                    LauncherName = "VibrantbitLauncher",
                    JavaPath = javaPath,
                }, minecraftParser);

                    snackbarService.Show($"正在启动 {selectedMinecraftEntry.Id}，请稍等...", "提示", ControlAppearance.Info, null, snackbarService.DefaultTimeOut);
                    var process = await runner.RunAsync(selectedMinecraftEntry.Id);
                    process.Started += (_, _) =>
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            snackbarService.Show("成功", "Minecraft 已启动", ControlAppearance.Success, null, snackbarService.DefaultTimeOut);
                        });
                    };
                    process.OutputLogReceived += (_, arg) => writeLog(arg.Data.Time, arg.Data.Log);
                    process.Exited += (_, arg) =>
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            snackbarService.Show("提示", $"Minecraft 已退出，{process.ArgumentList}", ControlAppearance.Info, null, snackbarService.DefaultTimeOut);
                        });
                    };

            }
            else
            {
                snackbarService.Show("错误", "请选择一个版本", ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);
            }
        }

        void writeLog(string time, string message)
        {
            using (StreamWriter sw = new StreamWriter("log.txt", true))
            {
                sw.WriteLine($"{time}: {message}");
            }
        }
    }
    public class LocalVersion
    {
        public string Version { get; set; }

        public RelayCommand<string> RunCommand { get; set; }
        public RelayCommand<string> SettingsCommand { get; set; }

    }
}