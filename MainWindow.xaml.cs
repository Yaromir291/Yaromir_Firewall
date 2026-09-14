using System;
using System.Windows;
using System.Windows.Threading;

namespace Yaromir_Firewall_FINAL1
{
    public partial class MainWindow : Window
    {
        private int _themeState = 2;
        private DispatcherTimer? _statusTimer;

        public MainWindow()
        {
            InitializeComponent();

            ApplyTheme(2);
            UpdateLanguageFromService();

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
                    case 2: 
                        bool isLight = SystemThemeHelper.GetSystemTheme();
                        themeName = isLight ? "LightTheme" : "DarkTheme";
                        iconText = "🖥"; 
                        break;
                    default: themeName = "LightTheme"; iconText = "☀️"; break;
                }

                // Получаем словарь темы из ресурсов окна
                ResourceDictionary? themeDict = null;
                if (this.Resources[themeName] is ResourceDictionary)
                {
                    themeDict = this.Resources[themeName] as ResourceDictionary;
                }

                if (themeDict != null)
                {
                    Application.Current.Resources.MergedDictionaries.Clear();
                    Application.Current.Resources.MergedDictionaries.Add(themeDict);
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
            LanguageService.Instance.IsRussian = !LanguageService.Instance.IsRussian;
            SettingsManager.Instance.IsRussian = LanguageService.Instance.IsRussian;
            SettingsManager.Instance.Save();
        }

        public void UpdateLanguageFromService()
        {
            bool isRussian = LanguageService.Instance.IsRussian;

            LangButton.Content = isRussian ? "🇷🇺" : "🇬🇧";
            LangButton.ToolTip = LanguageService.Instance.Get(isRussian ? "MainWindow_LangButton_ToolTip_RU" : "MainWindow_LangButton_ToolTip_EN");
            ThemeButton.ToolTip = LanguageService.Instance.Get("MainWindow_ThemeButton_ToolTip");
            AboutButton.ToolTip = LanguageService.Instance.Get("MainWindow_AboutButton_ToolTip");

            OpenMonitorButton.Content = LanguageService.Instance.Get("MainWindow_OpenMonitorButton");
            WhiteListButton.Content = LanguageService.Instance.Get("MainWindow_WhiteListButton");
            BlackListButton.Content = LanguageService.Instance.Get("MainWindow_BlackListButton");
            MinimizeButton.Content = LanguageService.Instance.Get("MainWindow_MinimizeButton");

            Title = LanguageService.Instance.Get("MainWindow_Title");

            UpdateStatus();
        }

        private void UpdateStatus()
        {
            try
            {
                int blockRules = FirewallService.Instance.GetRuleCount();
                int whiteListCount = SettingsManager.Instance.WhiteList.Count;
                int total = blockRules + whiteListCount;

                StatusText.Text = LanguageService.Instance.Get("MainWindow_StatusText", total);
            }
            catch
            {
                StatusText.Text = LanguageService.Instance.Get("MainWindow_StatusText", 0);
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