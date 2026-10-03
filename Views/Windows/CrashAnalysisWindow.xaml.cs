using System.Diagnostics;
using System.IO;
using System.Windows;
using MinecraftLaunch.Base.Models.Game;
using MinecraftLaunch.Components.Logging;

namespace VibrantbitLauncher.Views.Windows
{
    public partial class CrashAnalysisWindow : Window
    {
        private readonly string _crashReportFolder;

        public CrashAnalysisWindow(MinecraftEntry minecraft)
        {
            InitializeComponent();

            var analyzer = new LogAnalyzer(minecraft);
            var result = analyzer.Analyze();

            foreach (var reason in result.CrashReasons)
                CrashReasonsList.Items.Add(reason.ToString());

            foreach (var mod in result.SuspiciousMods)
                SuspiciousModsList.Items.Add(mod.ToString());

            if (CrashReasonsList.Items.Count == 0)
                CrashReasonsList.Items.Add("未检测到明确的崩溃原因，请查看崩溃报告文件。");

            if (SuspiciousModsList.Items.Count == 0)
                SuspiciousModsList.Items.Add("未检测到可疑模组。");

            var mcFolder = Path.Combine(AppContext.BaseDirectory, ".minecraft");
            _crashReportFolder = Path.Combine(mcFolder, "crash-reports");
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OpenCrashFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Directory.Exists(_crashReportFolder))
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", _crashReportFolder) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("崩溃报告目录不存在：" + _crashReportFolder, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法打开目录：" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
