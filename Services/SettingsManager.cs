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

        public int Theme { get; set; } = 2;
        public bool IsRussian { get; set; } = true;
        public List<string> WhiteList { get; set; } = new List<string>();
        public List<string> BlackList { get; set; } = new List<string>();
        public int RefreshRate { get; set; } = 5;

        /// <summary>
        /// true, если приложение когда-либо запускалось версии 1.0 на этом компьютере.
        /// Устанавливается один раз при первом запуске v1.0 и сохраняется во всех
        /// последующих версиях (используется для золотой темы/ачивки в v2.0+).
        /// </summary>
        public bool HadVersion1_0 { get; set; } = false;

        public void Load()
        {
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
                        RefreshRate = data.RefreshRate;
                        HadVersion1_0 = data.HadVersion1_0;
                    }
                }
                catch { }
            }
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

            // Эта сборка — v1.0: помечаем один раз и сохраняем, дальше флаг
            // переживёт любые будущие обновления, потому что settings.json
            // не входит в состав MSI-компонентов и не перезаписывается установщиком.
            if (!HadVersion1_0)
            {
                HadVersion1_0 = true;
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