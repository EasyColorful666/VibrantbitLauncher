using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using MinecraftLaunch.Base.Enums;
using MinecraftLaunch.Components.Installer;
using MinecraftLaunch.Components.Provider;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using UiMessageBox = Wpf.Ui.Controls.MessageBox;

namespace VibrantbitLauncher.ViewModels.Pages
{
    public class DownloadResourcesViewModel : ViewModelBase
    {
        private readonly ModrinthProvider provider = new();
        private ObservableCollection<ModrinthMod> modrinthMods = new();

        // 筛选
        private string _selectedVersion = "全部";
        private string _selectedLoader = "全部";
        private string _lastSearchText = string.Empty;
        private bool _isInitialized = false;

        public ObservableCollection<ModrinthMod> ModrinthMods
        {
            get => modrinthMods;
            set => Set(ref modrinthMods, value);
        }

        /// <summary>可选 Minecraft 版本（异步从 VanillaInstaller 加载）。</summary>
        public ObservableCollection<string> MinecraftVersions { get; } = new();

        /// <summary>可选模组加载器。</summary>
        public ObservableCollection<string> Loaders { get; } = new()
        {
            "全部", "fabric", "forge", "quilt", "neoforge"
        };

        public string SelectedVersion
        {
            get => _selectedVersion;
            set
            {
                if (Set(ref _selectedVersion, value) && _isInitialized)
                    _ = ApplyFilterAsync();
            }
        }

        public string SelectedLoader
        {
            get => _selectedLoader;
            set
            {
                if (Set(ref _selectedLoader, value) && _isInitialized)
                    _ = ApplyFilterAsync();
            }
        }

        public RelayCommand<string> SerachCommand { get; }

        public DownloadResourcesViewModel()
        {
            SerachCommand = new RelayCommand<string>(async searchText =>
            {
                _lastSearchText = searchText;
                await ApplyFilterAsync();
            });

            _ = InitializeAsync();
        }

        /// <summary>
        /// 异步初始化：后台获取 Minecraft 版本列表，然后加载精选模组。
        /// </summary>
        private async Task InitializeAsync()
        {
            try
            {
                MinecraftVersions.Add("全部");

                var versions = await Task.Run(async () =>
                {
                    var entries = await VanillaInstaller.EnumerableMinecraftAsync();
                    return entries.Select(v => v.Id).ToList();
                });

                foreach (var v in versions)
                    MinecraftVersions.Add(v);
            }
            catch
            {
                // 版本加载失败不影响主功能
            }
            finally
            {
                _isInitialized = true;
            }

            await LoadResourcesAsync();
        }

        /// <summary>
        /// 根据当前搜索词和筛选条件加载模组列表。
        /// </summary>
        private async Task ApplyFilterAsync()
        {
            if (!_isInitialized)
                return;

            ModrinthMods.Clear();

            try
            {
                // 无筛选：精选或普通搜索
                if (SelectedVersion == "全部" && SelectedLoader == "全部")
                {
                    if (string.IsNullOrWhiteSpace(_lastSearchText))
                    {
                        await LoadResourcesAsync();
                    }
                    else
                    {
                        var results = await provider.SearchAsync(_lastSearchText);
                        AddMods(results);
                    }
                    return;
                }

                // 有筛选：用 MinecraftLaunch 官方 SearchAsync 重载
                var version = SelectedVersion == "全部" ? null : SelectedVersion;
                var modLoader = SelectedLoader == "全部" ? (ModLoaderType?)null : ParseModLoader(SelectedLoader);

                IEnumerable<dynamic> results2;
                if (modLoader.HasValue)
                {
                    results2 = await provider.SearchAsync(
                        searchFilter: _lastSearchText ?? string.Empty,
                        version: version,
                        modLoader: modLoader.Value);
                }
                else
                {
                    results2 = await provider.SearchAsync(
                        searchFilter: _lastSearchText ?? string.Empty,
                        version: version);
                }

                AddMods(results2);
            }
            catch (Exception ex)
            {
                await new UiMessageBox { Title = "错误", Content = $"筛选模组失败：{ex.Message}" }.ShowDialogAsync();
            }
        }

        /// <summary>
        /// 将加载器字符串转换为 ModLoaderType 枚举。
        /// </summary>
        private static ModLoaderType ParseModLoader(string loader)
        {
            return loader.ToLowerInvariant() switch
            {
                "fabric" => ModLoaderType.Fabric,
                "forge" => ModLoaderType.Forge,
                "quilt" => ModLoaderType.Quilt,
                "neoforge" => ModLoaderType.NeoForge,
                _ => ModLoaderType.Any,
            };
        }

        /// <summary>
        /// 将搜索结果转换为 ModrinthMod 列表。
        /// </summary>
        private void AddMods(IEnumerable mods)
        {
            foreach (dynamic mod in mods)
            {
                modrinthMods.Add(new ModrinthMod
                {
                    Name = mod.Name ?? "",
                    ImagePath = mod.IconUrl ?? "",
                    MinecraftVersions = (mod.MinecraftVersions as List<string>)?.LastOrDefault() + "+",
                    ProjectId = mod.ProjectId ?? ""
                });
            }
        }

        private async Task LoadResourcesAsync()
        {
            try
            {
                var resourceslist = await provider.GetFeaturedResourcesAsync();
                foreach (var mod in resourceslist)
                {
                    modrinthMods.Add(new ModrinthMod
                    {
                        Name = mod.Name,
                        ImagePath = mod.IconUrl,
                        MinecraftVersions = mod.MinecraftVersions.FirstOrDefault() + "+",
                        ProjectId = mod.ProjectId
                    });
                }
            }
            catch (Exception ex)
            {
                await new UiMessageBox { Title = "错误", Content = $"加载精选模组失败：{ex.Message}" }.ShowDialogAsync();
            }
        }
    }

    public class ModrinthMod
    {
        public string Name { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public string MinecraftVersions { get; set; } = string.Empty;
        public string ProjectId { get; set; } = string.Empty;
    }
}
