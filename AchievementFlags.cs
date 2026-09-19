using System;
using System.Collections.Generic;
using System.IO;

namespace Yaromir_Firewall_FINAL1
{
    /// <summary>
    /// Менеджер флагов достижений.
    /// Хранит флаги в отдельном файле %LOCALAPPDATA%\Yaromir_Firewall\achievements.txt,
    /// который НЕ удаляется при деинсталляции программы.
    /// Это не даёт пользователю "сбросить" достижения простой переустановкой.
    /// </summary>
    public static class AchievementFlags
    {
        private static readonly string _folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Yaromir_Firewall");

        private static readonly string _filePath = Path.Combine(_folder, "achievements.txt");

        private static Dictionary<string, string> _flags = new();
        private static bool _loaded = false;

        // ===== Ключи флагов (добавляй новые при выпуске версий) =====
        public const string Version1_0 = "had_version_1_0";
        public const string Version2_0 = "had_version_2_0";
        public const string Version3_0 = "had_version_3_0";

        // ===== Загрузка из файла =====
        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            try
            {
                if (!Directory.Exists(_folder))
                    Directory.CreateDirectory(_folder);

                if (File.Exists(_filePath))
                {
                    foreach (var line in File.ReadAllLines(_filePath))
                    {
                        var trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed)) continue;
                        if (trimmed.StartsWith("#")) continue;

                        int eq = trimmed.IndexOf('=');
                        if (eq <= 0) continue;

                        string key = trimmed.Substring(0, eq).Trim();
                        string val = trimmed.Substring(eq + 1).Trim();
                        _flags[key] = val;
                    }
                }
            }
            catch { }
        }

        // ===== Публичный API =====
        public static bool Get(string key)
        {
            EnsureLoaded();
            return _flags.TryGetValue(key, out var val) &&
                   val.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        public static void SetTrue(string key)
        {
            EnsureLoaded();
            if (Get(key)) return; // уже true — не перезаписываем
            _flags[key] = "true";
            Save();
        }

        // ===== Сохранение в файл с шапкой-комментарием =====
        private static void Save()
        {
            try
            {
                if (!Directory.Exists(_folder))
                    Directory.CreateDirectory(_folder);

                var lines = new List<string>
                {
                    "# ============================================================",
                    "#  Yaromir Firewall — файл достижений",
                    "# ============================================================",
                    "#",
                    "#  В этом файле сохранены все флаги для получения достижений.",
                    "#  Если вы больше не собираетесь устанавливать нашу программу,",
                    "#  можете удалить этот файл.",
                    "#",
                    "#  Формат: ключ = true",
                    "#",
                    "# ============================================================",
                    ""
                };

                foreach (var kvp in _flags)
                {
                    lines.Add($"{kvp.Key} = {kvp.Value}");
                }

                File.WriteAllLines(_filePath, lines);
            }
            catch { }
        }
    }
}