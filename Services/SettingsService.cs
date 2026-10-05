using CommunityToolkit.Mvvm.Messaging;
using MinecraftLaunch.Base.Models.Authentication;
using MinecraftLaunch.Base.Models.Authentication.Yggdrasil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;
using VibrantbitLauncher.Models;
using VibrantbitLauncher.ViewModels.Windows;
using Wpf.Ui.Appearance;

namespace VibrantbitLauncher.Services
{
    /// <summary>
    /// 应用程序设置数据模型，序列化为 JSON 配置文件。
    /// 账户直接使用 MinecraftLaunch 的类型，不做二次封装。
    /// </summary>
    public class AppSettings
    {
        public string Theme { get; set; } = "Light";
        public string AccentColor { get; set; } = "#FF0078D4";
        /// <summary>主题渐变色的终点色（UseAccentGradient 为 true 时生效）。</summary>
        public string AccentColorEnd { get; set; } = "#FF00B7C3";
        /// <summary>是否启用「主题渐变色」。关闭时全程使用 AccentColor 单色。</summary>
        public bool UseAccentGradient { get; set; }
        /// <summary>背景图路径：本地绝对路径，或内置壁纸的 pack URI（/Assets/bg1.png）；空表示不使用背景图。</summary>
        public string BackgroundImagePath { get; set; } = string.Empty;
        /// <summary>背景图显示不透明度。</summary>
        public double BackgroundImageOpacity { get; set; } = 0.85;
        /// <summary>日志最低记录等级：Verbose / Debug / Information / Warning / Error / Fatal。</summary>
        public string LogLevel { get; set; } = "Information";
        /// <summary>日志文件保留天数，超期自动清理。</summary>
        public int LogRetentionDays { get; set; } = 7;
        /// <summary>是否同时把日志写到调试器输出（Visual Studio 输出窗口）。</summary>
        public bool LogToDebugOutput { get; set; } = true;
        public string MinecraftFolder { get; set; } = "./.minecraft";
        public string JavaPath { get; set; } = string.Empty;

        // ===== 联机（EasyTier） =====
        /// <summary>EasyTier 中继节点地址，默认用社区免费共享节点。</summary>
        public string EasyTierRelayServer { get; set; } = "tcp://easytier.weiai.org.cn:11010";
        /// <summary>上次房主侧使用的联机密钥（同时作为网络名与网络密码）。</summary>
        public string EasyTierNetworkKey { get; set; } = string.Empty;

        public List<MicrosoftAccount> MicrosoftAccounts { get; set; } = new();
        public List<YggdrasilAccount> YggdrasilAccounts { get; set; } = new();
        public List<OfflineAccount> OfflineAccounts { get; set; } = new();
        public string SelectedAccountUuid { get; set; } = string.Empty;
        public bool IsMicrosoftAccount { get; set; }
        public bool IsFirstRun { get; set; } = true;
    }

    /// <summary>
    /// 设置配置文件服务：负责加载、保存和应用设置。
    /// </summary>
    public static class SettingsService
    {
        private static readonly string ConfigPath =
            Path.Combine(AppContext.BaseDirectory, "settings.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() },
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static AppSettings Current { get; private set; } = new();

        /// <summary>
        /// 配置文件读写的互斥锁。
        /// Save() 会被 UI 线程（改设置）和后台线程（安装完成、令牌刷新后保存账户）同时调用，
        /// 没有保护地并发写同一个文件会写出半截 JSON —— 下次启动解析失败就回退默认值，
        /// 表现是"设置全丢、每次都弹开机向导"。
        /// </summary>
        private static readonly object ConfigLock = new();

        /// <summary>
        /// 取当前生效的 .minecraft 目录；未配置时回退到工作目录下的 ./.minecraft。
        /// 页面里不要再硬编码 "./.minecraft" 或 ".\\.minecraft"。
        /// </summary>
        public static string ResolveMinecraftFolder()
        {
            var folder = MainWindowViewModel.MainModel.MinecraftFolder;
            return string.IsNullOrWhiteSpace(folder) ? "./.minecraft" : folder;
        }
        public static void Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    // 有些编辑器 / 脚本会写入 UTF-8 BOM，JsonSerializer 不接受，这里显式去掉
                    var json = File.ReadAllText(ConfigPath).TrimStart('\uFEFF');
                    Current = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                // 以前是静默 catch：解析失败就回退默认值（IsFirstRun = true），
                // 表现是「每次启动都弹开机向导、所有设置丢失」。现在至少留下日志。
                Serilog.Log.Error(ex, "配置文件解析失败，已回退到默认设置：{Path}", ConfigPath);
                Current = new AppSettings();
            }

            MainWindowViewModel.MainModel.MinecraftFolder = Current.MinecraftFolder;
            MainWindowViewModel.MainModel.JavaPath = string.IsNullOrEmpty(Current.JavaPath) ? null : Current.JavaPath;
            MainWindowViewModel.MainModel.IsMicrosoftAccount = Current.IsMicrosoftAccount;
        }

        public static void Save()
        {
            lock (ConfigLock)
            {
                try
                {
                    Current.MinecraftFolder = MainWindowViewModel.MainModel.MinecraftFolder ?? "./.minecraft";
                    Current.JavaPath = MainWindowViewModel.MainModel.JavaPath ?? string.Empty;
                    Current.IsMicrosoftAccount = MainWindowViewModel.MainModel.IsMicrosoftAccount;

                    // 先写临时文件再原子替换：写到一半被打断也不会留下半截 JSON
                    var json = JsonSerializer.Serialize(Current, JsonOptions);
                    var tempPath = ConfigPath + ".tmp";
                    File.WriteAllText(tempPath, json);
                    File.Move(tempPath, ConfigPath, overwrite: true);
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "配置保存失败：{Path}", ConfigPath);
                }
            }
        }

        // ===== 账户保存（直接使用 MinecraftLaunch 类型） =====

        /// <summary>保存微软账户。</summary>
        public static void SaveMicrosoftAccount(MicrosoftAccount account)
        {
            var existing = Current.MicrosoftAccounts.FirstOrDefault(a => a.Uuid == account.Uuid);
            if (existing != null)
                Current.MicrosoftAccounts.Remove(existing);
            Current.MicrosoftAccounts.Add(account);
            Save();
        }

        /// <summary>保存外置（Yggdrasil）账户。</summary>
        public static void SaveYggdrasilAccount(YggdrasilAccount account)
        {
            var existing = Current.YggdrasilAccounts.FirstOrDefault(a => a.Uuid == account.Uuid);
            if (existing != null)
                Current.YggdrasilAccounts.Remove(existing);
            Current.YggdrasilAccounts.Add(account);
            Save();
        }

        /// <summary>保存离线账户。</summary>
        public static void SaveOfflineAccount(OfflineAccount account)
        {
            var existing = Current.OfflineAccounts.FirstOrDefault(a => a.Name == account.Name);
            if (existing != null)
                Current.OfflineAccounts.Remove(existing);
            Current.OfflineAccounts.Add(account);
            Save();
        }

        /// <summary>设置当前选中的账户 uuid。</summary>
        public static void SetSelectedAccount(string uuid)
        {
            Current.SelectedAccountUuid = uuid ?? string.Empty;
            Save();
        }

        // ===== 账户删除 =====

        /// <summary>删除微软账户。</summary>
        public static void RemoveMicrosoftAccount(Guid uuid)
        {
            var existing = Current.MicrosoftAccounts.FirstOrDefault(a => a.Uuid == uuid);
            if (existing != null)
                Current.MicrosoftAccounts.Remove(existing);
            Save();
        }

        /// <summary>删除外置（Yggdrasil）账户。</summary>
        public static void RemoveYggdrasilAccount(Guid uuid)
        {
            var existing = Current.YggdrasilAccounts.FirstOrDefault(a => a.Uuid == uuid);
            if (existing != null)
                Current.YggdrasilAccounts.Remove(existing);
            Save();
        }

        /// <summary>删除离线账户。</summary>
        public static void RemoveOfflineAccount(string name)
        {
            var existing = Current.OfflineAccounts.FirstOrDefault(a => a.Name == name);
            if (existing != null)
                Current.OfflineAccounts.Remove(existing);
            Save();
        }

        /// <summary>根据账户类型自动删除。</summary>
        public static void RemoveAccount(Account account)
        {
            switch (account)
            {
                case MicrosoftAccount ms:
                    RemoveMicrosoftAccount(ms.Uuid);
                    break;
                case YggdrasilAccount yg:
                    RemoveYggdrasilAccount(yg.Uuid);
                    break;
                case OfflineAccount off:
                    RemoveOfflineAccount(off.Name);
                    break;
            }
        }

        // ===== 主题 / 主题色 / 背景图 =====

        /// <summary>默认主题色（WPF-UI 蓝），配置缺失或解析失败时回退到它。</summary>
        public static readonly Color DefaultAccentColor = Color.FromRgb(0x00, 0x78, 0xD4);

        private static ApplicationTheme CurrentApplicationTheme =>
            Current.Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase)
                ? ApplicationTheme.Dark
                : ApplicationTheme.Light;

        /// <summary>一次性应用「明暗主题 + 主题色/渐变 + 背景图」，启动时调用。</summary>
        public static void ApplyAppearance()
        {
            ApplyTheme();
            ApplyBackground();
        }

        /// <summary>
        /// 刷新主题：先写入当前主题色 / 渐变，重载 WPF-UI 的主题词典，再按明暗主题重算背景遮罩，
        /// 最后修复各窗口底色。
        ///
        /// ①→② 的顺序不能反：主题词典里不少强调色画刷（例如 AccentButtonBackground ← AccentFillColorDefault）
        /// 是在「词典加载时」求值的，必须先有新的主题色、再重载词典，它们才会按新颜色重算，
        /// 否则会出现「换了主题色但按钮等控件仍是旧色」的问题。
        ///
        /// 切换明暗主题、切换主题色 / 渐变都走这里。
        /// </summary>
        public static void ApplyTheme()
        {
            var theme = CurrentApplicationTheme;

            // ① 先把主题色写进资源（SystemAccentColor / AccentFillColorDefault(Brush) / AppAccentGradientBrush …）
            ApplyAccent();

            // ② 再重载 WPF-UI 的主题词典。
            //    主题词典里很多强调色画刷（例如 AccentButtonBackground ← AccentFillColorDefault）
            //    是在「词典加载时」求值出来的，只有把词典替换成新实例才会按新主题色重算；
            //    否则会出现「主题色改了，但按钮等控件还是旧色」。
            //    updateAccent:false —— 不让 WPF-UI 把强调色重置成系统色。
            ApplicationThemeManager.Apply(theme, Wpf.Ui.Controls.WindowBackdropType.Mica, updateAccent: false);

            // ③ 主题词典重载后再刷一次背景遮罩，保证与当前明暗主题一致
            //    背景图遮罩要随明暗主题换色，否则亮色主题下深色图片会让文字糊成一片
            //    卡片本身是不透明的，遮罩只需要压住壁纸的对比度，不必太厚
            Application.Current.Resources["AppBackgroundScrimBrush"] = new SolidColorBrush(
                theme == ApplicationTheme.Dark
                    ? Color.FromArgb(0x8C, 0x00, 0x00, 0x00)
                    : Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF));

            // ④ 最后修复各窗口底色，见方法说明
            RepairWindowBackgrounds();
        }

        /// <summary>
        /// 把「被固定成某个具体颜色」的窗口底色重新指回主题画刷，使其随明暗主题变化。
        ///
        /// WPF-UI 在重载主题词典时，会把没走系统背板的窗口（向导、各类登录窗口等）Background
        /// 写死成一个「切换前」的色刷，之后不再更新：切到暗色后窗口仍是浅色底，而文字画刷已经
        /// 变成白色 —— 看上去就是整个界面「全白」。这里把它重新指回主题词典（DynamicResource），
        /// 换主题即自动跟随。背景透明的窗口（Mica 主窗口）保持原样，避免把系统材质盖住。
        /// </summary>
        private static void RepairWindowBackgrounds()
        {
            var app = Application.Current;
            if (app == null)
                return;

            foreach (System.Windows.Window window in app.Windows)
            {
                var background = window.Background;

                var isTransparent = background == null
                    || (background is SolidColorBrush solid && solid.Color.A == 0);
                if (isTransparent)
                    continue;

                window.SetResourceReference(System.Windows.Window.BackgroundProperty, "ApplicationBackgroundBrush");
            }
        }

        /// <summary>
        /// 主题色 / 渐变发生变化时的统一入口：整体刷新一次主题（见 <see cref="ApplyTheme"/>），
        /// 确保所有依赖强调色的控件都跟着变。设置页改动主题色后必须调用它。
        /// </summary>
        public static void ApplyAccentChange() => ApplyTheme();

        /// <summary>
        /// 按当前配置写入强调色资源键（含可选渐变）。
        /// 注意：只覆盖资源键、不做整窗主题刷新；用户改主题色请走 <see cref="ApplyAccentChange"/>。
        /// </summary>
        public static void ApplyAccent()
        {
            var primary = TryParseColor(Current.AccentColor, out var parsedPrimary)
                ? parsedPrimary
                : DefaultAccentColor;
            var end = TryParseColor(Current.AccentColorEnd, out var parsedEnd)
                ? parsedEnd
                : primary;

            ApplyAccentCore(primary, end, Current.UseAccentGradient, CurrentApplicationTheme);
        }

        /// <summary>
        /// 覆盖 WPF-UI 的强调色资源，并把 <c>AppAccentGradientBrush</c> 换成当前的主色渐变。
        /// </summary>
        public static void ApplyAccentCore(Color primary, Color end, bool gradient, ApplicationTheme theme)
        {
            var useGradient = gradient && primary != end;
            var resources = Application.Current.Resources;

            resources["SystemAccentColor"] = primary;
            resources["SystemAccentColorPrimary"] = primary;
            resources["SystemAccentColorSecondary"] = useGradient ? end : primary;
            resources["SystemAccentColorTertiary"] = useGradient ? Blend(primary, end, 0.5) : primary;
            resources["SystemAccentColorBrush"] = new SolidColorBrush(primary);

            // WPF-UI 的按钮等控件经由 Color 版的 AccentFillColorDefault → AccentButtonBackground 取色，
            // 只改 *Brush 版本它们不会跟着变，所以 Color 和 Brush 都要写。
            resources["AccentFillColorDefault"] = primary;
            resources["AccentFillColorSecondary"] = primary;
            resources["AccentFillColorTertiary"] = primary;

            resources["AccentFillColorDefaultBrush"] = new SolidColorBrush(primary);
            resources["AccentFillColorSecondaryBrush"] = new SolidColorBrush(primary) { Opacity = 0.7 };
            resources["AccentFillColorTertiaryBrush"] = new SolidColorBrush(primary) { Opacity = 0.5 };
            resources["AccentTextFillColorPrimaryBrush"] = new SolidColorBrush(Colors.White);
            resources["AccentTextFillColorSecondaryBrush"] = new SolidColorBrush(Colors.White) { Opacity = 0.7 };

            // 页面里的渐变强调条、渐变预览统一读这个键
            resources["AppAccentGradientBrush"] = BuildAccentBrush(primary, end, useGradient);

            ApplicationAccentColorManager.Apply(primary, theme, false);
        }

        /// <summary>把当前配置的背景图广播给主窗口（主窗口负责实际的图片加载与渲染）。</summary>
        public static void ApplyBackground()
        {
            ApplyContentBackground();

            WeakReferenceMessenger.Default.Send(
                new BackgroundChangedMessage(Current.BackgroundImagePath, Current.BackgroundImageOpacity));
        }

        /// <summary>
        /// 让 NavigationView 的内容区透出窗口背景。
        ///
        /// WPF-UI 的内容区默认自带一层不透明底（资源键 NavigationViewContentBackground），
        /// 不处理的话背景图只会被整块盖住。只有在确实设置了壁纸时才把它改成透明，
        /// 其余情况移除覆盖，交回给 WPF-UI 的主题默认值。
        /// </summary>
        private static void ApplyContentBackground()
        {
            var resources = Application.Current?.Resources;
            if (resources == null)
                return;

            if (!string.IsNullOrWhiteSpace(Current.BackgroundImagePath))
                resources["NavigationViewContentBackground"] = Brushes.Transparent;
            else
                resources.Remove("NavigationViewContentBackground");
        }

        private static Brush BuildAccentBrush(Color primary, Color end, bool gradient)
        {
            if (!gradient)
                return new SolidColorBrush(primary);

            var brush = new LinearGradientBrush(primary, end, new Point(0, 0), new Point(1, 1));
            brush.Freeze();
            return brush;
        }

        private static Color Blend(Color from, Color to, double ratio)
        {
            byte Mix(byte a, byte b) => (byte)Math.Round(a + (b - a) * ratio);

            return Color.FromArgb(
                Mix(from.A, to.A),
                Mix(from.R, to.R),
                Mix(from.G, to.G),
                Mix(from.B, to.B));
        }

        public static bool TryParseColor(string value, out Color color)
        {
            color = Colors.Transparent;
            if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("#"))
                return false;

            try
            {
                color = (Color)ColorConverter.ConvertFromString(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string ColorToString(Color color) =>
            $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
