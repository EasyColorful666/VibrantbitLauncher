using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VibrantbitLauncher.ViewModels.Pages;
using VibrantbitLauncher.Views.Windows;
using Wpf.Ui;
using UiMessageBox = Wpf.Ui.Controls.MessageBox;

namespace VibrantbitLauncher.Views.Pages
{
    public partial class DownloadResourcesPage : Page
    {
        public DownloadResourcesPage()
        {
            InitializeComponent();
        }

        /// <summary>由下载中心在切换分类时调用，指定这个页面展示哪一类资源。</summary>
        public void SetProjectType(string projectType)
        {
            if (DataContext is DownloadResourcesViewModel vm)
                vm.SetProjectType(projectType);
        }

        private async void OnDownloadClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not Button button || button.DataContext is not ModrinthMod mod)
                {
                    await new UiMessageBox { Title = "错误", Content = "无法获取模组信息" }.ShowDialogAsync();
                    return;
                }

                if (string.IsNullOrWhiteSpace(mod.ProjectId))
                {
                    await new UiMessageBox { Title = "提示", Content = $"模组「{mod.Name}」没有有效的项目 ID" }.ShowDialogAsync();
                    return;
                }

                bool navigated = false;

                var navService = App.Services.GetService(typeof(INavigationService)) as INavigationService;
                if (navService != null)
                    navigated = navService.Navigate(typeof(ModPage));

                if (!navigated)
                {
                    var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                    if (mainWindow != null)
                        navigated = mainWindow.Navigate(typeof(ModPage));
                }

                if (!navigated)
                {
                    await new UiMessageBox { Title = "错误", Content = "导航到模组详情页失败" }.ShowDialogAsync();
                    return;
                }

                var modPage = App.Services.GetService(typeof(ModPage)) as ModPage;
                if (modPage == null)
                {
                    await new UiMessageBox { Title = "错误", Content = "模组详情页未注册" }.ShowDialogAsync();
                    return;
                }

                _ = modPage.LoadForProject(mod.ProjectId);
            }
            catch (Exception ex)
            {
                await new UiMessageBox { Title = "错误", Content = $"打开模组详情失败：{ex.Message}" }.ShowDialogAsync();
            }
        }
    }
}
