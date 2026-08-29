using System.Windows;
using Microsoft.Win32;

namespace Yaromir_Firewall_FINAL1
{
    public partial class WhiteListWindow : Window
    {
        private SettingsManager _settings;

        public WhiteListWindow()
        {
            InitializeComponent();
            _settings = SettingsManager.Instance;
            RefreshList();
        }

        private void RefreshList()
        {
            ItemsList.Items.Clear();
            foreach (var item in _settings.WhiteList)
                ItemsList.Items.Add(item);
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Исполняемые файлы (*.exe)|*.exe|Все файлы (*.*)|*.*";
            dialog.Title = "Выберите программу для добавления в белый список";

            if (dialog.ShowDialog(this) == true)
            {
                var name = System.IO.Path.GetFileName(dialog.FileName);
                if (!_settings.WhiteList.Contains(name))
                {
                    _settings.WhiteList.Add(name);
                    _settings.Save();
                    RefreshList();
                }
            }
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (ItemsList.SelectedItem != null)
            {
                var name = ItemsList.SelectedItem.ToString();
                _settings.WhiteList.Remove(name);
                _settings.Save();
                RefreshList();
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Очистить весь белый список?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _settings.WhiteList.Clear();
                _settings.Save();
                RefreshList();
            }
        }
    }
}