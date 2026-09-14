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

            UpdateLanguageFromService();

            _timer = new DispatcherTimer();
            _timer.Tick += (s, e) => RefreshData();
            _timer.Interval = TimeSpan.FromSeconds(_refreshSeconds);
            _timer.Start();

            RefreshData();
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

        public void UpdateLanguageFromService()
        {
            Title = LanguageService.Instance.Get("MonitorWindow_Title");
            
            // Обновляем заголовки колонок DataGrid
            var columns = ConnectionsGrid.Columns;
            if (columns.Count >= 6)
            {
                columns[0].Header = LanguageService.Instance.Get("MonitorWindow_Column_ProcessName");
                columns[1].Header = LanguageService.Instance.Get("MonitorWindow_Column_PID");
                columns[2].Header = LanguageService.Instance.Get("MonitorWindow_Column_LocalPort");
                columns[3].Header = LanguageService.Instance.Get("MonitorWindow_Column_RemoteAddress");
                columns[4].Header = LanguageService.Instance.Get("MonitorWindow_Column_Protocol");
                columns[5].Header = LanguageService.Instance.Get("MonitorWindow_Column_Status");
            }

            // Обновляем кнопку назад
            var backBtn = FindName("BackButton") as System.Windows.Controls.Button;
            if (backBtn != null)
            {
                backBtn.Content = LanguageService.Instance.Get("MonitorWindow_BackButton");
                backBtn.ToolTip = LanguageService.Instance.Get("MonitorWindow_BackButton_ToolTip");
            }

            // Обновляем label и ComboBox
            var refreshLabel = FindName("RefreshRateLabel") as System.Windows.Controls.TextBlock;
            if (refreshLabel != null)
            {
                refreshLabel.Text = LanguageService.Instance.Get("MonitorWindow_RefreshRateLabel");
            }

            if (RefreshRateCombo.Items.Count >= 3)
            {
                var item0 = RefreshRateCombo.Items[0] as System.Windows.Controls.ComboBoxItem;
                var item1 = RefreshRateCombo.Items[1] as System.Windows.Controls.ComboBoxItem;
                var item2 = RefreshRateCombo.Items[2] as System.Windows.Controls.ComboBoxItem;
                
                if (item0 != null) item0.Content = LanguageService.Instance.Get("MonitorWindow_RefreshRate_Fast");
                if (item1 != null) item1.Content = LanguageService.Instance.Get("MonitorWindow_RefreshRate_Moderate");
                if (item2 != null) item2.Content = LanguageService.Instance.Get("MonitorWindow_RefreshRate_Slow");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer?.Stop();
            base.OnClosed(e);
        }
    }
}