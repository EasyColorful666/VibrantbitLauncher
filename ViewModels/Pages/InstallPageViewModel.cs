using System.Windows.Media;
using VibrantbitLauncher.Models;
using Wpf.Ui.Abstractions.Controls;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight;
using System;
using MinecraftLaunch.Components.Installer;
using MinecraftLaunch.Base.Models.Network;
using MinecraftLaunch.Base.Interfaces; 
using VibrantbitLauncher.ViewModels.Windows;
using VibrantbitLauncher.Views.Windows;
using Wpf.Ui.Controls;
using Wpf.Ui;
using MinecraftLaunch.Base.Models.Game;
namespace VibrantbitLauncher.ViewModels.Pages
{
    public class InstallPageViewModel : ViewModelBase
    {
        private List<IInstallEntry> installEntries = new List<IInstallEntry>();
        public string mcVersion = string.Empty;
        private string mcFolder = "./.minecraft";
        private List<string> forgeVersions = [];
        private List<string> fabricVersions = [];
        private List<string> optfineVersions = [];
        private List<string> quiltVersions = [];
        private string selectForgeVersion = string.Empty;
        private string selectFabricVersion = string.Empty;
        private string selectOptifineVersion = string.Empty;
        private string selectQuiltVersion = string.Empty;
        private int installProgress = 0;
        private string installStep = string.Empty;
        private string speed;
        private SnackbarService snackbarService = new();

        public RelayCommand InstallCommand { get; set; }
        public RelayCommand<SnackbarPresenter> LoadCommand { get; set; }

        public List<string> ForgeVersions
        {
            get => forgeVersions;
            set => Set(ref forgeVersions, value);
        }
        public List<string> FabricVersions
        {
            get => fabricVersions;
            set => Set(ref fabricVersions, value);
        }
        public List<string> OptfineVersions
        {
            get => optfineVersions;      
            set => Set(ref optfineVersions, value);
        }
        public List<string> QuiltVersions
        {
            get => quiltVersions;
            set => Set(ref quiltVersions, value);
        }
        public string McVersion
        {
            get => mcVersion;
            set => Set(ref mcVersion, value);
        }
        public string SelectForgeVersion
        {
            get => selectForgeVersion;
            set => Set(ref selectForgeVersion, value);
        }
        public string SelectFabricVersion
        {
            get => selectFabricVersion;
            set => Set(ref selectFabricVersion, value);
        }
        public string SelectOptifineVersion
        {
            get => selectOptifineVersion;
            set => Set(ref selectOptifineVersion, value);
        }
        public string SelectQuiltVersion
        {
            get => selectQuiltVersion;
            set => Set(ref selectQuiltVersion, value);
        }
        public int InstallProgress
        {
            get => installProgress;
            set => Set(ref installProgress, value);
        }
        public string InstallStep
        {
            get => installStep;
            set => Set(ref installStep, value);
        }
        public string Speed
        {
            get => speed;
            set => Set(ref speed, value);
        }
        public InstallPageViewModel(string McVersion)
        {
            this.mcVersion = McVersion;
            InstallCommand = new RelayCommand(Install);
            LoadCommand = new(Load);
        }

        private void Load(SnackbarPresenter snackbarPresenter)
        {
            this.snackbarService.SetSnackbarPresenter(snackbarPresenter);
        }


        private async void Install()
        {
            CompositeInstaller installer;
            List <IInstallEntry> installEntries = [];
            await Task.Run(() =>
            {

                installEntries.Add(VanillaInstaller.EnumerableMinecraftAsync().Result.First(x => x.McVersion == mcVersion));
                if (!string.IsNullOrEmpty(selectForgeVersion))
                {
                    installEntries.Add(ForgeInstaller.EnumerableForgeAsync(McVersion).Result.First(x => x.DisplayVersion == SelectForgeVersion));
                }
                if (!string.IsNullOrEmpty(selectFabricVersion))
                {
                    installEntries.Add(FabricInstaller.EnumerableFabricAsync(McVersion).Result.First(x => x.DisplayVersion == SelectFabricVersion));
                }
                if (!string.IsNullOrEmpty(selectOptifineVersion))
                {
                    installEntries.Add(OptifineInstaller.EnumerableOptifineAsync(McVersion).Result.First(x => x.DisplayVersion == SelectOptifineVersion));
                }
                if (!string.IsNullOrEmpty(selectQuiltVersion))
                {
                    installEntries.Add(QuiltInstaller.EnumerableQuiltAsync(McVersion).Result.First(x => x.DisplayVersion == SelectQuiltVersion));
                }

            });

            installer = CompositeInstaller.Create(installEntries, mcFolder);
            


            await Task.Run(async () =>
            {
                try
                {
                    installer.ProgressChanged += (_, arg) =>
                    {
                        InstallStep = $"{arg.FinishedStepTaskCount}/{arg.TotalStepTaskCount} ";
                        InstallProgress = (int)(arg.Progress);
                        Speed = (arg.IsStepSupportSpeed ? $"{arg.Speed / 1024 / 1024}" : "N/A");
                    }; 
                    var minecraft = await installer.InstallAsync();
                    App.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        snackbarService.Show("安装完成", $"安装完成: {minecraft.Id}", ControlAppearance.Success, null, snackbarService.DefaultTimeOut);
                    }));
                }
                catch (Exception ex)
                {
                    App.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        snackbarService.Show("错误4", $"错误信息{ex.Message}", ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);
                    }));
                }
                    
                


               
            });
           // VanillaInstaller installer = VanillaInstaller.Create("./.minecraft",( await VanillaInstaller.EnumerableMinecraftAsync()).First(x => x.McVersion == McVersion));
           // installer.ProgressChanged += (_, arg) =>
           //{
           //    InstallStep = $"{arg.FinishedStepTaskCount}/{arg.TotalStepTaskCount} ";
           //    InstallProgress = (int)(arg.Progress * 100);
           //    Speed = (arg.IsStepSupportSpeed ? $"{arg.Speed}" : "N/A");
           //};
           // await Task.Run(async () =>
           // {

           //     var minecraft = await installer.InstallAsync();
           //     App.Current.Dispatcher.Invoke((Action)(() =>
           //     {
           //         snackbarService.Show("安装完成", $"安装完成: {minecraft.Id}", ControlAppearance.Success, null, snackbarService.DefaultTimeOut);
           //     }));
           // });
        }
    }
}
