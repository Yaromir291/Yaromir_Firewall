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

        private string _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

        /// <summary>Версия приложения, которая последний раз запускалась на этом ПК.</summary>
        public string LastVersion { get; set; } = "";

        public int Theme { get; set; } = 2;
        public bool IsRussian { get; set; } = true;
        public List<string> WhiteList { get; set; } = new List<string>();
        public List<string> BlackList { get; set; } = new List<string>();
        public List<ProgramPortRule> ProgramPortRules { get; set; } = new List<ProgramPortRule>();

        /// <summary>
        /// true, если приложение когда-либо запускалось версии 1.0 на этом компьютере.
        /// Устанавливается ОДИН РАЗ в v1.0 и больше никогда не перезаписывается в true.
        /// </summary>
        public bool HadVersion1_0 { get; set; } = false;

        // Текущая версия этого билда. Меняй при выпуске новой версии.
        private const string CurrentVersion = "2.0.0";

        public void Load()
        {
            bool settingsExistedBefore = File.Exists(_settingsPath);

            if (settingsExistedBefore)
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
                        HadVersion1_0 = data.HadVersion1_0;   // ← НЕ перезаписываем!
                        LastVersion = data.LastVersion ?? "";
                        ProgramPortRules = data.ProgramPortRules ?? new List<ProgramPortRule>();
                    }
                }
                catch { }
            }
            else
            {
                // 🔑 СВЕЖАЯ УСТАНОВКА: settings.json не существовал.
                // Пользователь НЕ ветеран, даже если это v2.0.
                HadVersion1_0 = false;
            }

            // Обновляем LastVersion до текущей версии (для будущих релизов)
            LastVersion = CurrentVersion;

            // Белый список по умолчанию (только при первой установке)
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

        public void Save()
        {
            try
            {
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(_settingsPath, json);
            }
            catch { }
        }
    }
}
