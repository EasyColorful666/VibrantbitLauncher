using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using VibrantbitLauncher.ViewModels.Pages;
using Wpf.Ui.Controls;

namespace VibrantbitLauncher.Views.Windows
{
    /// <summary>
    /// OfflineAuthenticatorWindow.xaml 的交互逻辑
    /// </summary>
    public partial class OfflineAuthenticatorWindow : Window
    {
        public OfflineAuthenticatorWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if(!string.IsNullOrEmpty(textBox1.Text))
            {
                WeakReferenceMessenger.Default.Send<string, string>(textBox1.Text, "OfflineAccountProfile");
                this.Close();
            }

        }
    }
}
