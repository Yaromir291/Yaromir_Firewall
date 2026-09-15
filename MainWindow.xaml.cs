using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Yaromir_Firewall_FINAL1
{
    public partial class MainWindow : Window
    {
        private int _themeState = 2;
        private bool _isRussian = true;
        private DispatcherTimer? _statusTimer;

        /// <summary>
        /// true — разрешить реальное закрытие окна (выход через трей).
        /// false — крестик только сворачивает окно в трей.
        /// </summary>
        public bool AllowExit { get; set; } = false;

        public MainWindow()
        {
            InitializeComponent();

            // Язык берём из сохранённых настроек
            _isRussian = SettingsManager.Instance.IsRussian;

            ApplyTheme(SettingsManager.Instance.Theme);
            UpdateLanguage();

            _statusTimer = new DispatcherTimer();
            _statusTimer.Interval = TimeSpan.FromSeconds(5);
            _statusTimer.Tick += (s, e) => UpdateStatus();
            _statusTimer.Start();
        }

        // Кнопка темы — переключает только Светлая/Тёмная/Системная
        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            int current = _themeState;
            if (current > 2) current = 0;
            _themeState = (current + 1) % 3;
            ApplyTheme(_themeState);
        }

        // ПУБЛИЧНЫЙ МЕТОД — доступен из SettingsWindow
        public void ApplyTheme(int state)
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
                        themeName = SystemThemeHelper.GetSystemTheme() ? "LightTheme" : "DarkTheme";
                        iconText = "🖥";
                        break;
                    case 3: themeName = "NeonTheme"; iconText = "💡"; break;
                    case 4: themeName = "GoldTheme"; iconText = "⭐"; break;
                    default: themeName = "LightTheme"; iconText = "☀️"; break;
                }

                if (this.Resources[themeName] is ResourceDictionary themeDict)
                {
                    Application.Current.Resources.MergedDictionaries.Clear();
                    Application.Current.Resources.MergedDictionaries.Add(themeDict);

                    var bgImage = Application.Current.Resources["BackgroundImage"] as ImageBrush;
                    if (bgImage != null)
                        this.Background = bgImage;
                    else
                        this.Background = (System.Windows.Media.Brush)Application.Current.Resources["BackgroundBrush"];
                }

                if (state <= 2)
                    ThemeButton.Content = iconText;
                else
                    ThemeButton.Content = "🖥";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка темы: {ex.Message}");
            }
        }

        private void LangButton_Click(object sender, RoutedEventArgs e)
        {
            _isRussian = !_isRussian;
            // Сохраняем выбор языка, чтобы пережить перезапуск
            SettingsManager.Instance.IsRussian = _isRussian;
            SettingsManager.Instance.Save();
            UpdateLanguage();
        }

        // ПУБЛИЧНЫЙ МЕТОД — доступен из SettingsWindow
        public void SetLanguage(bool isRussian)
        {
            _isRussian = isRussian;
            UpdateLanguage();
        }

        private void UpdateLanguage()
        {
            LangButton.Content = _isRussian ? "🇷🇺" : "🇬🇧";
            LangButton.ToolTip = _isRussian ? "Русский" : "English";

            OpenMonitorButton.Content = _isRussian ? "Открыть мониторинг" : "Open Monitor";
            WhiteListButton.Content = _isRussian ? "Белый список" : "Whitelist";
            BlackListButton.Content = _isRussian ? "Чёрный список" : "Blacklist";

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

        // Крестик: не закрываем программу, а сворачиваем в трей
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (AllowExit) return; // выход через меню трея — закрываем по-настоящему

            e.Cancel = true;
            Hide();
        }

        private void OpenAbout_Click(object sender, RoutedEventArgs e)
        {
            var about = new AboutWindow();
            about.Owner = this;
            about.ShowDialog();
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            var settings = new SettingsWindow();
            settings.Owner = this;
            settings.ShowDialog();
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