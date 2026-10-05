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
            var path = Path.GetFullPath(VibrantbitLauncher.Services.SettingsService.ResolveMinecraftFolder());
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

                var extensions = await VibrantbitLauncher.Services.MinecraftVersionCache.GetAsync();

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

        /// <summary>
        /// 从下载链接取扩展名（小写、带点）。
        ///
        /// 链接可能带查询串或 CDN 的路径后缀（如 `...sodium.jar?fabric-1.21.4.jar`），
        /// 因此先去查询串和片段，再对路径取扩展名。识别不出来的（版本号路径如 `/download/1.21.4`、
        /// 纯数字后缀 `.4`、无扩展名）一律按 `.jar` 处理 —— Modrinth 上模组/整合包/数据包/资源包/光影
        /// 绝大多数是 `.jar` 或 `.zip`，其中 `.jar` 占绝对多数。
        /// </summary>
        private static string GetDownloadExtension(string url)
        {
            try
            {
                var path = url;

                // 去掉查询串与片段
                var cut = path.IndexOfAny(new[] { '?', '#' });
                if (cut >= 0)
                    path = path[..cut];

                var extension = Path.GetExtension(path);

                // 必须是「. + 字母开头的纯字母数字」，且至少两个字符（如 .jar / .zip / .mcpack）。
                // 这样可以挡掉把版本号当扩展名的情况（`/download/1.21.4` 的 `.4`），
                // 也能挡住超长的路径段落（`weird.superlongextension` 是名字不是扩展名）。
                if (string.IsNullOrWhiteSpace(extension) || extension.Length < 3)
                    return ".jar";

                var name = extension[1..];
                if (!char.IsLetter(name[0]) || !name.All(char.IsLetterOrDigit))
                    return ".jar";

                // 常见扩展名最长 6 个字符（mcpack / mcworld）；更长的当路径段处理
                return name.Length <= 6 ? extension.ToLowerInvariant() : ".jar";
            }
            catch
            {
                return ".jar";
            }
        }

        /// <summary>
        /// 按扩展名生成**唯一一项**的保存过滤器 —— 用户没有其他可选类型。
        /// 找不到友好名称的扩展名就用「文件 (*.xxx)」兜底，仍然只给一项。
        /// </summary>
        private static string BuildFilter(string extension) => extension switch
        {
            ".jar" => "模组文件 (*.jar)|*.jar",
            ".zip" => "压缩包 (*.zip)|*.zip",
            ".litemod" => "LiteLoader 模组 (*.litemod)|*.litemod",
            ".txt" => "文本文件 (*.txt)|*.txt",
            ".json" => "JSON 文件 (*.json)|*.json",
            ".yml" or ".yaml" => "YAML 文件 (*.yml;*.yaml)|*.yml;*.yaml",
            ".mcpack" => "基岩版资源包 (*.mcpack)|*.mcpack",
            ".mcworld" => "基岩版世界 (*.mcworld)|*.mcworld",
            _ => $"文件 (*{extension})|*{extension}"
        };

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

            // 文件类型由下载链接的扩展名决定，不给用户改：
            // 选错类型会存出一个扩展名与实际内容不符的文件（如 .jar 存成 .zip），
            // 游戏加载时识别不了。这里把过滤器锁死成唯一一项。
            var extension = GetDownloadExtension(downloadUrl);
            var saveFileDialog = new SaveFileDialog
            {
                FileName = Path.GetFileName(downloadUrl),
                InitialDirectory = GetGamePath(),
                Filter = BuildFilter(extension),
                FilterIndex = 1,
                // 只允许保存为探测到的类型：用户改扩展名会被 Windows 追加回原扩展名
                AddExtension = true,
                DefaultExt = extension,
                ValidateNames = true,
                // 关掉「所有文件」这类兜底项后，CheckFileExists 之外无需额外校验
                CheckPathExists = true
            };

            if (saveFileDialog.ShowDialog() != true)
                return;

            isDownloading = true;

            var taskService = App.Services.GetService(typeof(Services.DownloadTaskService)) as Services.DownloadTaskService;
            var taskName = Path.GetFileName(saveFileDialog.FileName);
            var task = taskService?.CreateTask(taskName, Models.DownloadTaskType.ModDownload);

            try
            {
                using var httpClient = new HttpClient();
                using var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = File.Create(saveFileDialog.FileName);

                var buffer = new byte[81920];
                long totalRead = 0;
                long lastReportedBytes = 0;
                int read;
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, read);
                    totalRead += read;

                    // 每 200ms 报告一次进度，避免高频刷新 UI
                    if (totalBytes > 0 && sw.ElapsedMilliseconds >= 200)
                    {
                        var percent = (int)(totalRead * 100 / totalBytes);
                        var intervalBytes = totalRead - lastReportedBytes;
                        var speed = intervalBytes / sw.Elapsed.TotalSeconds / 1024 / 1024;
                        taskService?.UpdateProgress(task, percent, $"{speed:F1} MB/s",
                            $"{totalRead / 1024 / 1024:F1}/{totalBytes / 1024 / 1024:F1} MB");
                        lastReportedBytes = totalRead;
                        sw.Restart();
                    }
                }

                taskService?.CompleteTask(task, $"已保存到：{saveFileDialog.FileName}");
                await new UiMessageBox { Title = "下载完成", Content = $"文件已保存到：\n{saveFileDialog.FileName}" }.ShowDialogAsync();
            }
            catch (Exception ex)
            {
                taskService?.FailTask(task, ex.Message);
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
