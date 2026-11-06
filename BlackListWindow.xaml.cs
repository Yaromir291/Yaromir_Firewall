using System;
using System.Collections.Generic;
using System.Windows;

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
            foreach (var item in _settings.BlackListCopy())
                ItemsList.Items.Add(item);
        }

        /// <summary>
        /// Добавление в чёрный список.
        ///
        /// БЫЛО: синхронный вызов BlockProgram в UI-потоке. Внутри — поиск exe по диску
        /// и несколько запусков netsh: окно намертво висело (в худшем случае минуты),
        /// а если правило не применялось, пользователь об этом не узнавал вовсе.
        /// СТАЛО: await, кнопка блокируется на время операции, ошибка показывается в диалоге.
        /// </summary>
        private async void Add_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = Localization.T("DialogExeFilter"),
                Title = Localization.T("DialogTitleBlack")
            };

            if (dialog.ShowDialog(this) != true)
                return;

            string name = System.IO.Path.GetFileName(dialog.FileName);

            if (_settings.IsBlackListed(name))
                return;

            AddButton.IsEnabled = false;
            try
            {
                var result = await FirewallService.Instance.BlockProgramAsync(name, killRunning: true);

                if (!result.Ok)
                    ShowRuleError(name, result.FailText);
            }
            catch (Exception ex)
            {
                AppLog.Error($"Добавление {name} в чёрный список", ex);
                ShowRuleError(name, ex.Message);
            }
            finally
            {
                AddButton.IsEnabled = true;
                RefreshList();
            }
        }

        private async void Remove_Click(object sender, RoutedEventArgs e)
        {
            string? name = ItemsList.SelectedItem?.ToString();
            if (name == null)
                return;

            RemoveButton.IsEnabled = false;
            try
            {
                var result = await FirewallService.Instance.UnblockProgramAsync(name, killRunning: true);

                if (!result.Ok)
                    ShowRuleError(name, result.FailText);
            }
            catch (Exception ex)
            {
                AppLog.Error($"Удаление {name} из чёрного списка", ex);
                ShowRuleError(name, ex.Message);
            }
            finally
            {
                RemoveButton.IsEnabled = true;
                RefreshList();
            }
        }

        private async void Clear_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Localization.T("ConfirmClearBlack"), Localization.T("Confirmation"),
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var toClear = _settings.BlackListCopy();
            int failed = 0;

            ClearButton.IsEnabled = false;
            try
            {
                foreach (var name in toClear)
                {
                    var result = await FirewallService.Instance.UnblockProgramAsync(name, killRunning: true);
                    if (!result.Ok)
                    {
                        failed++;
                        AppLog.Warn($"Очистка: не удалось снять правило для {name} — {result.FailText}");
                    }
                }

                _settings.ClearBlackList();
            }
            catch (Exception ex)
            {
                AppLog.Error("Очистка чёрного списка", ex);
                failed = toClear.Count;
            }
            finally
            {
                ClearButton.IsEnabled = true;
                RefreshList();
            }

            if (failed > 0)
                MessageBox.Show(this, Localization.T("PartialFailure", failed),
                    Localization.T("Error"), MessageBoxButton.OK, MessageBoxImage.Warning);
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