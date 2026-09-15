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
            UpdateLanguageFromService();
        }

        public void UpdateLanguageFromService()
        {
            var lang = LanguageService.Instance;
            this.Title = lang.GetResource("BlackListWindow_Title");
            AddButton.Content = lang.GetResource("BlackListWindow_AddButton");
            RemoveButton.Content = lang.GetResource("BlackListWindow_RemoveButton");
            ClearButton.Content = lang.GetResource("BlackListWindow_ClearButton");
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
                    // 🔥 КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ: если программа была в белом списке — убираем оттуда
                    if (_settings.WhiteList.Contains(name))
                    {
                        _settings.WhiteList.Remove(name);
                        _settings.Save();
                    }

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
                
                // 🔥 КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ: полностью разблокируем с убийством процесса
                if (!string.IsNullOrEmpty(name))
                {
                    FirewallService.Instance.UnblockProgram(name, killRunning: true);
                }
                
                RefreshList();
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Очистить весь чёрный список?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // 🔥 КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ: разблокируем каждую программу перед очисткой
                var toClear = new System.Collections.Generic.List<string>(_settings.BlackList);
                foreach (var name in toClear)
                {
                    FirewallService.Instance.UnblockProgram(name, killRunning: true);
                }
                
                _settings.BlackList.Clear();
                _settings.Save();
                RefreshList();
            }
        }
    }
}