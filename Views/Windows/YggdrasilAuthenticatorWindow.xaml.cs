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
    /// YggdrasilAuthenticatorWindow.xaml 的交互逻辑
    /// </summary>
    public partial class YggdrasilAuthenticatorWindow : FluentWindow
    {
        public YggdrasilAuthenticatorWindow()
        {

            InitializeComponent();

            // 兜底：无论从哪里 new 出来，都挂到主窗口上。
            // WindowStartupLocation="CenterOwner" 在 Owner 为 null 时会退化，
            // 窗口就跑到屏幕角落（而不是主窗口中央）去了。
            Owner ??= Application.Current?.MainWindow;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(textBox1.Text)&& !string.IsNullOrEmpty(textBox2.Text)&& !string.IsNullOrEmpty(textBox3.Password))
            {
                
                WeakReferenceMessenger.Default.Send<YggdrasilAccountProfile, string>(new YggdrasilAccountProfile{Server=textBox1.Text,Email=textBox2.Text,Password=textBox3.Password}, "YggdrasilAccountProfile");
                this.Close();
            }
        }
    }
}
