using System.Windows;
using System.Windows.Controls;
using VibrantbitLauncher.ViewModels.Pages;

namespace VibrantbitLauncher.Views.Pages
{
    public partial class VersionManagePage : Page
    {
        public VersionManagePage()
        {
            InitializeComponent();

            // 列表内容变化时同步「空状态」提示（加载是异步的，光靠切标签会漏更新）
            if (DataContext is VersionManageViewModel vm)
            {
                vm.Mods.CollectionChanged += (_, _) => UpdateEmptyHint();
                vm.Saves.CollectionChanged += (_, _) => UpdateEmptyHint();
            }

            ShowMods();
        }

        private void ModsTab_Click(object sender, RoutedEventArgs e) => ShowMods();

        private void SavesTab_Click(object sender, RoutedEventArgs e) => ShowSaves();

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as VersionManageViewModel)?.OpenGameFolderCommand.Execute(null);
        }

        private void ShowMods()
        {
            ModsList.Visibility = Visibility.Visible;
            ModsToolbar.Visibility = Visibility.Visible;
            SavesList.Visibility = Visibility.Collapsed;
            SavesToolbar.Visibility = Visibility.Collapsed;

            ModsTab.Appearance = Wpf.Ui.Controls.ControlAppearance.Primary;
            SavesTab.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;

            UpdateEmptyHint();
        }

        private void ShowSaves()
        {
            ModsList.Visibility = Visibility.Collapsed;
            ModsToolbar.Visibility = Visibility.Collapsed;
            SavesList.Visibility = Visibility.Visible;
            SavesToolbar.Visibility = Visibility.Visible;

            ModsTab.Appearance = Wpf.Ui.Controls.ControlAppearance.Secondary;
            SavesTab.Appearance = Wpf.Ui.Controls.ControlAppearance.Primary;

            UpdateEmptyHint();
        }

        private void UpdateEmptyHint()
        {
            var showingMods = ModsList.Visibility == Visibility.Visible;
            var count = showingMods ? ModsList.Items.Count : SavesList.Items.Count;

            EmptyHint.Text = showingMods ? "没有找到模组" : "没有找到存档";
            EmptyHint.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
