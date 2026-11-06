using System;
using System.IO;

namespace Yaromir_Firewall_FINAL1
{
    /// <summary>
    /// Единый логгер приложения: один файл, один lock, один формат, одна ротация.
    ///
    /// Зачем: раньше свой Log() был только внутри FirewallService, а остальные 28 пустых
    /// `catch { }` молча глотали ошибки — пользователь не мог понять, почему правило
    /// не применилось. Теперь любой модуль может написать в тот же лог,
    /// который открывается кнопкой «Открыть лог» в настройках.
    /// </summary>
    public static class AppLog
    {
        private static readonly object Gate = new object();

        private static readonly string LogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Yaromir_Firewall",
            "firewall_log.txt");

        /// <summary>Лог больше 5 МБ — бессмысленно. Ротируем, чтобы не съесть диск.</summary>
        private const long MaxBytes = 5 * 1024 * 1024;

        public static string FilePath => LogPath;

        public static void Write(string message)
        {
            try
            {
                lock (Gate)
                {
                    string folder = Path.GetDirectoryName(LogPath) ?? "";
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    RotateIfNeeded(folder);

                    File.AppendAllText(LogPath,
                        $"{DateTime.Now:yyyy.MM.dd HH:mm:ss} {message}{Environment.NewLine}");
                }
            }
            catch
            {
                // Логгер не имеет права ронять приложение.
                // Если писать некуда (нет прав на %ProgramData%) — просто игнорируем.
            }

            Console.WriteLine(message);
        }

        public static void Ok(string message) => Write($"[+] {message}");
        public static void Info(string message) => Write($"[i] {message}");
        public static void Warn(string message) => Write($"[!] {message}");

        /// <summary>
        /// Для catch-блоков: вместо `catch { }` пишем `catch (Exception ex) { AppLog.Error("что делали", ex); }`.
        /// </summary>
        public static void Error(string scope, Exception ex)
        {
            if (ex == null)
            {
                Warn($"{scope}: неизвестная ошибка");
                return;
            }

            Write($"[!] {scope}: {ex.GetType().Name}: {ex.Message}");
        }

        private static void RotateIfNeeded(string folder)
        {
            try
            {
                var info = new FileInfo(LogPath);
                if (!info.Exists || info.Length < MaxBytes)
                    return;

                string old = Path.Combine(folder, "firewall_log.old.txt");
                if (File.Exists(old))
                    File.Delete(old);

                File.Move(LogPath, old);
                Write($"[i] Лог превысил {MaxBytes / 1024 / 1024} МБ — предыдущий сохранён как firewall_log.old.txt");
            }
            catch { }
        }
    }
}
