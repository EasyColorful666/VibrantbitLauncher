using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using MinecraftLaunch.Base.Models.Authentication;
using MinecraftLaunch.Base.Models.Game;
using MinecraftLaunch.Components.Authenticator;
using MinecraftLaunch.Extensions;
using MinecraftLaunch.Utilities;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Principal;
using System.Windows.Media;
using VibrantbitLauncher.Models;
using VibrantbitLauncher.ViewModels.Windows;
using VibrantbitLauncher.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;
using Xunit.Internal;
namespace VibrantbitLauncher.ViewModels.Pages
{
    public partial class RunPageViewModel : ViewModelBase
    {
        private MinecraftParser minecraftParser = ".\\.minecraft";
        ObservableCollection<JavaEntry> asyncJavas = [.. JavaUtil.EnumerableJavaAsync().ToBlockingEnumerable()];
        ObservableCollection<MinecraftEntry> minecrafts = [];
        ObservableCollection<LocalVersion> minecraftVersions = [];
        ObservableCollection<string> javaVersions = [];
        MinecraftRunner runner;
        private SnackbarService snackbarService = new();
        string selectedVersion = "";
        private string accountName;

        public RelayCommand<string> RunMinecraftCommand { get; set; }
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
            asyncJavas.ForEach(x =>
            {
                javaVersions.Add(x.JavaVersion);
            });
            try
            {
                minecrafts = new ObservableCollection<MinecraftEntry>( minecraftParser.GetMinecrafts());
                minecrafts.Clear();
                minecraftParser.GetMinecrafts().ForEach(x =>
                {
                    minecraftVersions.Add(new LocalVersion { Version = x.Version.VersionId, RunCommand = new(RunMinecraft), SettingsCommand = new(Settings) });
                });
            }
            catch (Exception ex)
            {
                snackbarService.Show("错误", $"无法获取本地版本{ex}, ControlAppearance.Danger, null, snackbarService.DefaultTimeOut",ControlAppearance.Danger,null,snackbarService.DefaultTimeOut);
            }
            RunMinecraftCommand = new RelayCommand<string>(RunMinecraft);
            LoadCommand = new RelayCommand<SnackbarPresenter>(Load);
            RefreshCommand = new RelayCommand(Refresh);

        }
        void Refresh()
        {
            Task.Run(() =>
            {
                minecraftParser = new(".\\.minecraft");
                minecrafts = new ObservableCollection<MinecraftEntry>(minecraftParser.GetMinecrafts());
                MinecraftVersions.Clear();
                minecraftParser.GetMinecrafts().ForEach(x =>
                {
                    MinecraftVersions.Add(new LocalVersion { Version = x.Version.VersionId, RunCommand = new(RunMinecraft), SettingsCommand = new(Settings) });
                });
            });
        }
        private void Load(SnackbarPresenter snackbarPresenter)
        {
            this.snackbarService.SetSnackbarPresenter(snackbarPresenter);

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
                MinecraftEntry selectedMinecraftEntry = minecrafts.First(x => x.Version.VersionId == McVersion);
                runner = new(new LaunchConfig
                {
                    Account = MainWindowViewModel.MainModel.Account,
                    MaxMemorySize = 2048,
                    MinMemorySize = 512,
                    LauncherName = "VibrantbitLauncher",
                    JavaPath = selectedMinecraftEntry.GetAppropriateJava(asyncJavas),
                }, minecraftParser);
                    try
                    {
                        snackbarService.Show($"正在启动 {selectedMinecraftEntry.Version.VersionId}，请稍等...", "提示", ControlAppearance.Info ,null ,snackbarService.DefaultTimeOut);
                        var process = await runner.RunAsync(selectedMinecraftEntry);
                        process.Started += (_, _) => {
                            App.Current.Dispatcher.Invoke(() => {
                                snackbarService.Show("成功", "Minecraft 已启动", ControlAppearance.Success, null, snackbarService.DefaultTimeOut);
                            });
                        }; 
                        process.OutputLogReceived += (_, arg) => writeLog(arg.Data.Time,arg.Data.Log);
                        process.Exited += (_, arg) =>
                        {
                            App.Current.Dispatcher.Invoke(() => {
                                snackbarService.Show("提示", $"Minecraft 已退出，{process.ArgumentList}", ControlAppearance.Info, null, snackbarService.DefaultTimeOut);
                            } );
                        };
                    }
                    catch (Exception)
                    {
                        snackbarService.Show("错误", "请登录一个用户", ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);
                    }
                
            }
            else 
            {
                snackbarService.Show("错误", "请选择一个版本", ControlAppearance.Danger,null,snackbarService.DefaultTimeOut);
            }

        }
        void writeLog(string time,string message)
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
