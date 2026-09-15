using System;
using System.Windows;
using System.Windows.Controls;
using Yaromir_Firewall_FINAL1.Services;

namespace Yaromir_Firewall_FINAL1
{
    public partial class SettingsWindow : Window
    {
        private readonly SettingsManager _settings = SettingsManager.Instance;
        private readonly LanguageService _languageService = LanguageService.Instance;

        public SettingsWindow(Window owner)
        {
            InitializeComponent();
            Owner = owner;
            LoadSettings();
            UpdateLanguageFromService();
        }

        private void LoadSettings()
        {
            // Load language
            LanguageComboBox.SelectedItem = null;
            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if (item.Tag?.ToString() == _languageService.CurrentLanguage)
                {
                    LanguageComboBox.SelectedItem = item;
                    break;
                }
            }

            // Load theme
            ThemeComboBox.SelectedItem = null;
            foreach (ComboBoxItem item in ThemeComboBox.Items)
            {
                if (item.Tag?.ToString() == _settings.ThemeIndex.ToString())
                {
                    ThemeComboBox.SelectedItem = item;
                    break;
                }
            }

            // Load auto-start
            AutoStartCheckBox.IsChecked = _settings.AutoStart;
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string lang = selectedItem.Tag?.ToString() ?? "ru";
                _languageService.SetLanguage(lang);
                UpdateLanguageFromService();
            }
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                if (int.TryParse(selectedItem.Tag?.ToString(), out int themeIndex))
                {
                    _settings.ThemeIndex = themeIndex;
                    _settings.Save();
                    App.ApplyTheme(themeIndex);
                }
            }
        }

        private void AutoStartCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _settings.AutoStart = true;
            _settings.Save();
            App.SetAutoStart(true);
        }

        private void AutoStartCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _settings.AutoStart = false;
            _settings.Save();
            App.SetAutoStart(false);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void UpdateLanguageFromService()
        {
            string lang = _languageService.CurrentLanguage;
            
            Title = lang == "ru" ? "Настройки" : "Settings";
            LanguageLabel.Text = lang == "ru" ? "Язык:" : "Language:";
            ThemeLabel.Text = lang == "ru" ? "Тема:" : "Theme:";
            AutoStartCheckBox.Content = lang == "ru" 
                ? "Автозапуск при старте системы" 
                : "Run at startup";
            InfoLabel.Text = lang == "ru" 
                ? "Настройки сохраняются автоматически." 
                : "Settings will be saved automatically.";
            
            // Update button content
            if (this.FindName("CloseButton") is Button closeButton)
            {
                closeButton.Content = lang == "ru" ? "Закрыть" : "Close";
            }
        }
    }
}
