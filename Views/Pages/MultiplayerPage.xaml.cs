using System.Windows.Controls;
using VibrantbitLauncher.ViewModels.Pages;

namespace VibrantbitLauncher.Views.Pages
{
    /// <summary>
    /// MultiplayerPage.xaml 的交互逻辑。
    /// 全部行为都走 ViewModel 的命令，这里只负责把 DI 注入的 ViewModel 接到 DataContext。
    /// </summary>
    public partial class MultiplayerPage : Page
    {
        public MultiplayerPage(MultiplayerPageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
