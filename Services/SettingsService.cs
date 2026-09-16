using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;
using VibrantbitLauncher.ViewModels.Windows;
using Wpf.Ui.Appearance;

namespace VibrantbitLauncher.Services
{
    /// <summary>微软账户保存数据。</summary>
    public class MicrosoftAccountData
    {
        public string Name { get; set; } = string.Empty;
        public string Uuid { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime LastRefreshTime { get; set; }
    }

    /// <summary>外置（Yggdrasil）账户保存数据。</summary>
    public class YggdrasilAccountData
    {
        public string Name { get; set; } = string.Empty;
        public string Uuid { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string ClientToken { get; set; } = string.Empty;
        public string YggdrasilServerUrl { get; set; } = string.Empty;
    }

    /// <summary>离线账户保存数据。</summary>
    public class OfflineAccountData
    {
        public string Name { get; set; } = string.Empty;
        public string Uuid { get; set; } = string.Empty;
    }

    /// <summary>
    /// 应用程序设置数据模型，序列化为 JSON 配置文件。
    /// </summary>
    public class AppSettings
    {
        public string Theme { get; set; } = "Light";
        public string AccentColor { get; set; } = "System";
        public string MinecraftFolder { get; set; } = "./.minecraft";
        public string JavaPath { get; set; } = string.Empty;
        public List<MicrosoftAccountData> MicrosoftAccounts { get; set; } = new();
        public List<YggdrasilAccountData> YggdrasilAccounts { get; set; } = new();
        public List<OfflineAccountData> OfflineAccounts { get; set; } = new();
        public string SelectedAccountUuid { get; set; } = string.Empty;
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
            Converters = { new JsonStringEnumConverter() }
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
        }

        public static void Save()
        {
            try
            {
                Current.MinecraftFolder = MainWindowViewModel.MainModel.MinecraftFolder ?? "./.minecraft";
                Current.JavaPath = MainWindowViewModel.MainModel.JavaPath ?? string.Empty;

                var json = JsonSerializer.Serialize(Current, JsonOptions);
                File.WriteAllText(ConfigPath, json);
            }
            catch
            {
                // 保存失败静默处理
            }
        }

        // ===== 账户保存 =====

        /// <summary>保存微软账户。</summary>
        public static void SaveMicrosoftAccount(object account)
        {
            var data = ExtractProperties<MicrosoftAccountData>(account);
            if (string.IsNullOrEmpty(data.Uuid)) return;

            var existing = Current.MicrosoftAccounts.FirstOrDefault(a => a.Uuid == data.Uuid);
            if (existing != null)
                Current.MicrosoftAccounts.Remove(existing);
            Current.MicrosoftAccounts.Add(data);
            Save();
        }

        /// <summary>保存外置（Yggdrasil）账户。</summary>
        public static void SaveYggdrasilAccount(object account)
        {
            var data = ExtractProperties<YggdrasilAccountData>(account);
            if (string.IsNullOrEmpty(data.Uuid)) return;

            var existing = Current.YggdrasilAccounts.FirstOrDefault(a => a.Uuid == data.Uuid);
            if (existing != null)
                Current.YggdrasilAccounts.Remove(existing);
            Current.YggdrasilAccounts.Add(data);
            Save();
        }

        /// <summary>保存离线账户。</summary>
        public static void SaveOfflineAccount(object account)
        {
            var data = ExtractProperties<OfflineAccountData>(account);
            if (string.IsNullOrEmpty(data.Name)) return;

            var existing = Current.OfflineAccounts.FirstOrDefault(a => a.Name == data.Name);
            if (existing != null)
                Current.OfflineAccounts.Remove(existing);
            Current.OfflineAccounts.Add(data);
            Save();
        }

        /// <summary>设置当前选中的账户 uuid。</summary>
        public static void SetSelectedAccount(string uuid)
        {
            Current.SelectedAccountUuid = uuid ?? string.Empty;
            Save();
        }

        /// <summary>
        /// 用反射从账户对象提取属性，填充到目标数据类型。
        /// </summary>
        private static T ExtractProperties<T>(object account) where T : new()
        {
            var data = new T();
            var dataType = typeof(T);
            var accountType = account.GetType();

            foreach (var prop in dataType.GetProperties())
            {
                var sourceProp = accountType.GetProperty(prop.Name,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (sourceProp == null || !sourceProp.CanRead) continue;

                try
                {
                    var value = sourceProp.GetValue(account);
                    if (value == null) continue;

                    if (prop.PropertyType == typeof(string))
                        prop.SetValue(data, value.ToString());
                    else if (prop.PropertyType == typeof(DateTime))
                        prop.SetValue(data, Convert.ToDateTime(value));
                    else
                        prop.SetValue(data, value);
                }
                catch { /* 忽略单个属性提取失败 */ }
            }
            return data;
        }

        // ===== 主题 =====

        public static void ApplyTheme()
        {
            var theme = Current.Theme.Equals("Dark", StringComparison.OrdinalIgnoreCase)
                ? ApplicationTheme.Dark
                : ApplicationTheme.Light;

            ApplicationThemeManager.Apply(theme);

            if (Current.AccentColor.Equals("System", StringComparison.OrdinalIgnoreCase))
            {
                ApplicationAccentColorManager.ApplySystemAccent();
            }
            else if (TryParseColor(Current.AccentColor, out var color))
            {
                Application.Current.Resources["SystemAccentColor"] = color;
                Application.Current.Resources["SystemAccentColorPrimary"] = color;
                Application.Current.Resources["SystemAccentColorSecondary"] = color;
                Application.Current.Resources["SystemAccentColorTertiary"] = color;
                ApplicationAccentColorManager.Apply(color, theme, false);
            }
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
