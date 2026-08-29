using System;
using System.Windows;
using Microsoft.Win32;
using Hardcodet.Wpf.TaskbarNotification;

namespace Yaromir_Firewall_FINAL1
{
    public partial class App : Application
    {
        private TaskbarIcon? _notifyIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (System.Diagnostics.Process.GetProcessesByName("Yaromir_Firewall_FINAL1").Length > 1)
            {
                MessageBox.Show("Программа уже запущена!", "Yaromir_Firewall_FINAL1", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            SettingsManager.Instance.Load();

            // Путь к иконке в папке с .exe
            string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");

            _notifyIcon = new TaskbarIcon();

            // Если иконка есть — загружаем, если нет — используем стандартную
            if (System.IO.File.Exists(iconPath))
            {
                _notifyIcon.Icon = new System.Drawing.Icon(iconPath);
            }
            else
            {
                // Используем стандартную иконку Windows
                _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                    System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "shell32.dll"
                );
            }

            _notifyIcon.ToolTipText = "Yaromir Firewall — защита подключений";
            _notifyIcon.ContextMenu = new System.Windows.Controls.ContextMenu();

            var openItem = new System.Windows.Controls.MenuItem { Header = "Открыть" };
            openItem.Click += (s, ev) => { MainWindow?.Show(); MainWindow?.Activate(); };
            _notifyIcon.ContextMenu.Items.Add(openItem);

            var exitItem = new System.Windows.Controls.MenuItem { Header = "Выход" };
            exitItem.Click += (s, ev) => { Shutdown(); };
            _notifyIcon.ContextMenu.Items.Add(exitItem);

            _notifyIcon.Visibility = Visibility.Visible;

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

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose();
            FirewallService.Instance.Stop();
            NetworkMonitor.Instance.Stop();
            base.OnExit(e);
        }
    }
}