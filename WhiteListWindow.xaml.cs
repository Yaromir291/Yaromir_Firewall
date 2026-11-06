using System;
using System.Windows;

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
            foreach (var item in _settings.WhiteListCopy())
                ItemsList.Items.Add(item);
        }

        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = Localization.T("DialogExeFilter"),
                Title = Localization.T("DialogTitleWhite")
            };

            if (dialog.ShowDialog(this) != true)
                return;

            string name = System.IO.Path.GetFileName(dialog.FileName);

            if (_settings.IsWhiteListed(name))
                return;

            AddButton.IsEnabled = false;
            try
            {
                // Если программа была в чёрном списке — снимаем её правила.
                // Await обязателен: раньше UnblockProgram вызывался синхронно и
                // подвешивал окно на время работы netsh.
                if (_settings.IsBlackListed(name))
                {
                    var result = await FirewallService.Instance.UnblockProgramAsync(name, killRunning: false);
                    if (!result.Ok)
                        ShowRuleError(name, result.FailText);
                }

                _settings.AddToWhiteList(name);
            }
            catch (Exception ex)
            {
                AppLog.Error($"Добавление {name} в белый список", ex);
                ShowRuleError(name, ex.Message);
            }
            finally
            {
                AddButton.IsEnabled = true;
                RefreshList();
            }
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            string? name = ItemsList.SelectedItem?.ToString();
            if (name == null)
                return;

            _settings.RemoveFromWhiteList(name);
            RefreshList();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Localization.T("ConfirmClearWhite"), Localization.T("Confirmation"),
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                _settings.ClearWhiteList();
            }
            catch (Exception ex)
            {
                AppLog.Error("Очистка белого списка", ex);
                ShowRuleError("", ex.Message);
            }
            finally
            {
                RefreshList();
            }
        }

        private void ShowRuleError(string name, string reason)
        {
            MessageBox.Show(this, Localization.T("RuleApplyFailed", name, reason),
                Localization.T("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        protected override void OnClosed(EventArgs e)
        {
            Localization.LanguageChanged -= OnLanguageChanged;
            base.OnClosed(e);
        }
    }
}