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

            Localization.LanguageChanged += OnLanguageChanged;

            var settings = SettingsManager.Instance;

            // Загружаем темы (с учётом флага HadVersion1_0)
            LoadThemeComboBoxItems();

            foreach (ComboBoxItem item in ThemeComboBox.Items)
            {
                if (int.TryParse(item.Tag?.ToString(), out int tagValue) && tagValue == settings.Theme)
                {
                    ThemeComboBox.SelectedItem = item;
                    break;
                }
            }

            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if (bool.TryParse(item.Tag?.ToString(), out bool tagValue) && tagValue == settings.IsRussian)
                {
                    LanguageComboBox.SelectedItem = item;
                    break;
                }
            }

            RefreshProgramsList();
            RefreshLocalization();
        }

        private void OnLanguageChanged()
        {
            Dispatcher.Invoke(() =>
            {
                RefreshComboBoxItems();
                RefreshLocalization();
            });
        }

        /// <summary>
        /// Загружает ComboBoxItem-ы тем с учётом HadVersion1_0.
        /// Золотая тема (Tag=4) добавляется только если пользователь скачивал 1.0.
        /// </summary>
        private void LoadThemeComboBoxItems()
        {
            ThemeComboBox.Items.Clear();

            ThemeComboBox.Items.Add(new ComboBoxItem { Tag = "0" }); // Light
            ThemeComboBox.Items.Add(new ComboBoxItem { Tag = "1" }); // Dark
            ThemeComboBox.Items.Add(new ComboBoxItem { Tag = "2" }); // System
            ThemeComboBox.Items.Add(new ComboBoxItem { Tag = "3" }); // Neon

            // Золотая — только для тех, у кого есть флаг HadVersion1_0
            if (SettingsManager.Instance.HadVersion1_0)
            {
                ThemeComboBox.Items.Add(new ComboBoxItem { Tag = "4" }); // Gold
            }

            RefreshComboBoxItems();
        }

        private void RefreshComboBoxItems()
        {
            if (ThemeComboBox.Items.Count >= 4)
            {
                ((ComboBoxItem)ThemeComboBox.Items[0]).Content = Localization.T("ThemeLight");
                ((ComboBoxItem)ThemeComboBox.Items[1]).Content = Localization.T("ThemeDark");
                ((ComboBoxItem)ThemeComboBox.Items[2]).Content = Localization.T("ThemeSystem");
                ((ComboBoxItem)ThemeComboBox.Items[3]).Content = Localization.T("ThemeNeon");
            }

            if (ThemeComboBox.Items.Count >= 5)
            {
                ((ComboBoxItem)ThemeComboBox.Items[4]).Content = Localization.T("ThemeGold");
            }

            if (LanguageComboBox.Items.Count >= 2)
            {
                ((ComboBoxItem)LanguageComboBox.Items[0]).Content = Localization.T("LangRussian");
                ((ComboBoxItem)LanguageComboBox.Items[1]).Content = Localization.T("LangEnglish");
            }
        }

        private void RefreshLocalization()
        {
            Title = Localization.T("Settings");
            HeaderText.Text = Localization.T("Settings");
            ThemeLabel.Text = Localization.T("ThemeLabel");
            LanguageLabel.Text = Localization.T("LanguageLabel");
            PortRulesTitle.Text = Localization.T("PortRulesTitle");
            ProgramsLabel.Text = Localization.T("ProgramsLabel");
            AllowedPortsLabel.Text = Localization.T("AllowedPortsLabel");
            BlockedPortsLabel.Text = Localization.T("BlockedPortsLabel");
            PortInputLabel.Text = Localization.T("PortInputLabel");
            AddProgramButton.Content = Localization.T("Add");
            RemoveProgramButton.Content = Localization.T("Remove");
            AllowPortButton.Content = Localization.T("Allow");
            BlockPortButton.Content = Localization.T("Block");
            RemovePortButton.Content = Localization.T("Remove");
            SaveButton.Content = Localization.T("Save");
            NewProgramTextBox.ToolTip = Localization.T("NewProgramTooltip");
            PortTextBox.ToolTip = Localization.T("PortTooltip");
        }

        private void RefreshProgramsList()
        {
            var settings = SettingsManager.Instance;
            ProgramsListBox.Items.Clear();
            foreach (var rule in settings.ProgramPortRules)
                ProgramsListBox.Items.Add(rule.ProgramName);
        }

        private void ProgramsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshPortsLists();

        private void RefreshPortsLists()
        {
            AllowedPortsListBox.Items.Clear();
            BlockedPortsListBox.Items.Clear();

            if (ProgramsListBox.SelectedItem == null) return;

            string programName = ProgramsListBox.SelectedItem.ToString() ?? "";
            var settings = SettingsManager.Instance;
            var rule = settings.ProgramPortRules.FirstOrDefault(r => r.ProgramName == programName);
            if (rule == null) return;

            foreach (int port in rule.AllowedPorts.OrderBy(p => p))
                AllowedPortsListBox.Items.Add(port);
            foreach (int port in rule.BlockedPorts.OrderBy(p => p))
                BlockedPortsListBox.Items.Add(port);
        }

        private void AddProgram_Click(object sender, RoutedEventArgs e)
        {
            string name = NewProgramTextBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show(Localization.T("ErrEnterProgram"), Localization.T("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var settings = SettingsManager.Instance;
            if (settings.ProgramPortRules.Any(r => r.ProgramName.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show(Localization.T("ErrProgramExists"), Localization.T("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
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
                MessageBox.Show(Localization.T("ErrSelectProgramRemove"), Localization.T("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string programName = ProgramsListBox.SelectedItem.ToString() ?? "";
            var settings = SettingsManager.Instance;
            var rule = settings.ProgramPortRules.FirstOrDefault(r => r.ProgramName == programName);

            if (rule != null)
            {
                var result = MessageBox.Show(
                    Localization.T("ConfirmRemoveProgram", programName),
                    Localization.T("Confirmation"),
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    settings.ProgramPortRules.Remove(rule);
                    RefreshProgramsList();
                    AllowedPortsListBox.Items.Clear();
                    BlockedPortsListBox.Items.Clear();
                }
            }
        }

        private void AllowPort_Click(object sender, RoutedEventArgs e) => AddPortToRule(true);
        private void BlockPort_Click(object sender, RoutedEventArgs e) => AddPortToRule(false);

        private void AddPortToRule(bool allow)
        {
            if (ProgramsListBox.SelectedItem == null)
            {
                MessageBox.Show(Localization.T("ErrSelectProgramFirst"), Localization.T("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(PortTextBox.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show(Localization.T("ErrInvalidPort"), Localization.T("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string programName = ProgramsListBox.SelectedItem.ToString() ?? "";
            var settings = SettingsManager.Instance;
            var rule = settings.ProgramPortRules.FirstOrDefault(r => r.ProgramName == programName);
            if (rule == null) return;

            if (allow)
            {
                if (rule.BlockedPorts.Contains(port)) rule.BlockedPorts.Remove(port);
                if (!rule.AllowedPorts.Contains(port)) rule.AllowedPorts.Add(port);
            }
            else
            {
                if (rule.AllowedPorts.Contains(port)) rule.AllowedPorts.Remove(port);
                if (!rule.BlockedPorts.Contains(port)) rule.BlockedPorts.Add(port);
            }

            PortTextBox.Clear();
            RefreshPortsLists();
        }

        private void RemovePort_Click(object sender, RoutedEventArgs e)
        {
            if (ProgramsListBox.SelectedItem == null)
            {
                MessageBox.Show(Localization.T("ErrSelectProgramFirst"), Localization.T("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(PortTextBox.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show(Localization.T("ErrInvalidPort"), Localization.T("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string programName = ProgramsListBox.SelectedItem.ToString() ?? "";
            var settings = SettingsManager.Instance;
            var rule = settings.ProgramPortRules.FirstOrDefault(r => r.ProgramName == programName);
            if (rule == null) return;

            bool removed = false;
            if (rule.AllowedPorts.Contains(port)) { rule.AllowedPorts.Remove(port); removed = true; }
            if (rule.BlockedPorts.Contains(port)) { rule.BlockedPorts.Remove(port); removed = true; }

            if (removed)
            {
                PortTextBox.Clear();
                RefreshPortsLists();
            }
            else
            {
                MessageBox.Show(Localization.T("PortNotFound"), Localization.T("Information"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void PortTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var settings = SettingsManager.Instance;

            if (ThemeComboBox.SelectedItem is ComboBoxItem themeItem)
                if (int.TryParse(themeItem.Tag?.ToString(), out int themeValue))
                    settings.Theme = themeValue;

            if (LanguageComboBox.SelectedItem is ComboBoxItem langItem)
                if (bool.TryParse(langItem.Tag?.ToString(), out bool langValue))
                    settings.IsRussian = langValue;

            settings.Save();

            Localization.SetLanguage(settings.IsRussian);

            if (Application.Current.MainWindow is MainWindow mw)
                mw.ApplyTheme(settings.Theme);

            MessageBox.Show(Localization.T("SettingsSaved"), "Yaromir Firewall", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
    }
}