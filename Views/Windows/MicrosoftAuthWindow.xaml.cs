using System;
using System.Diagnostics;
using System.Windows;

namespace VibrantbitLauncher.Views.Windows
{
    /// <summary>
    /// MicrosoftAuthWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MicrosoftAuthWindow : Window
    {
        /// <summary>用户是否点击了取消。</summary>
        public bool IsCancelled { get; private set; }

        private string _verificationUrl = string.Empty;

        public MicrosoftAuthWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 更新设备代码和验证 URL（在 DeviceFlowAuthAsync 回调中调用）。
        /// </summary>
        public void SetCode(string userCode, string verificationUrl)
        {
            CodeText.Text = userCode;
            _verificationUrl = verificationUrl;

            // 自动复制代码到剪贴板
            try
            {
                Clipboard.SetText(userCode);
            }
            catch { /* 剪贴板访问失败忽略 */ }
        }

        private void OpenBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_verificationUrl))
                return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = _verificationUrl
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开浏览器：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsCancelled = true;
            Close();
        }
    }
}
