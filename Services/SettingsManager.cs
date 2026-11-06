using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Yaromir_Firewall_FINAL1
{
    public class SettingsManager
    {
        private static SettingsManager? _instance = null;
        public static SettingsManager Instance => _instance ??= new SettingsManager();

        // Храним в %LOCALAPPDATA%\Yaromir_Firewall\
        private static readonly string _folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Yaromir_Firewall");

        private string _settingsPath = Path.Combine(_folder, "settings.json");

        public string LastVersion { get; set; } = "";

        public int Theme { get; set; } = 2;
        public bool IsRussian { get; set; } = true;
        public List<string> WhiteList { get; set; } = new List<string>();
        public List<string> BlackList { get; set; } = new List<string>();
        public List<ProgramPortRule> ProgramPortRules { get; set; } = new List<ProgramPortRule>();

        /// <summary>
        /// Запускать вместе с Windows. По умолчанию true — чтобы у тех, кто обновится
        /// с 2.0 (где автозагрузка прописывалась насильно), поведение не изменилось.
        /// </summary>
        public bool AutoStart { get; set; } = true;

        // Списки читают из UI-потока (окна) и из фонового таймера NetworkMonitor
        // одновременно. List<string> на такое не рассчитан: исключение
        // «Collection was modified» внутри Save() означало, что настройки НЕ сохранились,
        // а пустой catch это прятал.
        private readonly object _gate = new object();

        // ---------- Потокобезопасный доступ к спискам ----------

        public bool IsWhiteListed(string exeName)
        {
            lock (_gate) return WhiteList.Contains(exeName);
        }

        public bool IsBlackListed(string exeName)
        {
            lock (_gate) return BlackList.Contains(exeName);
        }

        public List<string> WhiteListCopy()
        {
            lock (_gate) return new List<string>(WhiteList);
        }

        public List<string> BlackListCopy()
        {
            lock (_gate) return new List<string>(BlackList);
        }

        public bool AddToWhiteList(string exeName)
        {
            lock (_gate)
            {
                if (WhiteList.Contains(exeName)) return false;
                WhiteList.Add(exeName);
                Save();
                return true;
            }
        }

        public bool RemoveFromWhiteList(string exeName)
        {
            lock (_gate)
            {
                if (!WhiteList.Remove(exeName)) return false;
                Save();
                return true;
            }
        }

        public bool AddToBlackList(string exeName)
        {
            lock (_gate)
            {
                if (BlackList.Contains(exeName)) return false;
                BlackList.Add(exeName);
                Save();
                return true;
            }
        }

        public bool RemoveFromBlackList(string exeName)
        {
            lock (_gate)
            {
                if (!BlackList.Remove(exeName)) return false;
                Save();
                return true;
            }
        }

        public void ClearWhiteList()
        {
            lock (_gate) { WhiteList.Clear(); Save(); }
        }

        public void ClearBlackList()
        {
            lock (_gate) { BlackList.Clear(); Save(); }
        }

        /// <summary>
        /// 🔑 Версия текущего билда. Берётся из метаданных сборки (свойство &lt;Version&gt;
        /// в .csproj), а НЕ прописывается руками: при const-строке компилятор считал
        /// сравнение с "1.0.0" заведомо ложным и выдавал warning CS0162, а главное —
        /// версию легко было забыть обновить, и она разъезжалась с настоящей.
        /// </summary>
        public static readonly string CurrentVersion =
            (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version ?? new Version(2, 0, 0)).ToString(3);

        public void Load()
        {
            try
            {
                if (!Directory.Exists(_folder))
                    Directory.CreateDirectory(_folder);

                if (File.Exists(_settingsPath))
                {
                    try
                    {
                        var json = File.ReadAllText(_settingsPath);
                        var data = JsonConvert.DeserializeObject<SettingsManager>(json);
                        if (data != null)
                        {
                            Theme = data.Theme;
                            IsRussian = data.IsRussian;
                            WhiteList = data.WhiteList ?? new List<string>();
                            BlackList = data.BlackList ?? new List<string>();
                            LastVersion = data.LastVersion ?? "";
                            ProgramPortRules = data.ProgramPortRules ?? new List<ProgramPortRule>();
                            AutoStart = data.AutoStart;
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLog.Error("Чтение settings.json", ex);
                    }
                }

                // 🎖 УСТАНАВЛИВАЕМ ФЛАГ ТЕКУЩЕЙ ВЕРСИИ
                SetVersionAchievement(CurrentVersion);

                LastVersion = CurrentVersion;

                // Если сохранённая тема недоступна — сбрасываем на системную
                if (Theme == 4 && !AchievementFlags.Get(AchievementFlags.Version1_0)) Theme = 2;
                if (Theme == 5 && !AchievementFlags.Get(AchievementFlags.Version2_0)) Theme = 2;

                lock (_gate)
                {
                    if (WhiteList.Count == 0)
                    {
                        WhiteList.AddRange(new[]
                        {
                            "chrome.exe", "firefox.exe", "msedge.exe", "opera.exe", "brave.exe",
                            "steam.exe", "discord.exe", "telegram.exe", "whatsapp.exe",
                            "svchost.exe", "System", "services.exe", "lsass.exe", "winlogon.exe",
                            "csrss.exe", "dwm.exe", "explorer.exe", "taskhostw.exe",
                            "SearchApp.exe", "ShellExperienceHost.exe", "SystemSettings.exe"
                        });
                    }
                }

                Save();
            }
            catch (Exception ex)
            {
                AppLog.Error("Загрузка настроек", ex);
            }
        }

        /// <summary>
        /// При выпуске новой версии достаточно добавить одну строку case.
        /// Вынесено в метод со string-параметром намеренно: иначе компилятор
        /// сворачивает сравнение с константой и ругается на недостижимый код.
        /// </summary>
        private static void SetVersionAchievement(string version)
        {
            switch (version)
            {
                case "1.0.0": AchievementFlags.SetTrue(AchievementFlags.Version1_0); break;
                case "2.0.0": AchievementFlags.SetTrue(AchievementFlags.Version2_0); break;
                case "3.0.0": AchievementFlags.SetTrue(AchievementFlags.Version3_0); break;
            }
        }

        public void Save()
        {
            try
            {
                lock (_gate)   // Monitor реентерабелен: Save() зовётся и из Add/Remove
                {
                    if (!Directory.Exists(_folder))
                        Directory.CreateDirectory(_folder);

                    var json = JsonConvert.SerializeObject(this, Formatting.Indented);

                    // Запись через временный файл: если процесс убьют посреди Save(),
                    // settings.json останется целым, а не превратится в огрызок.
                    string tmp = _settingsPath + ".tmp";
                    File.WriteAllText(tmp, json);
                    File.Move(tmp, _settingsPath, overwrite: true);
                }
            }
            catch (Exception ex)
            {
                AppLog.Error("Сохранение настроек", ex);
            }
        }
    }
}