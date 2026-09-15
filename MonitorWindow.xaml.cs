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

            // Устанавливаем выбранный элемент по умолчанию (после инициализации)
            RefreshRateCombo.SelectedIndex = 1; // "Умеренно (5 сек)"

            _timer = new DispatcherTimer();
            _timer.Tick += (s, e) => RefreshData();
            _timer.Interval = TimeSpan.FromSeconds(_refreshSeconds);
            _timer.Start();

            RefreshData();
        }

        public void UpdateLanguageFromService()
        {
            // Обновление текста для MonitorWindow
            var lang = LanguageService.Instance;
            this.Title = lang.GetResource("MonitorWindow_Title");
            BackButton.ToolTip = lang.GetResource("MonitorWindow_BackButton_ToolTip");
            RefreshRateLabel.Text = lang.GetResource("MonitorWindow_RefreshRateLabel");
            
            // Обновление элементов ComboBox
            FastOption.Content = lang.GetResource("MonitorWindow_RefreshRate_Fast");
            ModerateOption.Content = lang.GetResource("MonitorWindow_RefreshRate_Moderate");
            SlowOption.Content = lang.GetResource("MonitorWindow_RefreshRate_Slow");
            
            // Обновление заголовков колонок
            if (ConnectionsGrid.Columns.Count >= 5)
            {
                ConnectionsGrid.Columns[0].Header = lang.GetResource("MonitorWindow_Column_ProcessName");
                ConnectionsGrid.Columns[1].Header = lang.GetResource("MonitorWindow_Column_PID");
                ConnectionsGrid.Columns[2].Header = lang.GetResource("MonitorWindow_Column_LocalPort");
                ConnectionsGrid.Columns[3].Header = lang.GetResource("MonitorWindow_Column_RemoteAddress");
                ConnectionsGrid.Columns[4].Header = lang.GetResource("MonitorWindow_Column_Protocol");
                ConnectionsGrid.Columns[5].Header = lang.GetResource("MonitorWindow_Column_Status");
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
            // Проверяем, что ComboBox и выбранный элемент существуют
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
            base.OnClosed(e);
        }
    }
}