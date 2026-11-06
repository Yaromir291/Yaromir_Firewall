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

            // БЫЛО: GetProcessesByName("Yaromir_Firewall_FINAL1") — жёстко зашитое имя.
            // Переименуй .exe (или собери под другим именем) — и проверка перестанет
            // работать, разрешая два экземпляра, которые начнут драться за правила firewall.
            using (var self = System.Diagnostics.Process.GetCurrentProcess())
            {
                int selfId = self.Id;
                string selfName = self.ProcessName;

                bool alreadyRunning = false;
                foreach (var other in System.Diagnostics.Process.GetProcessesByName(selfName))
                {
                    using (other)
                    {
                        if (other.Id != selfId) { alreadyRunning = true; break; }
                    }
                }

                if (alreadyRunning)
                {
                    MessageBox.Show(Localization.T("AlreadyRunning"), "Yaromir Firewall", MessageBoxButton.OK, MessageBoxImage.Information);
                    Shutdown();
                    return;
                }
            }

            SettingsManager.Instance.Load();
            Localization.SetLanguage(SettingsManager.Instance.IsRussian);

            // Путь к иконке в папке с .exe
            string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");

            _notifyIcon = new TaskbarIcon();

            try
            {
                if (System.IO.File.Exists(iconPath))
                {
                    _notifyIcon.Icon = new System.Drawing.Icon(iconPath);
                }
                else
                {
                    string exePath;
                    using (var self = System.Diagnostics.Process.GetCurrentProcess())
                        exePath = self.MainModule?.FileName ?? "shell32.dll";

                    _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                }
            }
            catch (Exception ex)
            {
                AppLog.Error("Иконка в трее", ex);
            }

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

            // БЫЛО: значение в HKCU\...\Run записывалось безусловно при каждом старте,
            // а DeleteValue не встречался во всём проекте — отключить автозагрузку было
            // невозможно даже через удаление программы. Плюс ключ Run запускает процесс
            // БЕЗ повышения, а у нас requireAdministrator → UAC при каждом входе.
            //
            // СТАЛО: AutostartManager ставит задачу Планировщика с /RL HIGHEST
            // (стартует без запроса UAC) и умеет всё это снимать. Уважает настройку пользователя.
            // В фоне: schtasks.exe — отдельный процесс, незачем тормозить им запуск окна.
            System.Threading.Tasks.Task.Run(() => AutostartManager.Apply(SettingsManager.Instance.AutoStart));

            FirewallService.Instance.Start();
            NetworkMonitor.Instance.Start();

            AppLog.Ok($"Запуск Yaromir Firewall {SettingsManager.Instance.LastVersion}.");
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
            // Порядок важен: сначала снимаем правила (пока процесс ещё жив и у нас есть
            // права администратора), потом гасим трей. Если исключение в одном шаге
            // прервёт остальные, пользователь останется с заблокированным интернетом.
            try { NetworkMonitor.Instance.Stop(); }
            catch (Exception ex) { AppLog.Error("Остановка мониторинга", ex); }

            try { FirewallService.Instance.Stop(); }
            catch (Exception ex) { AppLog.Error("Снятие правил firewall", ex); }

            try
            {
                Localization.LanguageChanged -= RefreshTrayLocalization;
                _notifyIcon?.Dispose();
            }
            catch (Exception ex) { AppLog.Error("Закрытие трея", ex); }

            AppLog.Ok("Приложение завершено.");
            base.OnExit(e);
        }
    }
}