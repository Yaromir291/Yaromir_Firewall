using System;
using System.Collections.Generic;
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

            Localization.LanguageChanged += OnLanguageChanged;

            RefreshLocalization();
            RefreshList();
        }

        private void OnLanguageChanged()
        {
            Dispatcher.Invoke(RefreshLocalization);
        }

        private void RefreshLocalization()
        {
            Title = Localization.T("BlackList");
            AddButton.Content = Localization.T("Add");
            RemoveButton.Content = Localization.T("Remove");
            ClearButton.Content = Localization.T("ClearAll");
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
            dialog.Filter = Localization.T("DialogExeFilter");
            dialog.Title = Localization.T("DialogTitleBlack");

            if (dialog.ShowDialog(this) == true)
            {
                var fullPath = dialog.FileName;
                var name = System.IO.Path.GetFileName(fullPath);

                if (!_settings.BlackList.Contains(name))
                {
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
                if (name != null)
                {
                    FirewallService.Instance.UnblockProgram(name, killRunning: true);
                    RefreshList();
                }
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Localization.T("ConfirmClearBlack"), Localization.T("Confirmation"),
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var toClear = new List<string>(_settings.BlackList);
                foreach (var name in toClear)
                {
                    FirewallService.Instance.UnblockProgram(name, killRunning: true);
                }

                _settings.BlackList.Clear();
                _settings.Save();
                RefreshList();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            Localization.LanguageChanged -= OnLanguageChanged;
            base.OnClosed(e);
        }
    }
}