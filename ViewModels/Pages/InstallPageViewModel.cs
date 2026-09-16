using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using MinecraftLaunch.Base.Interfaces;
using MinecraftLaunch.Base.Models.Game;
using MinecraftLaunch.Base.Models.Network;
using MinecraftLaunch.Components.Installer;
using MinecraftLaunch.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using VibrantbitLauncher.Models;
using VibrantbitLauncher.ViewModels.Windows;
using VibrantbitLauncher.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace VibrantbitLauncher.ViewModels.Pages
{
    public class InstallPageViewModel : ViewModelBase
    {
        private string mcVersion = string.Empty;
        private string mcFolder = "./.minecraft";
        private List<string> forgeVersions = new();
        private List<string> fabricVersions = new();
        private List<string> optfineVersions = new();
        private List<string> quiltVersions = new();
        private string selectForgeVersion = string.Empty;
        private string selectFabricVersion = string.Empty;
        private string selectOptifineVersion = string.Empty;
        private string selectQuiltVersion = string.Empty;
        private int installProgress = 0;
        private string installStep = string.Empty;
        private string speed;
        private SnackbarService snackbarService = new();
        private bool isLoading = false;

        public RelayCommand InstallCommand { get; set; }
        public RelayCommand RefreshCommand { get; set; }
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
        public bool IsLoading
        {
            get => isLoading;
            set
            {
                Set(ref isLoading, value);
                InstallCommand?.RaiseCanExecuteChanged();
            }
        }
        public bool CanInstall => !IsLoading;

        public InstallPageViewModel(string McVersion)
        {
            this.mcVersion = McVersion;
            InstallCommand = new RelayCommand(async () => await InstallAsync(), () => CanInstall);
            RefreshCommand = new RelayCommand(async () => await LoadVersionsAsync());
            LoadCommand = new RelayCommand<SnackbarPresenter>(async (presenter) => await Load(presenter));
        }

        private async Task Load(SnackbarPresenter snackbarPresenter)
        {
            this.snackbarService.SetSnackbarPresenter(snackbarPresenter);
            await LoadVersionsAsync();
        }

        public async Task LoadVersionsAsync()
        {
            IsLoading = true;
            try
            {
                ForgeVersions = new List<string>();
                FabricVersions = new List<string>();
                OptfineVersions = new List<string>();
                QuiltVersions = new List<string>();

                // 并行加载所有加载器版本，减少总等待时间
                var forgeTask = Task.Run(async () => (await ForgeInstaller.EnumerableForgeAsync(mcVersion)).Select(x => x.DisplayVersion).ToList());
                var fabricTask = Task.Run(async () => (await FabricInstaller.EnumerableFabricAsync(mcVersion)).Select(x => x.DisplayVersion).ToList());
                var optifineTask = Task.Run(async () => (await OptifineInstaller.EnumerableOptifineAsync(mcVersion)).Select(x => x.DisplayVersion).ToList());
                var quiltTask = Task.Run(async () => (await QuiltInstaller.EnumerableQuiltAsync(mcVersion)).Select(x => x.DisplayVersion).ToList());

                await Task.WhenAll(forgeTask, fabricTask, optifineTask, quiltTask);

                ForgeVersions = forgeTask.Result;
                FabricVersions = fabricTask.Result;
                OptfineVersions = optifineTask.Result;
                QuiltVersions = quiltTask.Result;
            }
            catch (Exception ex)
            {
                snackbarService.Show("错误", $"加载模组/安装列表失败: {ex.Message}", ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task InstallAsync()
        {
            // 异步枚举 Java，不阻塞 UI 线程
            var javaList = await JavaUtil.EnumerableJavaAsync().ToListAsync();
            var asyncJavas = javaList.ToList();

            IsLoading = true;
            string CustomId = "";
            var installEntries = new List<IInstallEntry>();
            try
            {
                var vanillas = await VanillaInstaller.EnumerableMinecraftAsync();
                var vanilla = vanillas.FirstOrDefault(x => x.McVersion == mcVersion);
                CustomId += McVersion;
                if (vanilla == null)
                {
                    snackbarService.Show("错误", $"未找到原版版本: {McVersion}", ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);
                    return;
                }
                installEntries.Add(vanilla);

                if (!string.IsNullOrEmpty(SelectForgeVersion))
                {
                    var forges = await ForgeInstaller.EnumerableForgeAsync(McVersion);
                    var forge = forges.FirstOrDefault(x => string.Equals(x.DisplayVersion, SelectForgeVersion, StringComparison.OrdinalIgnoreCase));
                    CustomId += $"-Forge_{SelectForgeVersion}";
                    if (forge == null)
                    {
                        snackbarService.Show("错误", $"未找到 Forge 版本: {SelectForgeVersion}", ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);
                        return;
                    }
                    installEntries.Add(forge);
                }

                if (!string.IsNullOrEmpty(SelectFabricVersion))
                {
                    var fabrics = await FabricInstaller.EnumerableFabricAsync(McVersion);
                    var fabric = fabrics.FirstOrDefault(x => string.Equals(x.DisplayVersion, SelectFabricVersion, StringComparison.OrdinalIgnoreCase));
                    CustomId += $"-Fabric_{SelectFabricVersion}";
                    if (fabric == null)
                    {
                        snackbarService.Show("错误", $"未找到 Fabric 版本: {SelectFabricVersion}", ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);
                        return;
                    }
                    installEntries.Add(fabric);
                }

                if (!string.IsNullOrEmpty(SelectOptifineVersion))
                {
                    var optfines = await OptifineInstaller.EnumerableOptifineAsync(McVersion);
                    var opt = optfines.FirstOrDefault(x => string.Equals(x.DisplayVersion, SelectOptifineVersion, StringComparison.OrdinalIgnoreCase));
                    CustomId += $"-Optifine_{SelectOptifineVersion}";
                    if (opt == null)
                    {
                        snackbarService.Show("错误", $"未找到 Optifine 版本: {SelectOptifineVersion}", ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);
                        return;
                    }
                    installEntries.Add(opt);
                }

                if (!string.IsNullOrEmpty(SelectQuiltVersion))
                {
                    var quilts = await QuiltInstaller.EnumerableQuiltAsync(McVersion);
                    var quilt = quilts.FirstOrDefault(x => string.Equals(x.DisplayVersion, SelectQuiltVersion, StringComparison.OrdinalIgnoreCase));
                    CustomId += $"-Quilt_{SelectQuiltVersion}";
                    if (quilt == null)
                    {
                        snackbarService.Show("错误", $"未找到 Quilt 版本: {SelectQuiltVersion}", ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);
                        return;
                    }
                    installEntries.Add(quilt);
                }
            }
            catch (Exception ex)
            {
                snackbarService.Show("错误", $"获取安装项失败: {ex.Message}", ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);
                return;
            }
            finally
            {
                IsLoading = false;
            }

            snackbarService.Show("提示", $"正在安装版本{McVersion},Forge:{SelectForgeVersion},Fabric:{SelectFabricVersion},Optifine{SelectOptifineVersion},Quilt:{SelectQuiltVersion}", ControlAppearance.Info, null, snackbarService.DefaultTimeOut);

            var installer = CompositeInstaller.Create(installEntries, mcFolder, javaPath: asyncJavas.FirstOrDefault().JavaPath, customId: CustomId);
            installer.ProgressChanged += (_, arg) =>
            {
                InstallStep = $"{arg.FinishedStepTaskCount}/{arg.TotalStepTaskCount} ";
                InstallProgress = (int)(arg.Progress * 100);
                Speed = (arg.IsStepSupportSpeed ? $"{arg.Speed / 1024 / 1024}" : "N/A");
            };

            try
            {
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
                    snackbarService.Show("错误", $"错误信息: {ex.Message}", ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);
                }));
            }
            finally
            {
                IsLoading = false;
                InstallCommand?.RaiseCanExecuteChanged();
            }
        }
    }
}
