using Microsoft.Win32;
using MinecraftLaunch.Base.Models.Network;
using MinecraftLaunch.Components.Installer;
using MinecraftLaunch.Components.Provider;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using UiMessageBox = Wpf.Ui.Controls.MessageBox;

namespace VibrantbitLauncher.Views.Pages
{
    public partial class ModPage : Page
    {
        private string ProjectId = string.Empty;
        private ObservableCollection<ModrinthModInfo> modrinthModInfos = new();
        private List<ModrinthResourceFile> modrinthResources = new();
        private readonly ModrinthProvider provider = new();
        private bool isDownloading = false;

        public ModPage()
        {
            InitializeComponent();
            comboBox.SelectionChanged += OnSelectionChanged;
        }

        private static string GetGamePath()
        {
            var path = Path.GetFullPath("./.minecraft");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        public async Task LoadForProject(string projectId)
        {
            if (string.IsNullOrWhiteSpace(projectId))
                return;
            ProjectId = projectId;
            await LoadFileInfos(projectId);
        }

        private async Task LoadFileInfos(string projectId)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                loadingText.Visibility = Visibility.Visible;
                listBox.ItemsSource = null;
            });

            try
            {
                modrinthModInfos.Clear();
                modrinthResources.Clear();

                var modInfos = await provider.GetModFilesByProjectIdAsync(projectId);
                if (modInfos == null)
                {
                    await Dispatcher.InvokeAsync(async () =>
                    {
                        loadingText.Visibility = Visibility.Collapsed;
                        listBox.ItemsSource = null;
                        await new UiMessageBox { Title = "提示", Content = "未找到模组文件" }.ShowDialogAsync();
                    });
                    return;
                }

                modrinthResources = modInfos.ToList();
                foreach (var modInfo in modInfos)
                {
                    modrinthModInfos.Add(new ModrinthModInfo
                    {
                        Name = modInfo.DisplayName,
                        DownloadUrl = modInfo.DownloadUrl
                    });
                }

                if (modrinthModInfos.Count == 0)
                {
                    await Dispatcher.InvokeAsync(async () =>
                    {
                        loadingText.Visibility = Visibility.Collapsed;
                        listBox.ItemsSource = null;
                        await new UiMessageBox { Title = "提示", Content = "该项目暂无可用的模组文件" }.ShowDialogAsync();
                    });
                    return;
                }

                var extensions = await VanillaInstaller.EnumerableMinecraftAsync();

                await Dispatcher.InvokeAsync(() =>
                {
                    loadingText.Visibility = Visibility.Collapsed;
                    comboBox.Items.Clear();
                    comboBox.Items.Add(string.Empty);
                    foreach (var ext in extensions)
                        comboBox.Items.Add(ext.McVersion);
                    listBox.ItemsSource = modrinthModInfos;
                    comboBox.SelectedIndex = 0;
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    loadingText.Visibility = Visibility.Collapsed;
                    await new UiMessageBox { Title = "错误", Content = $"加载模组文件列表失败: {ex.Message}" }.ShowDialogAsync();
                });
            }
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = comboBox.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(selected))
            {
                listBox.ItemsSource = modrinthModInfos;
                return;
            }

            var filtered = modrinthResources
                .Where(m => !string.IsNullOrEmpty(m.FileName) &&
                            m.FileName.Contains(selected, StringComparison.OrdinalIgnoreCase))
                .Select(m => new ModrinthModInfo { Name = m.DisplayName, DownloadUrl = m.DownloadUrl })
                .ToList();
            listBox.ItemsSource = filtered;
        }

        private void OnSaveAsClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ModrinthModInfo info)
            {
                DownloadMod(info.DownloadUrl);
            }
            else
            {
                _ = new UiMessageBox { Title = "错误", Content = "无法获取文件下载链接" }.ShowDialogAsync();
            }
        }

        private async void DownloadMod(string downloadUrl)
        {
            if (string.IsNullOrWhiteSpace(downloadUrl))
            {
                await new UiMessageBox { Title = "提示", Content = "该文件没有有效的下载链接" }.ShowDialogAsync();
                return;
            }

            if (isDownloading)
            {
                await new UiMessageBox { Title = "提示", Content = "已有下载任务正在进行，请稍候" }.ShowDialogAsync();
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                FileName = Path.GetFileName(downloadUrl),
                InitialDirectory = GetGamePath(),
                Filter = "模组文件 (*.jar)|*.jar|压缩包 (*.zip)|*.zip|所有文件 (*.*)|*.*",
                FilterIndex = 1
            };

            if (saveFileDialog.ShowDialog() != true)
                return;

            isDownloading = true;

            try
            {
                using var httpClient = new HttpClient();
                using var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = File.Create(saveFileDialog.FileName);
                await contentStream.CopyToAsync(fileStream);

                await new UiMessageBox { Title = "下载完成", Content = $"文件已保存到：\n{saveFileDialog.FileName}" }.ShowDialogAsync();
            }
            catch (Exception ex)
            {
                await new UiMessageBox { Title = "下载失败", Content = ex.Message }.ShowDialogAsync();
            }
            finally
            {
                isDownloading = false;
            }
        }

        private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = comboBox1.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(selected))
            {
                listBox.ItemsSource = modrinthModInfos;
                return;
            }

            var filtered = modrinthResources
                .Where(m => !string.IsNullOrEmpty(m.FileName) &&
                            m.FileName.Contains(selected, StringComparison.OrdinalIgnoreCase))
                .Select(m => new ModrinthModInfo { Name = m.DisplayName, DownloadUrl = m.DownloadUrl })
                .ToList();
            listBox.ItemsSource = filtered;
        }
    }

    public class ModrinthModInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
    }
}
