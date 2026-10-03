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
        }

        private void ModsTab_Click(object sender, RoutedEventArgs e)
        {
            ShowMods();
        }

        private void SavesTab_Click(object sender, RoutedEventArgs e)
        {
            ShowSaves();
        }

        private void ModToggled(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.DataContext is ModItem mod)
            {
                (DataContext as VersionManageViewModel)?.ToggleMod(mod);
            }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as VersionManageViewModel)?.OpenGameFolderCommand.Execute(null);
        }

        private void ShowMods()
        {
            ModsList.Visibility = Visibility.Visible;
            SavesList.Visibility = Visibility.Collapsed;
            EmptyHint.Text = "没有找到模组";
            EmptyHint.Visibility = ModsList.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ShowSaves()
        {
            ModsList.Visibility = Visibility.Collapsed;
            SavesList.Visibility = Visibility.Visible;
            EmptyHint.Text = "没有找到存档";
            EmptyHint.Visibility = SavesList.Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
