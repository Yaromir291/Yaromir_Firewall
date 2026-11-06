using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace Yaromir_Firewall_FINAL1
{
    /// <summary>
    /// Автозагрузка. Управляется пользователем из настроек, а не прописывается насильно.
    ///
    /// БЫЛО (App.xaml.cs): при КАЖДОМ старте значение в HKCU\...\Run записывалось
    /// безусловно, `DeleteValue` не встречалось нигде в проекте — то есть отключить
    /// автозагрузку было невозможно в принципе, даже удалив программу.
    ///
    /// Вторая проблема: приложение требует администратора (requireAdministrator),
    /// а ключ Run запускает программы БЕЗ повышения — Windows либо покажет UAC
    /// при каждом входе, либо молча пропустит запуск. Поэтому основной механизм —
    /// задача Планировщика с /RL HIGHEST: она стартует уже повышенный процесс
    /// и не дёргает UAC. Ключ Run оставлен только как запасной вариант.
    /// </summary>
    public static class AutostartManager
    {
        private const string TaskName = "YaromirFirewall_AutoStart";
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "Yaromir_Firewall_FINAL1";   // как в версии 2.0 — для миграции

        /// <summary>Включена ли автозагрузка фактически (задача ИЛИ ключ Run).</summary>
        public static bool IsEnabled()
        {
            return TaskExists() || RunKeyExists();
        }

        /// <summary>
        /// Приводит систему к нужному состоянию. Идемпотентно: можно звать при каждом старте.
        /// </summary>
        public static void Apply(bool enable)
        {
            try
            {
                string exe = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (string.IsNullOrEmpty(exe))
                {
                    AppLog.Warn("Автозагрузка: не удалось определить путь к .exe");
                    return;
                }

                if (enable)
                {
                    // Apply() вызывается при КАЖДОМ старте, поэтому сначала проверяем,
                    // не настроено ли уже всё. Иначе на каждый запуск приходилось бы
                    // 2–3 вызова schtasks.exe (сотни миллисекунд).
                    if (TaskExists() && !RunKeyExists())
                        return;

                    // Задача планировщика — правильный способ для elevated-приложения
                    if (CreateTask(exe))
                    {
                        RemoveRunKey();      // иначе будет двойной запуск при входе
                        AppLog.Ok($"Автозагрузка включена: задача «{TaskName}» (без запроса UAC при входе).");
                        return;
                    }

                    // Запасной вариант
                    if (SetRunKey(exe))
                        AppLog.Warn("Автозагрузка включена через ключ Run. " +
                                    "Планировщик недоступен — при входе в систему возможен запрос UAC.");
                    else
                        AppLog.Warn("Автозагрузку включить не удалось: ни планировщик, ни ключ Run не сработали.");
                }
                else if (!IsEnabled())
                {
                    return;   // уже выключена — трогать систему не нужно
                }
                else
                {
                    DeleteTask();
                    RemoveRunKey();
                    AppLog.Ok("Автозагрузка отключена (задача и ключ Run удалены).");
                }
            }
            catch (Exception ex)
            {
                AppLog.Error("Автозагрузка", ex);
            }
        }

        // ==================== Планировщик задач ====================

        private static bool CreateTask(string exe)
        {
            // /RL HIGHEST — запуск с наивысшими правами (у нас и так requireAdministrator)
            // /SC ONLOGON  — при входе пользователя в систему
            // /F           — перезаписать, если задача уже есть
            var res = RunSchtasks($"/Create /F /TN \"{TaskName}\" /TR \"\\\"{exe}\\\"\" /SC ONLOGON /RL HIGHEST");
            return res.exitCode == 0;
        }

        private static bool TaskExists()
        {
            var res = RunSchtasks($"/Query /TN \"{TaskName}\"");
            return res.exitCode == 0;
        }

        private static void DeleteTask()
        {
            if (TaskExists())
                RunSchtasks($"/Delete /F /TN \"{TaskName}\"");
        }

        private static (int exitCode, string output) RunSchtasks(string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                    return (-1, "не удалось запустить schtasks.exe");

                // Читаем ДО WaitForExit — иначе возможен дедлок на переполнении pipe
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit(10_000);

                string text = string.IsNullOrWhiteSpace(error) ? output : output + " | " + error;

                if (process.ExitCode != 0)
                    AppLog.Warn($"schtasks {arguments} → код {process.ExitCode}: {text.Trim()}");

                return (process.ExitCode, text);
            }
            catch (Exception ex)
            {
                AppLog.Error($"schtasks {arguments}", ex);
                return (-2, ex.Message);
            }
        }

        // ==================== Ключ Run (запасной вариант и миграция с 2.0) ====================

        private static bool RunKeyExists()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
                return key?.GetValue(RunValueName) != null;
            }
            catch (Exception ex)
            {
                AppLog.Error("Чтение ключа автозагрузки", ex);
                return false;
            }
        }

        private static bool SetRunKey(string exe)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
                if (key == null) return false;

                key.SetValue(RunValueName, exe);
                return true;
            }
            catch (Exception ex)
            {
                AppLog.Error("Запись ключа автозагрузки", ex);
                return false;
            }
        }

        private static void RemoveRunKey()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
                if (key?.GetValue(RunValueName) != null)
                    key.DeleteValue(RunValueName, throwOnMissingValue: false);
            }
            catch (Exception ex)
            {
                AppLog.Error("Удаление ключа автозагрузки", ex);
            }
        }
    }
}
