using Flurl.Util;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using MinecraftLaunch.Base.Models.Authentication;
using MinecraftLaunch.Base.Models.Authentication.Yggdrasil;
using MinecraftLaunch.Components.Authenticator;
using MinecraftLaunch.Components.Provider;
using SixLabors.ImageSharp;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Windows.Media;
using VibrantbitLauncher.Models;
using VibrantbitLauncher.Services;
using VibrantbitLauncher.ViewModels.Windows;
using VibrantbitLauncher.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;
namespace VibrantbitLauncher.ViewModels.Pages
{
    public partial class AccountPageViewModel : ViewModelBase
    {
        private SnackbarService snackbarService = new();
        ObservableCollection<User> users = new();
        ObservableCollection<string> userNames = new();
        ObservableCollection<Account> accounts = new();
        Account selectedAccount;
        YggdrasilAccountProfile yggdrasilAccountProfile;
        string offlineAccountName;

        public RelayCommand AuthenticateMicrosoftCommand { get; set; }
        public RelayCommand AuthenticateYggdrasilCommand { get; set; }
        public RelayCommand AuthenticateOfflineCommand { get; set; }
        public RelayCommand<SnackbarPresenter> LoadCommand { get; set; }
        public ObservableCollection<User> Users
        {
            get => users;
            set => Set(ref users, value);
        }
        public bool IsLoaded { get; set; }
        public AccountPageViewModel()
        {
            IsLoaded = false;
            Messenger.Default.Register<YggdrasilAccountProfile>(this, "YggdrasilAccountProfile", NewYggdrasilAccountProfile);
            Messenger.Default.Register<string>(this, "OfflineAccountProfile", NewOfflineAccountProfile);
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
        /// 从配置文件恢复已保存的账户，按类型分别重建。
        /// </summary>
        private void RestoreSavedAccounts()
        {
            // 离线账户
            foreach (var saved in SettingsService.Current.OfflineAccounts)
            {
                if (string.IsNullOrEmpty(saved.Name)) continue;
                var account = new OfflineAuthenticator().Authenticate(saved.Name);
                AddAccountToList(account, "Offline", "/Assets/gravatar.png");
                AutoSelectIfSaved(account, saved.Uuid);
            }

            // 微软账户：用保存的 token 重建
            foreach (var saved in SettingsService.Current.MicrosoftAccounts)
            {
                var account = TryCreateMicrosoftAccount(saved);
                if (account != null)
                {
                    AddAccountToList(account, "Microsoft", ".\\res\\skin.png");
                    AutoSelectIfSaved(account, saved.Uuid);
                }
                else
                {
                    // 重建失败，用离线方式占位
                    var fallback = new OfflineAuthenticator().Authenticate(saved.Name);
                    AddAccountToList(fallback, "Microsoft", "/Assets/gravatar.png");
                }
            }

            // Yggdrasil 账户：用保存的 token 重建
            foreach (var saved in SettingsService.Current.YggdrasilAccounts)
            {
                var account = TryCreateYggdrasilAccount(saved);
                if (account != null)
                {
                    AddAccountToList(account, "Yggdrasil", ".\\res\\skin.png");
                    AutoSelectIfSaved(account, saved.Uuid);
                }
                else
                {
                    var fallback = new OfflineAuthenticator().Authenticate(saved.Name);
                    AddAccountToList(fallback, "Yggdrasil", "/Assets/gravatar.png");
                }
            }
        }

        private void AddAccountToList(Account account, string type, string imagePath)
        {
            userNames.Add(account.Name);
            accounts.Add(account);
            users.Add(new User
            {
                Name = account.Name,
                Account = account,
                AccountType = type,
                SelectedCommand = new RelayCommand<Account>(Selected),
                ImagePath = imagePath
            });
        }

        private void AutoSelectIfSaved(Account account, string uuid)
        {
            if (uuid == SettingsService.Current.SelectedAccountUuid)
            {
                selectedAccount = account;
                MainWindowViewModel.MainModel.Account = account;
            }
        }

        /// <summary>
        /// 尝试用保存的数据创建微软账户。
        /// </summary>
        private static Account? TryCreateMicrosoftAccount(MicrosoftAccountData data)
        {
            try
            {
                var asm = Assembly.Load("MinecraftLaunch");
                var type = asm.GetType("MinecraftLaunch.Base.Models.Authentication.MicrosoftAccount");
                if (type == null) return null;

                var instance = Activator.CreateInstance(type);
                SetProperty(instance, "Name", data.Name);
                SetProperty(instance, "Uuid", data.Uuid);
                SetProperty(instance, "AccessToken", data.AccessToken);
                SetProperty(instance, "RefreshToken", data.RefreshToken);
                SetProperty(instance, "LastRefreshTime", data.LastRefreshTime);
                return instance as Account;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 尝试用保存的数据创建 Yggdrasil 账户。
        /// </summary>
        private static Account? TryCreateYggdrasilAccount(YggdrasilAccountData data)
        {
            try
            {
                var asm = Assembly.Load("MinecraftLaunch");
                var type = asm.GetType("MinecraftLaunch.Base.Models.Authentication.Yggdrasil.YggdrasilAccount");
                if (type == null) return null;

                var instance = Activator.CreateInstance(type);
                SetProperty(instance, "Name", data.Name);
                SetProperty(instance, "Uuid", data.Uuid);
                SetProperty(instance, "AccessToken", data.AccessToken);
                SetProperty(instance, "ClientToken", data.ClientToken);
                SetProperty(instance, "YggdrasilServerUrl", data.YggdrasilServerUrl);
                return instance as Account;
            }
            catch
            {
                return null;
            }
        }

        private static void SetProperty(object obj, string name, object value)
        {
            var prop = obj.GetType().GetProperty(name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop != null && prop.CanWrite)
                prop.SetValue(obj, value);
        }

        /// <summary>
        /// 获取账户的 Uuid（反射兼容各类型）。
        /// </summary>
        private static string GetAccountUuid(Account account)
        {
            try
            {
                var prop = account.GetType().GetProperty("Uuid",
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                return prop?.GetValue(account)?.ToString() ?? account.Name;
            }
            catch
            {
                return account.Name;
            }
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
            var oAuth2Token = await authenticator.DeviceFlowAuthAsync(dc =>
            {
                snackbarService.Show("提示","设备代码: " + dc.UserCode + " 设备代码已写入剪贴板",ControlAppearance.Info,null,TimeSpan.MaxValue);
                string textToCopy = dc.UserCode;
                SetText(textToCopy);
                Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = dc.VerificationUrl });
            });

            var userProfile = await authenticator.AuthenticateAsync(oAuth2Token);
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await using var skinStream = await SkinProvider.GetMicrosoftSkinDataAsync(userProfile, cts.Token);

            using var ms = new MemoryStream();
            await skinStream.CopyToAsync(ms, cts.Token);
            var skinBytes = ms.ToArray();
            var skin = new MinecraftLaunch.Skin.SkinResolver(skinBytes);
            if(!Directory.Exists(".\\res"))  
                skin.CropSkinHeadBitmap().SaveAsPng(".\\res\\skin.png");
            else
            {
                Directory.CreateDirectory(".\\res");
            }
            App.Current.Dispatcher.Invoke(() =>
            { 
                userNames.Add(userProfile.Name);
                accounts.Add(userProfile);
                users.Add(new User { Name = userProfile.Name, Account = userProfile, AccountType = "Microsoft", SelectedCommand = new RelayCommand<Account>(Selected) , ImagePath = ".\\res\\skin.png" });
                SettingsService.SaveMicrosoftAccount(userProfile);
            });
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
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    await using var skinStream = await SkinProvider.GetYggdrasilSkinDataAsync(userProfile.First() as YggdrasilAccount, cts.Token);

                    using var ms = new MemoryStream();
                    await skinStream.CopyToAsync(ms, cts.Token);
                    var skinBytes = ms.ToArray();
                    var skin = new MinecraftLaunch.Skin.SkinResolver(skinBytes);
                    if (!Directory.Exists(".\\res"))
                        skin.CropSkinHeadBitmap().SaveAsPng(".\\res\\skin.png");
                    else
                    {
                        Directory.CreateDirectory(".\\res");
                    }
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        userNames.Add(userProfile.First().Name);
                        accounts.Add(userProfile.First() as YggdrasilAccount);
                        users.Add(new User { Name = userProfile.First().Name, Account = userProfile.First(), AccountType = "Yggdrasil", SelectedCommand = new RelayCommand<Account>(Selected) ,ImagePath= ".\\res\\skin.png" });
                        SettingsService.SaveYggdrasilAccount(userProfile.First());
                    });
                }
            }
            catch (Exception ex)
            {
                snackbarService.Show("错误", $"用户登录失败" + "\n" + $"{ex.Message}", ControlAppearance.Danger);
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
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        userNames.Add(userprofile.Name);
                        accounts.Add(userprofile);
                        users.Add(new User { Name = offlineAccountName, Account = userprofile, AccountType = "Offline", SelectedCommand = new RelayCommand<Account>(Selected) });
                        SettingsService.SaveOfflineAccount(userprofile);
                    });
                    snackbarService.Show("提示", $"离线用户{offlineAccountName}已创建", ControlAppearance.Info, null, snackbarService.DefaultTimeOut);
            }
        }

        void Selected(Account account)
        {
            selectedAccount = account;
            MainWindowViewModel.MainModel.Account = account;
            SettingsService.SetSelectedAccount(GetAccountUuid(account));
        }
    }

    public class User
    {
        public string Name { get; set; }
        public Account Account { get; set; }
        public string AccountType { get; set; }
        public RelayCommand<Account> SelectedCommand { get; set; }
        public string ImagePath { get; set; } = @"/Assets/gravatar.png";
    }

    public class YggdrasilAccountProfile
    {
        public string Server { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
