using System;
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
            Title = Localization.T("WhiteList");
            AddButton.Content = Localization.T("Add");
            RemoveButton.Content = Localization.T("Remove");
            ClearButton.Content = Localization.T("ClearAll");
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
            dialog.Filter = Localization.T("DialogExeFilter");
            dialog.Title = Localization.T("DialogTitleWhite");

            if (dialog.ShowDialog(this) == true)
            {
                var name = System.IO.Path.GetFileName(dialog.FileName);
                if (!_settings.WhiteList.Contains(name))
                {
                    if (_settings.BlackList.Contains(name))
                    {
                        FirewallService.Instance.UnblockProgram(name, killRunning: false);
                    }

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
                if (name != null)
                {
                    _settings.WhiteList.Remove(name);
                    _settings.Save();
                    RefreshList();
                }
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Localization.T("ConfirmClearWhite"), Localization.T("Confirmation"),
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _settings.WhiteList.Clear();
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