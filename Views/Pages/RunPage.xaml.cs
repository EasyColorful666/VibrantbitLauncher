

using VibrantbitLauncher.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace VibrantbitLauncher.Views.Pages
{
    /// <summary>
    /// RunPage.xaml 的交互逻辑
    /// </summary>
    public partial class RunPage : System.Windows.Controls.Page
    {

       
        public RunPage()
        {
            this.DataContext = new RunPageViewModel();

            InitializeComponent();
        }


    }
}
