using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Yaromir_Firewall_FINAL1
{
    public partial class MainWindow : Window
    {
        private int _themeState;
        private DispatcherTimer? _statusTimer;

        public bool AllowExit { get; set; } = false;

        public MainWindow()
        {
            InitializeComponent();

            Localization.LanguageChanged += OnLanguageChanged;

            _themeState = SettingsManager.Instance.Theme;
            ApplyTheme(_themeState);
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

        /// <summary>
        /// Переключает тему по кругу И СОХРАНЯЕТ выбор.
        ///
        /// БЫЛО: тема применялась только к открытому окну и не записывалась в настройки —
        /// после перезапуска всё откатывалось. Хуже того, если пользователь выбрал в
        /// настройках Gold/Silver/Neon, первое же нажатие на кнопку сбрасывало его на
        /// Light: цикл был жёстко зашит на (0,1,2), а разблокированные темы в него
        /// не входили.
        /// </summary>
        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            var available = AvailableThemes();

            int index = available.IndexOf(_themeState);
            if (index < 0) index = -1;          // тема недоступна (например, достижения сброшены)

            _themeState = available[(index + 1) % available.Count];
            ApplyTheme(_themeState);

            var settings = SettingsManager.Instance;
            settings.Theme = _themeState;
            settings.Save();
        }

        /// <summary>
        /// Темы, доступные пользователю: базовые + разблокированные достижениями.
        /// Список совпадает с тем, что показывает SettingsWindow.LoadThemeComboBoxItems.
        /// </summary>
        private static List<int> AvailableThemes()
        {
            var list = new List<int> { 0, 1, 2, 3 };   // Light, Dark, System, Neon

            if (AchievementFlags.Get(AchievementFlags.Version1_0)) list.Add(4);   // Gold
            if (AchievementFlags.Get(AchievementFlags.Version2_0)) list.Add(5);   // Silver

            return list;
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
                // Debug.WriteLine в релизной сборке не виден никому — ошибка темы
                // исчезала бесследно. Теперь она попадает в общий лог.
                AppLog.Error("Применение темы", ex);
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