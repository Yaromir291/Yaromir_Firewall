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

        // 🔑 ТЕКУЩАЯ ВЕРСИЯ ЭТОГО БИЛДА. Меняй при выпуске новой версии!
        private const string CurrentVersion = "2.0.0";

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
                        }
                    }
                    catch { }
                }

                // 🎖 УСТАНАВЛИВАЕМ ФЛАГ ТЕКУЩЕЙ ВЕРСИИ
                // ⚠️ При выпуске v3.0 добавить строку для Version3_0 и убрать проверку на v2.0 НЕ НУЖНО — она останется.
                if (CurrentVersion == "1.0.0") AchievementFlags.SetTrue(AchievementFlags.Version1_0);
                if (CurrentVersion == "2.0.0") AchievementFlags.SetTrue(AchievementFlags.Version2_0);

                LastVersion = CurrentVersion;

                // Если сохранённая тема недоступна — сбрасываем на системную
                if (Theme == 4 && !AchievementFlags.Get(AchievementFlags.Version1_0)) Theme = 2;
                if (Theme == 5 && !AchievementFlags.Get(AchievementFlags.Version2_0)) Theme = 2;

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

                Save();
            }
            catch { }
        }

        public void Save()
        {
            try
            {
                if (!Directory.Exists(_folder))
                    Directory.CreateDirectory(_folder);

                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(_settingsPath, json);
            }
            catch { }
        }
    }
}