using System.Windows;
using System.Windows.Controls;
using VibrantbitLauncher.Services;
using VibrantbitLauncher.ViewModels.Pages;

namespace VibrantbitLauncher.Views.Pages
{
    /// <summary>
    /// 开机向导第 3 步：登录账户。
    /// 复用账户页的视图模型和登录命令，保证两种入口行为完全一致。
    /// </summary>
    public partial class WelcomeAccountPage : Page
    {
        private readonly AccountPageViewModel _viewModel;

        public WelcomeAccountPage()
        {
            _viewModel = App.Services.GetService(typeof(AccountPageViewModel)) as AccountPageViewModel
                         ?? new AccountPageViewModel();

            DataContext = _viewModel;
            InitializeComponent();

            Loaded += (_, _) =>
            {
                if (!_viewModel.IsLoaded)
                    _viewModel.LoadCommand.Execute(SnackbarPresenter);
            };
        }

        private void OnBack(object sender, RoutedEventArgs e)
            => NavigationService?.Navigate(new WelcomeSetupPage());

        private void OnSkip(object sender, RoutedEventArgs e)
            => NavigationService?.Navigate(new WelcomeCompletePage());

        private void OnNext(object sender, RoutedEventArgs e)
        {
            SettingsService.Save();
            NavigationService?.Navigate(new WelcomeCompletePage());
        }
    }
}