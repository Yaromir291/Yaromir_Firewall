using System;
using System.Collections.Generic;

namespace Yaromir_Firewall_FINAL1
{
    public static class Localization
    {
        public static event Action? LanguageChanged;

        public static bool IsRussian { get; private set; } = true;

        public static void SetLanguage(bool isRussian)
        {
            if (IsRussian == isRussian) return;
            IsRussian = isRussian;
            LanguageChanged?.Invoke();
        }

        public static string T(string key)
        {
            var dict = IsRussian ? Ru : En;
            return dict.TryGetValue(key, out var s) ? s : key;
        }

        public static string T(string key, params object[] args)
        {
            return string.Format(T(key), args);
        }

        private static readonly Dictionary<string, string> Ru = new Dictionary<string, string>
        {
            // ===== MainWindow =====
            ["TooltipTheme"] = "Сменить тему",
            ["TooltipLang"] = "Сменить язык",
            ["TooltipAbout"] = "О программе",
            ["TooltipSettings"] = "Настройки",
            ["TooltipAchievements"] = "Достижения",
            ["OpenMonitor"] = "Открыть мониторинг",
            ["WhiteList"] = "Белый список",
            ["BlackList"] = "Чёрный список",
            ["StatusRules"] = "Активно правил: {0}",
            ["LicenseLine"] = "© 2026 Яромир. Лицензия Apache 2.0.",

            // ===== Достижения =====
            ["Achievements"] = "Достижения",
            ["InDevelopment"] = "В разработке!",
            ["Veteran_Title"] = "Ветеран",
            ["Veteran_Description"] = "Вы скачали самую первую версию нашего проекта! Спасибо!",
            ["Veteran_Unlocked"] = "Разблокировано: Золотая тема",
            ["Close"] = "Закрыть",

            // ===== Трей =====
            ["TrayTooltip"] = "Yaromir Firewall — защита подключений",
            ["TrayOpen"] = "Открыть",
            ["TrayExit"] = "Выход",
            ["AlreadyRunning"] = "Программа уже запущена!",

            // ===== MonitorWindow =====
            ["Monitoring"] = "Мониторинг",
            ["Back"] = "Назад",
            ["ColProcess"] = "Имя",
            ["ColPid"] = "PID",
            ["ColLocalPort"] = "Локальный порт",
            ["ColRemote"] = "Удалённый IP:порт",
            ["ColProtocol"] = "Протокол",
            ["ColStatus"] = "Статус",
            ["RefreshRateLabel"] = "Частота обновления:",
            ["RefreshFast"] = "Быстро (1 сек)",
            ["RefreshModerate"] = "Умеренно (5 сек)",
            ["RefreshSlow"] = "Медленно (10 сек)",

            // ===== White/Black List =====
            ["Add"] = "Добавить",
            ["Remove"] = "Удалить",
            ["ClearAll"] = "Очистить всё",
            ["Confirmation"] = "Подтверждение",
            ["ConfirmClearWhite"] = "Очистить весь белый список?",
            ["ConfirmClearBlack"] = "Очистить весь чёрный список?",
            ["DialogExeFilter"] = "Исполняемые файлы (*.exe)|*.exe|Все файлы (*.*)|*.*",
            ["DialogTitleWhite"] = "Выберите программу для добавления в белый список",
            ["DialogTitleBlack"] = "Выберите программу для блокировки",

            // ===== SettingsWindow =====
            ["Settings"] = "Настройки",
            ["ThemeLabel"] = "Тема:",
            ["ThemeLight"] = "Светлая",
            ["ThemeDark"] = "Тёмная",
            ["ThemeSystem"] = "Системная",
            ["ThemeNeon"] = "Неон",
            ["ThemeGold"] = "Золотая",
            ["LanguageLabel"] = "Язык:",
            ["LangRussian"] = "Русский",
            ["LangEnglish"] = "Английский",
            ["PortRulesTitle"] = "Правила портов для программ",
            ["ProgramsLabel"] = "Программы:",
            ["NewProgramTooltip"] = "Введите имя программы (например, chrome.exe)",
            ["AllowedPortsLabel"] = "Разрешённые порты:",
            ["BlockedPortsLabel"] = "Запрещённые порты:",
            ["PortInputLabel"] = "Добавить / удалить порт:",
            ["PortTooltip"] = "Введите номер порта (1-65535)",
            ["Allow"] = "Разрешить",
            ["Block"] = "Запретить",
            ["Save"] = "Сохранить",
            ["ErrEnterProgram"] = "Введите имя программы!",
            ["ErrProgramExists"] = "Такая программа уже есть в списке!",
            ["ErrSelectProgramRemove"] = "Выберите программу для удаления!",
            ["ErrSelectProgramFirst"] = "Сначала выберите программу!",
            ["ConfirmRemoveProgram"] = "Удалить программу \"{0}\" и все её правила?",
            ["ErrInvalidPort"] = "Введите корректный номер порта (1-65535)!",
            ["PortNotFound"] = "Такой порт не найден в правилах выбранной программы!",
            ["SettingsSaved"] = "Настройки сохранены!",
            ["Error"] = "Ошибка",
            ["Information"] = "Информация",

            // ===== AboutWindow =====
            ["About"] = "О программе",
            ["Version"] = "Версия 2.0",
            ["Copyright"] = "© 2026 Яромир. Все права защищены.",
            ["LicenseAbout"] = "Исходный код доступен под лицензией Apache 2.0.",
            ["Trademark"] = "Логотип и название являются товарными знаками автора.",

            // ===== PromptWindow =====
            ["NewConnection"] = "Новое подключение",
            ["BlockAndKill"] = "Запретить и завершить",
            ["OnlyOnce"] = "Только в этот раз",
        };

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            // ===== MainWindow =====
            ["TooltipTheme"] = "Switch theme",
            ["TooltipLang"] = "Switch language",
            ["TooltipAbout"] = "About",
            ["TooltipSettings"] = "Settings",
            ["TooltipAchievements"] = "Achievements",
            ["OpenMonitor"] = "Open Monitor",
            ["WhiteList"] = "Whitelist",
            ["BlackList"] = "Blacklist",
            ["StatusRules"] = "Active rules: {0}",
            ["LicenseLine"] = "© 2026 Yaromir. Apache 2.0 License.",

            // ===== Достижения =====
            ["Achievements"] = "Achievements",
            ["InDevelopment"] = "In development!",
            ["Veteran_Title"] = "Veteran",
            ["Veteran_Description"] = "You downloaded the very first version of our project! Thank you!",
            ["Veteran_Unlocked"] = "Unlocked: Gold theme",
            ["Close"] = "Close",

            // ===== Трей =====
            ["TrayTooltip"] = "Yaromir Firewall — connection protection",
            ["TrayOpen"] = "Open",
            ["TrayExit"] = "Exit",
            ["AlreadyRunning"] = "The program is already running!",

            // ===== MonitorWindow =====
            ["Monitoring"] = "Monitoring",
            ["Back"] = "Back",
            ["ColProcess"] = "Process",
            ["ColPid"] = "PID",
            ["ColLocalPort"] = "Local port",
            ["ColRemote"] = "Remote IP:port",
            ["ColProtocol"] = "Protocol",
            ["ColStatus"] = "Status",
            ["RefreshRateLabel"] = "Refresh rate:",
            ["RefreshFast"] = "Fast (1 sec)",
            ["RefreshModerate"] = "Moderate (5 sec)",
            ["RefreshSlow"] = "Slow (10 sec)",

            // ===== White/Black List =====
            ["Add"] = "Add",
            ["Remove"] = "Remove",
            ["ClearAll"] = "Clear all",
            ["Confirmation"] = "Confirmation",
            ["ConfirmClearWhite"] = "Clear the entire whitelist?",
            ["ConfirmClearBlack"] = "Clear the entire blacklist?",
            ["DialogExeFilter"] = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            ["DialogTitleWhite"] = "Select a program to add to the whitelist",
            ["DialogTitleBlack"] = "Select a program to block",

            // ===== SettingsWindow =====
            ["Settings"] = "Settings",
            ["ThemeLabel"] = "Theme:",
            ["ThemeLight"] = "Light",
            ["ThemeDark"] = "Dark",
            ["ThemeSystem"] = "System",
            ["ThemeNeon"] = "Neon",
            ["ThemeGold"] = "Gold",
            ["LanguageLabel"] = "Language:",
            ["LangRussian"] = "Russian",
            ["LangEnglish"] = "English",
            ["PortRulesTitle"] = "Port rules for programs",
            ["ProgramsLabel"] = "Programs:",
            ["NewProgramTooltip"] = "Enter program name (e.g. chrome.exe)",
            ["AllowedPortsLabel"] = "Allowed ports:",
            ["BlockedPortsLabel"] = "Blocked ports:",
            ["PortInputLabel"] = "Add / remove port:",
            ["PortTooltip"] = "Enter port number (1-65535)",
            ["Allow"] = "Allow",
            ["Block"] = "Block",
            ["Save"] = "Save",
            ["ErrEnterProgram"] = "Enter a program name!",
            ["ErrProgramExists"] = "This program is already in the list!",
            ["ErrSelectProgramRemove"] = "Select a program to remove!",
            ["ErrSelectProgramFirst"] = "Select a program first!",
            ["ConfirmRemoveProgram"] = "Remove program \"{0}\" and all its rules?",
            ["ErrInvalidPort"] = "Enter a valid port number (1-65535)!",
            ["PortNotFound"] = "This port was not found in the rules of the selected program!",
            ["SettingsSaved"] = "Settings saved!",
            ["Error"] = "Error",
            ["Information"] = "Information",

            // ===== AboutWindow =====
            ["About"] = "About",
            ["Version"] = "Version 2.0",
            ["Copyright"] = "© 2026 Yaromir. All rights reserved.",
            ["LicenseAbout"] = "Source code is available under the Apache 2.0 license.",
            ["Trademark"] = "Logo and name are trademarks of the author.",

            // ===== PromptWindow =====
            ["NewConnection"] = "New connection",
            ["BlockAndKill"] = "Block and kill",
            ["OnlyOnce"] = "Just this once",
        };
    }
}
