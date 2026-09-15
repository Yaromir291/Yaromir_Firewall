using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Yaromir_Firewall_FINAL1
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

            var settings = SettingsManager.Instance;

            // Тема
            foreach (ComboBoxItem item in ThemeComboBox.Items)
            {
                if (int.TryParse(item.Tag.ToString(), out int tagValue) && tagValue == settings.Theme)
                {
                    ThemeComboBox.SelectedItem = item;
                    break;
                }
            }

            // Язык
            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if (bool.TryParse(item.Tag.ToString(), out bool tagValue) && tagValue == settings.IsRussian)
                {
                    LanguageComboBox.SelectedItem = item;
                    break;
                }
            }

            // Загрузка программ
            RefreshProgramsList();
        }

        private void RefreshProgramsList()
        {
            var settings = SettingsManager.Instance;
            ProgramsListBox.Items.Clear();
            foreach (var rule in settings.ProgramPortRules)
            {
                ProgramsListBox.Items.Add(rule.ProgramName);
            }
        }

        private void ProgramsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshPortsLists();
        }

        private void RefreshPortsLists()
        {
            AllowedPortsListBox.Items.Clear();
            BlockedPortsListBox.Items.Clear();

            if (ProgramsListBox.SelectedItem == null) return;

            string programName = ProgramsListBox.SelectedItem.ToString();
            var settings = SettingsManager.Instance;
            var rule = settings.ProgramPortRules.FirstOrDefault(r => r.ProgramName == programName);

            if (rule == null) return;

            foreach (int port in rule.AllowedPorts.OrderBy(p => p))
            {
                AllowedPortsListBox.Items.Add(port);
            }

            foreach (int port in rule.BlockedPorts.OrderBy(p => p))
            {
                BlockedPortsListBox.Items.Add(port);
            }
        }

        private void AddProgram_Click(object sender, RoutedEventArgs e)
        {
            string name = NewProgramTextBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Введите имя программы!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var settings = SettingsManager.Instance;
            if (settings.ProgramPortRules.Any(r => r.ProgramName.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Такая программа уже есть в списке!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            settings.ProgramPortRules.Add(new ProgramPortRule { ProgramName = name });
            NewProgramTextBox.Clear();
            RefreshProgramsList();
        }

        private void RemoveProgram_Click(object sender, RoutedEventArgs e)
        {
            if (ProgramsListBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите программу для удаления!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string programName = ProgramsListBox.SelectedItem.ToString();
            var settings = SettingsManager.Instance;
            var rule = settings.ProgramPortRules.FirstOrDefault(r => r.ProgramName == programName);

            if (rule != null)
            {
                var result = MessageBox.Show($"Удалить программу \"{programName}\" и все её правила?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    settings.ProgramPortRules.Remove(rule);
                    RefreshProgramsList();
                    AllowedPortsListBox.Items.Clear();
                    BlockedPortsListBox.Items.Clear();
                }
            }
        }

        private void AllowPort_Click(object sender, RoutedEventArgs e)
        {
            AddPortToRule(true);
        }

        private void BlockPort_Click(object sender, RoutedEventArgs e)
        {
            AddPortToRule(false);
        }

        private void AddPortToRule(bool allow)
        {
            if (ProgramsListBox.SelectedItem == null)
            {
                MessageBox.Show("Сначала выберите программу!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(PortTextBox.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Введите корректный номер порта (1-65535)!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string programName = ProgramsListBox.SelectedItem.ToString();
            var settings = SettingsManager.Instance;
            var rule = settings.ProgramPortRules.FirstOrDefault(r => r.ProgramName == programName);

            if (rule == null) return;

            // Удаляем из противоположного списка, если есть
            if (allow)
            {
                if (rule.BlockedPorts.Contains(port))
                    rule.BlockedPorts.Remove(port);
                if (!rule.AllowedPorts.Contains(port))
                    rule.AllowedPorts.Add(port);
            }
            else
            {
                if (rule.AllowedPorts.Contains(port))
                    rule.AllowedPorts.Remove(port);
                if (!rule.BlockedPorts.Contains(port))
                    rule.BlockedPorts.Add(port);
            }

            PortTextBox.Clear();
            RefreshPortsLists();
        }

        private void RemovePort_Click(object sender, RoutedEventArgs e)
        {
            if (ProgramsListBox.SelectedItem == null)
            {
                MessageBox.Show("Сначала выберите программу!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(PortTextBox.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Введите корректный номер порта (1-65535)!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string programName = ProgramsListBox.SelectedItem.ToString();
            var settings = SettingsManager.Instance;
            var rule = settings.ProgramPortRules.FirstOrDefault(r => r.ProgramName == programName);

            if (rule == null) return;

            bool removed = false;
            if (rule.AllowedPorts.Contains(port))
            {
                rule.AllowedPorts.Remove(port);
                removed = true;
            }
            if (rule.BlockedPorts.Contains(port))
            {
                rule.BlockedPorts.Remove(port);
                removed = true;
            }

            if (removed)
            {
                PortTextBox.Clear();
                RefreshPortsLists();
            }
            else
            {
                MessageBox.Show("Такой порт не найден в правилах выбранной программы!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void PortTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем только цифры
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var settings = SettingsManager.Instance;

            if (ThemeComboBox.SelectedItem is ComboBoxItem themeItem)
            {
                if (int.TryParse(themeItem.Tag.ToString(), out int themeValue))
                    settings.Theme = themeValue;
            }

            if (LanguageComboBox.SelectedItem is ComboBoxItem langItem)
            {
                if (bool.TryParse(langItem.Tag.ToString(), out bool langValue))
                    settings.IsRussian = langValue;
            }

            settings.Save();

            // Обновляем тему и язык в главном окне
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.ApplyTheme(settings.Theme);
                mainWindow.SetLanguage(settings.IsRussian);
            }

            MessageBox.Show("Настройки сохранены!", "Yaromir Firewall", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
    }
}
