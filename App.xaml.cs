using System;
using System.Windows;
using Microsoft.Win32;
using Hardcodet.Wpf.TaskbarNotification;

namespace Yaromir_Firewall_FINAL1
{
    public partial class App : Application
    {
        private TaskbarIcon? _notifyIcon;
        private System.Windows.Controls.MenuItem? _openItem;
        private System.Windows.Controls.MenuItem? _exitItem;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (System.Diagnostics.Process.GetProcessesByName("Yaromir_Firewall_FINAL1").Length > 1)
            {
                MessageBox.Show(Localization.T("AlreadyRunning"), "Yaromir_Firewall_FINAL1", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            SettingsManager.Instance.Load();
            Localization.SetLanguage(SettingsManager.Instance.IsRussian);

            // Путь к иконке в папке с .exe
            string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");

            _notifyIcon = new TaskbarIcon();

            if (System.IO.File.Exists(iconPath))
                _notifyIcon.Icon = new System.Drawing.Icon(iconPath);
            else
                _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                    System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "shell32.dll"
                );

            _notifyIcon.ContextMenu = new System.Windows.Controls.ContextMenu();

            _openItem = new System.Windows.Controls.MenuItem();
            _openItem.Click += (s, ev) => { MainWindow?.Show(); MainWindow?.Activate(); };
            _notifyIcon.ContextMenu.Items.Add(_openItem);

            _exitItem = new System.Windows.Controls.MenuItem();
            _exitItem.Click += (s, ev) =>
            {
                if (MainWindow is MainWindow mw)
                    mw.AllowExit = true;
                Shutdown();
            };
            _notifyIcon.ContextMenu.Items.Add(_exitItem);

            _notifyIcon.Visibility = Visibility.Visible;

            // Локализация трея
            Localization.LanguageChanged += RefreshTrayLocalization;
            RefreshTrayLocalization();

            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (key?.GetValue("Yaromir_Firewall_FINAL1") == null)
                        key?.SetValue("Yaromir_Firewall_FINAL1", System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "Yaromir_Firewall_FINAL1.exe");
                }
            }
            catch { }

            FirewallService.Instance.Start();
            NetworkMonitor.Instance.Start();
        }

        private void RefreshTrayLocalization()
        {
            if (_notifyIcon == null) return;
            _notifyIcon.ToolTipText = Localization.T("TrayTooltip");
            if (_openItem != null) _openItem.Header = Localization.T("TrayOpen");
            if (_exitItem != null) _exitItem.Header = Localization.T("TrayExit");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Localization.LanguageChanged -= RefreshTrayLocalization;
            _notifyIcon?.Dispose();
            FirewallService.Instance.Stop();
            NetworkMonitor.Instance.Stop();
            base.OnExit(e);
        }
    }
}