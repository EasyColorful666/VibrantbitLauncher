using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MinecraftLaunch.Base.Models.Authentication;
using MinecraftLaunch.Base.Models.Authentication.Yggdrasil;
using MinecraftLaunch.Components.Authenticator;
using MinecraftLaunch.Components.Provider;
using SixLabors.ImageSharp;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VibrantbitLauncher.Models;
using VibrantbitLauncher.Services;
using VibrantbitLauncher.ViewModels.Windows;
using VibrantbitLauncher.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;
using UiMessageBox = Wpf.Ui.Controls.MessageBox;

namespace VibrantbitLauncher.ViewModels.Pages
{
    public partial class AccountPageViewModel : ObservableObject
    {
        private SnackbarService snackbarService = new();
        ObservableCollection<User> users = new();
        ObservableCollection<string> userNames = new();
        ObservableCollection<Account> accounts = new();
        Account selectedAccount;
        string selectedSkinPath = string.Empty;
        string selectedAccountName = "未选择账户";
        string selectedAccountType = string.Empty;
        YggdrasilAccountProfile yggdrasilAccountProfile;
        string offlineAccountName;

        public RelayCommand AuthenticateMicrosoftCommand { get; set; }
        public RelayCommand AuthenticateYggdrasilCommand { get; set; }
        public RelayCommand AuthenticateOfflineCommand { get; set; }
        public RelayCommand<SnackbarPresenter> LoadCommand { get; set; }
        public ObservableCollection<User> Users
        {
            get => users;
            set => SetProperty(ref users, value);
        }
        public bool IsLoaded { get; set; }

        public string SelectedSkinPath
        {
            get => selectedSkinPath;
            set => SetProperty(ref selectedSkinPath, value);
        }

        public string SelectedAccountName
        {
            get => selectedAccountName;
            set => SetProperty(ref selectedAccountName, value);
        }

        public string SelectedAccountType
        {
            get => selectedAccountType;
            set => SetProperty(ref selectedAccountType, value);
        }

        private void UpdateSelectedDisplay(Account account)
        {
            var user = users.FirstOrDefault(u => u.Account == account);
            SelectedSkinPath = user?.FullSkinPath ?? string.Empty;
            SelectedAccountName = account?.Name ?? "未选择账户";
            SelectedAccountType = user?.AccountType ?? string.Empty;
        }
        public AccountPageViewModel()
        {
            IsLoaded = false;
            WeakReferenceMessenger.Default.Register<YggdrasilAccountProfile, string>(this, "YggdrasilAccountProfile", (_, m) => NewYggdrasilAccountProfile(m));
            WeakReferenceMessenger.Default.Register<string, string>(this, "OfflineAccountProfile", (_, m) => NewOfflineAccountProfile(m));
            AuthenticateMicrosoftCommand = new RelayCommand(AuthenticateMicrosoftAsync);
            AuthenticateYggdrasilCommand = new RelayCommand(AuthenticateYggdrasilAsync);
            AuthenticateOfflineCommand = new RelayCommand(AuthenticateOffline);
            LoadCommand = new RelayCommand<SnackbarPresenter>(Load);
        }

        private void NewOfflineAccountProfile(string name)
        {
            offlineAccountName = name;
        }

        private void NewYggdrasilAccountProfile(YggdrasilAccountProfile profile)
        {
            yggdrasilAccountProfile = profile;
        }

        public void Load(SnackbarPresenter snackbarPresenter)
        {
            this.snackbarService.SetSnackbarPresenter(snackbarPresenter);
            if (!IsLoaded)
            {
                RestoreSavedAccounts();
                IsLoaded = true;
            }
        }

        /// <summary>
        /// 从配置文件恢复已保存的账户，直接使用 MinecraftLaunch 的账户类型。
        /// </summary>
        private void RestoreSavedAccounts()
        {
            // 离线账户：使用史蒂夫皮肤
            foreach (var saved in SettingsService.Current.OfflineAccounts)
            {
                if (string.IsNullOrEmpty(saved.Name)) continue;
                var uuid = saved.Uuid.ToString();
                if (string.IsNullOrEmpty(ResolveFullSkinPath(uuid)))
                    EnsureSteveSkin(saved);
                AddAccountToList(saved, "Offline", ResolveAvatarPath(uuid), ResolveFullSkinPath(uuid));
                AutoSelectIfSaved(saved, uuid);
            }

            // 微软账户
            foreach (var saved in SettingsService.Current.MicrosoftAccounts)
            {
                var uuid = saved.Uuid.ToString();
                AddAccountToList(saved, "Microsoft", ResolveAvatarPath(uuid), ResolveFullSkinPath(uuid));
                AutoSelectIfSaved(saved, uuid);
            }

            // Yggdrasil 账户
            foreach (var saved in SettingsService.Current.YggdrasilAccounts)
            {
                var uuid = saved.Uuid.ToString();
                AddAccountToList(saved, "Yggdrasil", ResolveAvatarPath(uuid), ResolveFullSkinPath(uuid));
                AutoSelectIfSaved(saved, uuid);
            }
        }

        /// <summary>
        /// 为离线账户生成史蒂夫皮肤文件（完整皮肤 + 头部头像）。
        /// </summary>
        private static void EnsureSteveSkin(OfflineAccount account)
        {
            try
            {
                var resDir = Path.Combine(AppContext.BaseDirectory, "res");
                if (!Directory.Exists(resDir)) Directory.CreateDirectory(resDir);
                var fullSkinPath = Path.Combine(resDir, $"{account.Uuid}_full.png");
                var skinPath = Path.Combine(resDir, $"{account.Uuid}.png");

                var resourceStream = Application.GetResourceStream(
                    new Uri("/Assets/steve.png", UriKind.Relative));
                using (var fs = new FileStream(fullSkinPath, FileMode.Create))
                    resourceStream.Stream.CopyTo(fs);

                var skinBytes = File.ReadAllBytes(fullSkinPath);
                var skin = new MinecraftLaunch.Skin.SkinResolver(skinBytes);
                skin.CropSkinHeadBitmap().SaveAsPng(skinPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EnsureSteveSkin] {ex.Message}");
            }
        }

        /// <summary>
        /// 解析头像路径：优先使用按 UUID 保存的皮肤文件，不存在则回退到默认头像。
        /// </summary>
        private static string ResolveAvatarPath(string? uuid)
        {
            var defaultAvatar = "/Assets/gravatar.png";
            if (string.IsNullOrEmpty(uuid)) return defaultAvatar;

            var skinPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "res", $"{uuid}.png"));
            if (File.Exists(skinPath)) return skinPath;

            // 兼容旧路径 .\res\skin.png
            var legacyPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "res", "skin.png"));
            if (File.Exists(legacyPath)) return legacyPath;

            return defaultAvatar;
        }

        /// <summary>
        /// 解析完整皮肤路径（用于3D预览），不存在则返回空字符串。
        /// </summary>
        private static string ResolveFullSkinPath(string? uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return string.Empty;
            var fullPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "res", $"{uuid}_full.png"));
            return File.Exists(fullPath) ? fullPath : string.Empty;
        }

        private void AddAccountToList(Account account, string type, string imagePath, string fullSkinPath = "")
        {
            userNames.Add(account.Name);
            accounts.Add(account);
            users.Add(new User
            {
                Name = account.Name,
                Account = account,
                AccountType = type,
                SelectedCommand = new RelayCommand<Account>(Selected),
                DeleteCommand = new RelayCommand<Account>(DeleteAccount),
                ImagePath = imagePath,
                FullSkinPath = fullSkinPath
            });
        }

        private void AutoSelectIfSaved(Account account, string uuid)
        {
            if (uuid == SettingsService.Current.SelectedAccountUuid)
            {
                selectedAccount = account;
                MainWindowViewModel.MainModel.Account = account;
                MainWindowViewModel.MainModel.IsMicrosoftAccount = account is MicrosoftAccount;
                UpdateSelectedDisplay(account);
            }
        }

        /// <summary>
        /// 获取账户的 Uuid，兼容各账户类型。
        /// </summary>
        private static string GetAccountUuid(Account account)
        {
            return account switch
            {
                MicrosoftAccount ms => ms.Uuid.ToString(),
                YggdrasilAccount yg => yg.Uuid.ToString(),
                OfflineAccount off => off.Uuid.ToString(),
                _ => account.Name
            };
        }

        [DllImport("User32")]
        public static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("User32")]
        public static extern bool CloseClipboard();

        [DllImport("User32")]
        public static extern bool EmptyClipboard();

        [DllImport("User32")]
        public static extern bool IsClipboardFormatAvailable(int format);

        [DllImport("User32")]
        public static extern IntPtr GetClipboardData(int uFormat);

        [DllImport("User32", CharSet = CharSet.Unicode)]
        public static extern IntPtr SetClipboardData(int uFormat, IntPtr hMem);
        public static void SetText(string text)
        {
            if (!OpenClipboard(IntPtr.Zero))
            {
                SetText(text);
                return;
            }
            EmptyClipboard();
            SetClipboardData(13, Marshal.StringToHGlobalUni(text));
            CloseClipboard();
        }

        private async void AuthenticateMicrosoftAsync()
        {
            MicrosoftAuthenticator authenticator = new("f4c1c237-e68f-4866-a998-82f0db041c55");
            var authWindow = new MicrosoftAuthWindow
            {
                Owner = Application.Current.MainWindow
            };

            try
            {
                // 后台执行 DeviceFlow 认证
                var authTask = Task.Run(async () =>
                {
                    var token = await authenticator.DeviceFlowAuthAsync(dc =>
                    {
                        App.Current.Dispatcher.Invoke(() => authWindow.SetCode(dc.UserCode, dc.VerificationUrl));
                    });
                    App.Current.Dispatcher.Invoke(() => authWindow.Close());
                    return token;
                });

                authWindow.ShowDialog();

                if (authWindow.IsCancelled)
                    return;

                var oAuth2Token = await authTask;

                var userProfile = await authenticator.AuthenticateAsync(oAuth2Token);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await using var skinStream = await SkinProvider.GetMicrosoftSkinDataAsync(userProfile, cts.Token);

                using var ms = new MemoryStream();
                await skinStream.CopyToAsync(ms, cts.Token);
                var skinBytes = ms.ToArray();
                var skin = new MinecraftLaunch.Skin.SkinResolver(skinBytes);
                var resDir = Path.Combine(AppContext.BaseDirectory, "res");
                if (!Directory.Exists(resDir)) Directory.CreateDirectory(resDir);
                var skinPath = Path.Combine(resDir, $"{userProfile.Uuid}.png");
                skin.CropSkinHeadBitmap().SaveAsPng(skinPath);
                App.Current.Dispatcher.Invoke(() =>
                {
                    userNames.Add(userProfile.Name);
                    accounts.Add(userProfile);
                    users.Add(new User { Name = userProfile.Name, Account = userProfile, AccountType = "Microsoft", SelectedCommand = new RelayCommand<Account>(Selected), DeleteCommand = new RelayCommand<Account>(DeleteAccount), ImagePath = skinPath });
                    SettingsService.SaveMicrosoftAccount(userProfile);
                });
            }
            catch (TaskCanceledException)
            {
                authWindow.Close();
            }
            catch (Exception ex)
            {
                authWindow.Close();
                await new UiMessageBox { Title = "错误", Content = $"微软登录失败：{ex.Message}" }.ShowDialogAsync();
            }
        }

        private async void AuthenticateYggdrasilAsync()
        {
            try
            {
                YggdrasilAuthenticatorWindow window = new();
                window.ShowDialog();
                if (yggdrasilAccountProfile != null)
                {
                    YggdrasilAuthenticator authenticator = new(yggdrasilAccountProfile.Server, yggdrasilAccountProfile.Email, yggdrasilAccountProfile.Password);
                    var userProfile = await authenticator.AuthenticateAsync();
                    var account = userProfile.First() as YggdrasilAccount;

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    await using var skinStream = await SkinProvider.GetYggdrasilSkinDataAsync(account, cts.Token);

                    using var ms = new MemoryStream();
                    await skinStream.CopyToAsync(ms, cts.Token);
                    var skinBytes = ms.ToArray();
                    var skin = new MinecraftLaunch.Skin.SkinResolver(skinBytes);
                    var resDir = Path.Combine(AppContext.BaseDirectory, "res");
                    if (!Directory.Exists(resDir)) Directory.CreateDirectory(resDir);
                    var skinPath = Path.Combine(resDir, $"{account.Uuid}.png");
                    skin.CropSkinHeadBitmap().SaveAsPng(skinPath);
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        userNames.Add(account.Name);
                        accounts.Add(account);
                        users.Add(new User { Name = account.Name, Account = account, AccountType = "Yggdrasil", SelectedCommand = new RelayCommand<Account>(Selected), DeleteCommand = new RelayCommand<Account>(DeleteAccount), ImagePath = skinPath });
                        SettingsService.SaveYggdrasilAccount(account);
                    });
                }
            }
            catch (Exception ex)
            {
                snackbarService.Show("错误", $"用户登录失败" + "\n" + $"{ex.Message}", ControlAppearance.Danger, null, snackbarService.DefaultTimeOut);
            }
        }

        private void AuthenticateOffline()
        {
            OfflineAuthenticatorWindow window = new();
            window.ShowDialog();
            OfflineAuthenticator authenticator = new();
            if (!string.IsNullOrEmpty(offlineAccountName))
            {
                var userprofile = authenticator.Authenticate(offlineAccountName);

                // 使用史蒂夫皮肤
                var resDir = Path.Combine(AppContext.BaseDirectory, "res");
                if (!Directory.Exists(resDir)) Directory.CreateDirectory(resDir);
                var skinPath = Path.Combine(resDir, $"{userprofile.Uuid}.png");
                var fullSkinPath = Path.Combine(resDir, $"{userprofile.Uuid}_full.png");
                try
                {
                    // 从嵌入资源提取史蒂夫皮肤
                    var resourceStream = Application.GetResourceStream(
                        new Uri("/Assets/steve.png", UriKind.Relative));
                    using (var fs = new FileStream(fullSkinPath, FileMode.Create))
                    {
                        resourceStream.Stream.CopyTo(fs);
                    }
                    // 生成头部头像
                    var skinBytes = File.ReadAllBytes(fullSkinPath);
                    var skin = new MinecraftLaunch.Skin.SkinResolver(skinBytes);
                    skin.CropSkinHeadBitmap().SaveAsPng(skinPath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Offline] Steve skin setup failed: {ex.Message}");
                }

                App.Current.Dispatcher.Invoke(() =>
                {
                    userNames.Add(userprofile.Name);
                    accounts.Add(userprofile);
                    users.Add(new User
                    {
                        Name = offlineAccountName,
                        Account = userprofile,
                        AccountType = "Offline",
                        SelectedCommand = new RelayCommand<Account>(Selected),
                        DeleteCommand = new RelayCommand<Account>(DeleteAccount),
                        ImagePath = File.Exists(skinPath) ? skinPath : "/Assets/gravatar.png",
                        FullSkinPath = File.Exists(fullSkinPath) ? fullSkinPath : string.Empty
                    });
                    SettingsService.SaveOfflineAccount(userprofile);
                });
                snackbarService.Show("提示", $"离线用户{offlineAccountName}已创建", ControlAppearance.Info, null, snackbarService.DefaultTimeOut);
            }
        }

        void Selected(Account account)
        {
            selectedAccount = account;
            MainWindowViewModel.MainModel.Account = account;
            MainWindowViewModel.MainModel.IsMicrosoftAccount = account is MicrosoftAccount;
            SettingsService.SetSelectedAccount(GetAccountUuid(account));
            UpdateSelectedDisplay(account);
        }

        /// <summary>
        /// 删除账户：从列表和配置文件中移除。
        /// </summary>
        void DeleteAccount(Account account)
        {
            // 从 UI 列表移除
            var user = users.FirstOrDefault(u => u.Account == account);
            if (user != null)
                users.Remove(user);
            accounts.Remove(account);
            userNames.Remove(account.Name);

            // 从配置文件移除（统一使用 SettingsService 的删除方法）
            SettingsService.RemoveAccount(account);

            // 如果删除的是当前选中账户，清空选中状态
            if (selectedAccount == account)
            {
                selectedAccount = null;
                MainWindowViewModel.MainModel.Account = null;
                MainWindowViewModel.MainModel.IsMicrosoftAccount = false;
                SettingsService.SetSelectedAccount(string.Empty);
            }
        }
    }

    public class User
    {
        public string Name { get; set; }
        public Account Account { get; set; }
        public string AccountType { get; set; }
        public RelayCommand<Account> SelectedCommand { get; set; }
        public RelayCommand<Account> DeleteCommand { get; set; }
        public string ImagePath { get; set; } = @"/Assets/gravatar.png";
        public string FullSkinPath { get; set; } = string.Empty;
    }

    public class YggdrasilAccountProfile
    {
        public string Server { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
