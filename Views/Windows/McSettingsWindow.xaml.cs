using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using MinecraftLaunch.Base.Models.Authentication;
using MinecraftLaunch.Base.Models.Game;
using MinecraftLaunch.Components.Authenticator;
using MinecraftLaunch.Extensions;
using MinecraftLaunch.Utilities;
using System.IO;
using System.Security.Principal;
using System.Windows.Media;
using VibrantbitLauncher.Models;
using VibrantbitLauncher.ViewModels.Windows;
using VibrantbitLauncher.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.Abstractions.Controls;
using Wpf.Ui.Controls;

namespace VibrantbitLauncher.Views.Windows
{
    /// <summary>
    /// McSettingsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class McSettingsWindow : FluentWindow
    {
        string mcVersion = "";
        public McSettingsWindow()
        {
            InitializeComponent();
            Messenger.Default.Register<string>(this, "McVersionForSetting", SetMcVersion);
        }

        private void SetMcVersion(string version)
        {
            mcVersion = version;
            Title = $"设置版本 - {version}";
        }
    }
}
