using MinecraftLaunch.Base.Models.Authentication;
using MinecraftLaunch.Base.Models.Authentication.Yggdrasil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;
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
        public string MinecraftFolder { get; set; } = "./.minecraft";
        public string JavaPath { get; set; } = string.Empty;
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

        public static void Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    Current = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
                }
            }
            catch
            {
                Current = new AppSettings();
            }

            MainWindowViewModel.MainModel.MinecraftFolder = Current.MinecraftFolder;
            MainWindowViewModel.MainModel.JavaPath = string.IsNullOrEmpty(Current.JavaPath) ? null : Current.JavaPath;
            MainWindowViewModel.MainModel.IsMicrosoftAccount = Current.IsMicrosoftAccount;
        }

        public static void Save()
        {
            try
            {
                Current.MinecraftFolder = MainWindowViewModel.MainModel.MinecraftFolder ?? "./.minecraft";
                Current.JavaPath = MainWindowViewModel.MainModel.JavaPath ?? string.Empty;
                Current.IsMicrosoftAccount = MainWindowViewModel.MainModel.IsMicrosoftAccount;

                var json = JsonSerializer.Serialize(Current, JsonOptions);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SettingsService.Save] 保存失败: {ex}");
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

        // ===== 主题 =====

        public static void ApplyTheme()
        {
            var theme = Current.Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase)
                ? ApplicationTheme.Dark
                : ApplicationTheme.Light;

            ApplicationThemeManager.Apply(theme);

            // 统一使用蓝色主题色，不跟随系统
            var blue = (Color)ColorConverter.ConvertFromString("#FF0078D4");
            Application.Current.Resources["SystemAccentColor"] = blue;
            Application.Current.Resources["SystemAccentColorPrimary"] = blue;
            Application.Current.Resources["SystemAccentColorSecondary"] = blue;
            Application.Current.Resources["SystemAccentColorTertiary"] = blue;
            ApplicationAccentColorManager.Apply(blue, theme, false);
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
