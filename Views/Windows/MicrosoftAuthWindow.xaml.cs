using System;
using System.Diagnostics;
using System.Windows;
using Wpf.Ui.Controls;

namespace VibrantbitLauncher.Views.Windows
{
    /// <summary>
    /// MicrosoftAuthWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MicrosoftAuthWindow : FluentWindow
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
                // 用 WPF-UI 的消息框，跟随明暗主题（原生 MessageBox 是固定的系统外观）
                ShowError("无法打开浏览器", ex.Message);
            }
        }

        /// <summary>显示一个跟随主题的错误提示框（模态，挂在当前窗口上）。</summary>
        private async void ShowError(string title, string message)
        {
            var box = new Wpf.Ui.Controls.MessageBox
            {
                Title = title,
                Content = message,
                CloseButtonText = "确定",
                Owner = this,
            };

            await box.ShowDialogAsync();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsCancelled = true;
            Close();
        }
    }
}
