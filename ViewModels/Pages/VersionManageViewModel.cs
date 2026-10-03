using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using MinecraftLaunch.Base.Models.Game;
using MinecraftLaunch.Utilities;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace VibrantbitLauncher.ViewModels.Pages
{
    public partial class VersionManageViewModel : ViewModelBase
    {
        private SnackbarService snackbarService = new();
        private MinecraftParser minecraftParser;
        private string currentVersion;
        private string modsFolder;
        private string savesFolder;
        private bool showMods = true;

        public ObservableCollection<ModItem> Mods { get; } = new();
        public ObservableCollection<SaveItem> Saves { get; } = new();

        public string CurrentVersion
        {
            get => currentVersion;
            set => Set(ref currentVersion, value);
        }

        public bool ShowMods
        {
            get => showMods;
            set => Set(ref showMods, value);
        }

        public RelayCommand<SnackbarPresenter> LoadCommand { get; set; }
        public RelayCommand RefreshCommand { get; set; }
        public RelayCommand GoBackCommand { get; set; }
        public RelayCommand<string> OpenModFolderCommand { get; set; }
        public RelayCommand<ModItem> DeleteModCommand { get; set; }
        public RelayCommand<string> OpenSaveFolderCommand { get; set; }
        public RelayCommand<SaveItem> DeleteSaveCommand { get; set; }
        public RelayCommand OpenGameFolderCommand { get; set; }

        public VersionManageViewModel()
        {
            LoadCommand = new RelayCommand<SnackbarPresenter>(Load);
            RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
            GoBackCommand = new RelayCommand(GoBack);
            OpenModFolderCommand = new RelayCommand<string>(OpenFolder);
            DeleteModCommand = new RelayCommand<ModItem>(DeleteMod);
            OpenSaveFolderCommand = new RelayCommand<string>(OpenFolder);
            DeleteSaveCommand = new RelayCommand<SaveItem>(DeleteSave);
            OpenGameFolderCommand = new RelayCommand(OpenGameFolder);

            Messenger.Default.Register<string>(this, "McVersionForManage", OnVersionReceived);
        }

        private void OnVersionReceived(string version)
        {
            CurrentVersion = version;
            _ = LoadDataAsync();
        }

        private void Load(SnackbarPresenter presenter)
        {
            snackbarService.SetSnackbarPresenter(presenter);
        }

        private async Task LoadDataAsync()
        {
            if (string.IsNullOrEmpty(CurrentVersion))
                return;

            try
            {
                minecraftParser ??= new MinecraftParser(".\\.minecraft");
                var entry = minecraftParser.GetMinecraft(CurrentVersion);

                modsFolder = TryGetFolderPath(entry, "ModsFolder", "mods");
                savesFolder = TryGetFolderPath(entry, "SavesFolder", "saves");

                Directory.CreateDirectory(modsFolder);
                Directory.CreateDirectory(savesFolder);

                await LoadModsAsync();
                await LoadSavesAsync();
            }
            catch (Exception ex)
            {
                snackbarService.Show("错误", $"加载版本管理失败: {ex.Message}", ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);
            }
        }

        private static string TryGetFolderPath(MinecraftEntry entry, string propertyName, string subFolder)
        {
            try
            {
                var prop = entry.GetType().GetProperty(propertyName);
                if (prop != null)
                {
                    var path = prop.GetValue(entry) as string;
                    if (!string.IsNullOrEmpty(path))
                        return path;
                }
            }
            catch { }

            var versionDir = Path.Combine(".\\.minecraft", "versions", entry.Id);
            if (Directory.Exists(versionDir))
                return Path.Combine(versionDir, subFolder);
            return Path.Combine(".\\.minecraft", subFolder);
        }

        private async Task LoadModsAsync()
        {
            await Task.Run(() =>
            {
                var files = Directory.GetFiles(modsFolder, "*.jar*", SearchOption.TopDirectoryOnly);
                var items = files.Select(f =>
                {
                    var fi = new FileInfo(f);
                    var isDisabled = f.EndsWith(".jar.disabled", StringComparison.OrdinalIgnoreCase);
                    return new ModItem
                    {
                        FileName = isDisabled
                            ? Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(f))
                            : Path.GetFileNameWithoutExtension(f),
                        FullPath = f,
                        FolderPath = modsFolder,
                        IsEnabled = !isDisabled,
                        FileSize = FormatSize(fi.Length)
                    };
                }).ToList();

                App.Current.Dispatcher.Invoke(() =>
                {
                    Mods.Clear();
                    foreach (var item in items)
                        Mods.Add(item);
                });
            });
        }

        private async Task LoadSavesAsync()
        {
            await Task.Run(() =>
            {
                if (!Directory.Exists(savesFolder)) return;
                var dirs = Directory.GetDirectories(savesFolder);
                var items = dirs.Select(d =>
                {
                    var di = new DirectoryInfo(d);
                    return new SaveItem
                    {
                        FolderName = di.Name,
                        FolderPath = d,
                        LastPlayed = di.LastWriteTime.ToString("yyyy-MM-dd HH:mm")
                    };
                }).OrderByDescending(x => x.LastPlayed).ToList();

                App.Current.Dispatcher.Invoke(() =>
                {
                    Saves.Clear();
                    foreach (var item in items)
                        Saves.Add(item);
                });
            });
        }

        private void GoBack()
        {
            Messenger.Default.Send(Type.GetType("VibrantbitLauncher.Views.Pages.RunPage"), "NavigateTo");
        }

        private static void OpenFolder(string path)
        {
            if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
        }

        private void OpenGameFolder()
        {
            OpenFolder(".\\.minecraft");
        }

        private void DeleteMod(ModItem mod)
        {
            var result = MessageBox.Show(
                $"确定要删除模组 \"{mod.FileName}\" 吗？此操作不可撤销。",
                "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                if (File.Exists(mod.FullPath))
                    File.Delete(mod.FullPath);
                Mods.Remove(mod);
                snackbarService.Show("成功", $"已删除模组 {mod.FileName}", ControlAppearance.Success, null, snackbarService.DefaultTimeOut);
            }
            catch (Exception ex)
            {
                snackbarService.Show("错误", $"删除失败: {ex.Message}", ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);
            }
        }

        private void DeleteSave(SaveItem save)
        {
            var result = MessageBox.Show(
                $"确定要删除存档 \"{save.FolderName}\" 吗？此操作不可撤销。",
                "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                if (Directory.Exists(save.FolderPath))
                    Directory.Delete(save.FolderPath, true);
                Saves.Remove(save);
                snackbarService.Show("成功", $"已删除存档 {save.FolderName}", ControlAppearance.Success, null, snackbarService.DefaultTimeOut);
            }
            catch (Exception ex)
            {
                snackbarService.Show("错误", $"删除失败: {ex.Message}", ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);
            }
        }

        private static string FormatSize(long bytes)
        {
            if (bytes >= 1024 * 1024) return $"{bytes / 1024.0 / 1024.0:F1} MB";
            if (bytes >= 1024) return $"{bytes / 1024.0:F1} KB";
            return $"{bytes} B";
        }

        public void ToggleMod(ModItem mod)
        {
            try
            {
                if (mod.IsEnabled)
                {
                    var newPath = mod.FullPath + ".disabled";
                    File.Move(mod.FullPath, newPath);
                    mod.FullPath = newPath;
                }
                else
                {
                    var newPath = mod.FullPath.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase)
                        ? mod.FullPath[..^9]
                        : Path.ChangeExtension(mod.FullPath, ".jar");
                    File.Move(mod.FullPath, newPath);
                    mod.FullPath = newPath;
                }
            }
            catch (Exception ex)
            {
                snackbarService.Show("错误", $"切换模组状态失败: {ex.Message}", ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);
            }
        }
    }

    public class ModItem : ViewModelBase
    {
        private bool isEnabled;
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public string FolderPath { get; set; }
        public string FileSize { get; set; }
        public bool IsEnabled
        {
            get => isEnabled;
            set => Set(ref isEnabled, value);
        }
    }

    public class SaveItem
    {
        public string FolderName { get; set; }
        public string FolderPath { get; set; }
        public string LastPlayed { get; set; }
    }
}
