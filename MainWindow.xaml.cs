using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Yaromir_Firewall_FINAL1
{
    public partial class MainWindow : Window
    {
        private int _themeState = 2;
        private DispatcherTimer? _statusTimer;

        public bool AllowExit { get; set; } = false;

        public MainWindow()
        {
            InitializeComponent();

            Localization.LanguageChanged += OnLanguageChanged;

            ApplyTheme(SettingsManager.Instance.Theme);
            RefreshLocalization();

            _statusTimer = new DispatcherTimer();
            _statusTimer.Interval = TimeSpan.FromSeconds(5);
            _statusTimer.Tick += (s, e) => UpdateStatus();
            _statusTimer.Start();
        }

        private void OnLanguageChanged()
        {
            Dispatcher.Invoke(RefreshLocalization);
        }

        private void RefreshLocalization()
        {
            bool ru = Localization.IsRussian;

            ThemeButton.ToolTip = Localization.T("TooltipTheme");
            LangButton.ToolTip = Localization.T("TooltipLang");
            LangButton.Content = ru ? "🇷🇺" : "🇬🇧";
            AboutButton.ToolTip = Localization.T("TooltipAbout");
            SettingsButton.ToolTip = Localization.T("TooltipSettings");
            AchievementsButton.ToolTip = Localization.T("TooltipAchievements");

            OpenMonitorButton.Content = Localization.T("OpenMonitor");
            WhiteListButton.Content = Localization.T("WhiteList");
            BlackListButton.Content = Localization.T("BlackList");
            LicenseLine.Text = Localization.T("LicenseLine");

            UpdateStatus();
        }

        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            int current = _themeState;
            if (current > 2) current = 0;
            _themeState = (current + 1) % 3;
            ApplyTheme(_themeState);
        }

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
                        iconText = "🖥"; break;
                    case 3: themeName = "NeonTheme"; iconText = "💡"; break;
                    case 4: themeName = "GoldTheme"; iconText = "⭐"; break;
                    case 5: themeName = "SilverTheme"; iconText = "🥈"; break;
                    default: themeName = "LightTheme"; iconText = "☀️"; break;
                }

                ResourceDictionary? themeDict = null;
                try { themeDict = Application.Current.FindResource(themeName) as ResourceDictionary; } catch { }

                if (themeDict != null)
                {
                    Application.Current.Resources.Remove("BackgroundImage");
                    foreach (var key in themeDict.Keys)
                    {
                        Application.Current.Resources[key] = themeDict[key];
                    }
                }

                if (state == 0 || state == 1 || state == 2)
                {
                    var brush = Application.Current.Resources["BackgroundBrush"] as Brush;
                    if (brush != null) this.Background = brush;
                }
                else
                {
                    var img = Application.Current.Resources["BackgroundImage"] as ImageBrush;
                    var brush = Application.Current.Resources["BackgroundBrush"] as Brush;
                    this.Background = img ?? brush;
                }

                if (state <= 2) ThemeButton.Content = iconText;
                else ThemeButton.Content = "🖥";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка темы: {ex.Message}");
            }
        }

        private void LangButton_Click(object sender, RoutedEventArgs e)
        {
            Localization.SetLanguage(!Localization.IsRussian);
            SettingsManager.Instance.IsRussian = Localization.IsRussian;
            SettingsManager.Instance.Save();
        }

        private void UpdateStatus()
        {
            try
            {
                int blockRules = FirewallService.Instance.GetRuleCount();
                int whiteListCount = SettingsManager.Instance.WhiteList.Count;
                int total = blockRules + whiteListCount;
                StatusText.Text = Localization.T("StatusRules", total);
            }
            catch
            {
                StatusText.Text = Localization.T("StatusRules", 0);
            }
        }

        private void OpenMonitor_Click(object sender, RoutedEventArgs e)
        {
            var w = new MonitorWindow { Owner = this };
            w.Show();
        }

        private void OpenWhiteList_Click(object sender, RoutedEventArgs e)
        {
            var w = new WhiteListWindow { Owner = this };
            w.Show();
        }

        private void OpenBlackList_Click(object sender, RoutedEventArgs e)
        {
            var w = new BlackListWindow { Owner = this };
            w.Show();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (AllowExit) return;
            e.Cancel = true;
            Hide();
        }

        private void OpenAbout_Click(object sender, RoutedEventArgs e)
        {
            var w = new AboutWindow { Owner = this };
            w.ShowDialog();
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            var w = new SettingsWindow { Owner = this };
            w.ShowDialog();
        }

        private void OpenAchievements_Click(object sender, RoutedEventArgs e)
        {
            var w = new AchievementsWindow { Owner = this };
            w.ShowDialog();
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
                        if (value != null) return Convert.ToInt32(value) == 1;
                    }
                }
            }
            catch { }
            return true;
        }
    }
}