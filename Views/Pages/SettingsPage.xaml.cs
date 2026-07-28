using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using VibrantbitLauncher.ViewModels.Pages;
using Wpf.Ui.Abstractions.Controls;

namespace VibrantbitLauncher.Views.Pages
{
    public partial class SettingsPage : System.Windows.Controls.Page
    {
        

        public SettingsPage()
        {
            var ViewModel = new SettingsPageViewModel();
            DataContext = ViewModel;

            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "图片文件|*.png;*.jpg";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            openFileDialog.Title = "打开图片文件";
            openFileDialog.Multiselect = false;
            if (openFileDialog.ShowDialog() is true)
            {
                Messenger.Default.Send<string>(openFileDialog.FileName, "UpdateBackground");
            }
        }
    }
}
