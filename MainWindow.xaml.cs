using System;
using System.Windows;
using System.Windows.Threading;

namespace Yaromir_Firewall_FINAL1
{
    public partial class MainWindow : Window
    {
        private int _themeState = 2;
        private bool _isRussian = true;
        private DispatcherTimer? _statusTimer;

        public MainWindow()
        {
            InitializeComponent();

            ApplyTheme(2);
            UpdateLanguage();

            _statusTimer = new DispatcherTimer();
            _statusTimer.Interval = TimeSpan.FromSeconds(5);
            _statusTimer.Tick += (s, e) => UpdateStatus();
            _statusTimer.Start();
        }

        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            _themeState = (_themeState + 1) % 3;
            ApplyTheme(_themeState);
        }

        private void ApplyTheme(int state)
        {
            try
            {
                if (Application.Current == null) return;

                string themeName, iconText;
                switch (state)
                {
                    case 0: themeName = "LightTheme"; iconText = "☀️"; break;
                    case 1: themeName = "DarkTheme"; iconText = "🌙"; break;
                    default:
                        themeName = SystemThemeHelper.GetSystemTheme() ? "LightTheme" : "DarkTheme";
                        iconText = "🖥"; break;
                }

                if (this.Resources[themeName] is ResourceDictionary themeDict)
                {
                    Application.Current.Resources.MergedDictionaries.Clear();
                    Application.Current.Resources.MergedDictionaries.Add(themeDict);

                    this.Background = (System.Windows.Media.Brush)Application.Current.Resources["BackgroundBrush"];
                }

                ThemeButton.Content = iconText;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка темы: {ex.Message}");
            }
        }

        private void LangButton_Click(object sender, RoutedEventArgs e)
        {
            _isRussian = !_isRussian;
            UpdateLanguage();
        }

        private void UpdateLanguage()
        {
            LangButton.Content = _isRussian ? "🇷🇺" : "🇬🇧";
            LangButton.ToolTip = _isRussian ? "Русский" : "English";

            OpenMonitorButton.Content = _isRussian ? "Открыть мониторинг" : "Open Monitor";
            WhiteListButton.Content = _isRussian ? "Белый список" : "Whitelist";
            BlackListButton.Content = _isRussian ? "Чёрный список" : "Blacklist";
            MinimizeButton.Content = _isRussian ? "ТРЕЙ" : "TRAY";

            UpdateStatus();
        }

        private void UpdateStatus()
        {
            try
            {
                int blockRules = FirewallService.Instance.GetRuleCount();
                int whiteListCount = SettingsManager.Instance.WhiteList.Count;
                int total = blockRules + whiteListCount;

                StatusText.Text = _isRussian ? $"Активно правил: {total}" : $"Active rules: {total}";
            }
            catch
            {
                StatusText.Text = _isRussian ? "Активно правил: 0" : "Active rules: 0";
            }
        }

        private void OpenMonitor_Click(object sender, RoutedEventArgs e)
        {
            var monitor = new MonitorWindow();
            monitor.Owner = this;
            monitor.Show();
        }

        private void OpenWhiteList_Click(object sender, RoutedEventArgs e)
        {
            var wl = new WhiteListWindow();
            wl.Owner = this;
            wl.Show();
        }

        private void OpenBlackList_Click(object sender, RoutedEventArgs e)
        {
            var bl = new BlackListWindow();
            bl.Owner = this;
            bl.Show();
        }

        private void MinimizeToTray_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Hide();
        }

        // 👇 НОВЫЙ МЕТОД ДЛЯ КНОПКИ "О ПРОГРАММЕ"
        private void OpenAbout_Click(object sender, RoutedEventArgs e)
        {
            var about = new AboutWindow();
            about.Owner = this;
            about.ShowDialog();
        }
    }

    public static class SystemThemeHelper
    {
        public static bool GetSystemTheme()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("AppsUseLightTheme");
                        if (value != null)
                            return Convert.ToInt32(value) == 1;
                    }
                }
            }
            catch { }
            return true;
        }
    }
}