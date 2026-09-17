using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Yaromir_Firewall_FINAL1
{
    public partial class MonitorWindow : Window
    {
        private DispatcherTimer _timer;
        private int _refreshSeconds = 5;

        public MonitorWindow()
        {
            InitializeComponent();

            Localization.LanguageChanged += OnLanguageChanged;

            RefreshRateCombo.SelectedIndex = 1;

            _timer = new DispatcherTimer();
            _timer.Tick += (s, e) => RefreshData();
            _timer.Interval = TimeSpan.FromSeconds(_refreshSeconds);
            _timer.Start();

            RefreshLocalization();
            RefreshData();
        }

        private void OnLanguageChanged()
        {
            Dispatcher.Invoke(() =>
            {
                RefreshLocalization();
                RefreshRateComboItems();
            });
        }

        private void RefreshLocalization()
        {
            Title = Localization.T("Monitoring");
            BackButton.ToolTip = Localization.T("Back");
            RefreshRateLabel.Text = Localization.T("RefreshRateLabel");

            ColProcess.Header = Localization.T("ColProcess");
            ColPid.Header = Localization.T("ColPid");
            ColLocalPort.Header = Localization.T("ColLocalPort");
            ColRemote.Header = Localization.T("ColRemote");
            ColProtocol.Header = Localization.T("ColProtocol");
            ColStatus.Header = Localization.T("ColStatus");

            RefreshRateComboItems();
        }

        private void RefreshRateComboItems()
        {
            if (RefreshRateCombo.Items.Count >= 3)
            {
                int savedIndex = RefreshRateCombo.SelectedIndex;
                ((ComboBoxItem)RefreshRateCombo.Items[0]).Content = Localization.T("RefreshFast");
                ((ComboBoxItem)RefreshRateCombo.Items[1]).Content = Localization.T("RefreshModerate");
                ((ComboBoxItem)RefreshRateCombo.Items[2]).Content = Localization.T("RefreshSlow");
                RefreshRateCombo.SelectedIndex = savedIndex;
            }
        }

        private void RefreshData()
        {
            try
            {
                ConnectionsGrid.ItemsSource = null;
                ConnectionsGrid.ItemsSource = NetworkMonitor.Instance.GetConnections();
            }
            catch { }
        }

        private void RefreshRate_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (RefreshRateCombo == null || RefreshRateCombo.SelectedItem == null)
                return;

            if (RefreshRateCombo.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                try
                {
                    _refreshSeconds = Convert.ToInt32(item.Tag);
                    _timer.Interval = TimeSpan.FromSeconds(_refreshSeconds);
                }
                catch { }
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer?.Stop();
            Localization.LanguageChanged -= OnLanguageChanged;
            base.OnClosed(e);
        }
    }
}