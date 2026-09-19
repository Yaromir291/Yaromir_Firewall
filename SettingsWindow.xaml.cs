using System;
using System.Diagnostics;
using System.IO;
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
        /// Загружает темы в ComboBox с учётом достижений:
        /// - 0: Light, 1: Dark, 2: System, 3: Neon — доступны всегда
        /// - 4: Gold — только если had_version_1_0 == true
        /// - 5: Silver — только если had_version_2_0 == true
        /// </summary>
        private void LoadThemeComboBoxItems()
        {
            ThemeComboBox.Items.Clear();

            ThemeComboBox.Items.Add(new ComboBoxItem { Tag = "0" }); // Light
            ThemeComboBox.Items.Add(new ComboBoxItem { Tag = "1" }); // Dark
            ThemeComboBox.Items.Add(new ComboBoxItem { Tag = "2" }); // System
            ThemeComboBox.Items.Add(new ComboBoxItem { Tag = "3" }); // Neon

            if (AchievementFlags.Get(AchievementFlags.Version1_0))
                ThemeComboBox.Items.Add(new ComboBoxItem { Tag = "4" }); // Gold

            if (AchievementFlags.Get(AchievementFlags.Version2_0))
                ThemeComboBox.Items.Add(new ComboBoxItem { Tag = "5" }); // Silver

            RefreshComboBoxItems();
        }

        private void RefreshComboBoxItems()
        {
            foreach (ComboBoxItem item in ThemeComboBox.Items)
            {
                switch (item.Tag?.ToString())
                {
                    case "0": item.Content = Localization.T("ThemeLight"); break;
                    case "1": item.Content = Localization.T("ThemeDark"); break;
                    case "2": item.Content = Localization.T("ThemeSystem"); break;
                    case "3": item.Content = Localization.T("ThemeNeon"); break;
                    case "4": item.Content = Localization.T("ThemeGold"); break;
                    case "5": item.Content = Localization.T("ThemeSilver"); break;
                }
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
            OpenLogButton.Content = Localization.T("OpenLog");
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

        private void ProgramsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshPortsLists();
        }

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

        // ===== Открыть лог =====
        private void OpenLog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "Yaromir_Firewall",
                    "firewall_log.txt");

                if (!File.Exists(logPath))
                {
                    MessageBox.Show(Localization.T("LogNotFound"), Localization.T("Information"), MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = logPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Localization.T("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
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