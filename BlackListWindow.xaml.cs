using System.Windows;
using Microsoft.Win32;

namespace Yaromir_Firewall_FINAL1
{
    public partial class BlackListWindow : Window
    {
        private SettingsManager _settings;

        public BlackListWindow()
        {
            InitializeComponent();
            _settings = SettingsManager.Instance;
            RefreshList();
        }

        private void RefreshList()
        {
            ItemsList.Items.Clear();
            foreach (var item in _settings.BlackList)
                ItemsList.Items.Add(item);
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Исполняемые файлы (*.exe)|*.exe|Все файлы (*.*)|*.*";
            dialog.Title = "Выберите программу для блокировки";

            if (dialog.ShowDialog(this) == true)
            {
                var fullPath = dialog.FileName;
                var name = System.IO.Path.GetFileName(fullPath);

                if (!_settings.BlackList.Contains(name))
                {
                    FirewallService.Instance.BlockProgram(name, killRunning: true);
                    RefreshList();
                }
            }
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (ItemsList.SelectedItem != null)
            {
                var name = ItemsList.SelectedItem.ToString();
                _settings.BlackList.Remove(name);
                _settings.Save();
                RefreshList();
                FirewallService.Instance.RemoveBlockRule(name);
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Очистить весь чёрный список?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _settings.BlackList.Clear();
                _settings.Save();
                RefreshList();
                FirewallService.Instance.RemoveAllBlockRules();
            }
        }
    }
}