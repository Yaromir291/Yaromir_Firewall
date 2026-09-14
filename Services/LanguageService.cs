using System;
using System.Collections.Generic;
using System.Windows;

namespace Yaromir_Firewall_FINAL1
{
    public class LanguageService
    {
        private static LanguageService? _instance = null;
        public static LanguageService Instance => _instance ??= new LanguageService();

        private bool _isRussian = true;
        public bool IsRussian
        {
            get => _isRussian;
            set
            {
                _isRussian = value;
                ApplyLanguageToAllWindows();
            }
        }

        private readonly Dictionary<string, string> _russianStrings = new Dictionary<string, string>
        {
            // MainWindow
            { "MainWindow_Title", "Yaromir Firewall" },
            { "MainWindow_ThemeButton_ToolTip", "Светлая тема" },
            { "MainWindow_LangButton_ToolTip_RU", "Русский" },
            { "MainWindow_LangButton_ToolTip_EN", "English" },
            { "MainWindow_AboutButton_ToolTip", "О программе" },
            { "MainWindow_MinimizeButton", "ТРЕЙ" },
            { "MainWindow_OpenMonitorButton", "Открыть мониторинг" },
            { "MainWindow_WhiteListButton", "Белый список" },
            { "MainWindow_BlackListButton", "Чёрный список" },
            { "MainWindow_StatusText", "Активно правил: {0}" },
            { "MainWindow_LicenseText", "© 2026 Яромир. Лицензия Apache 2.0." },

            // MonitorWindow
            { "MonitorWindow_Title", "Мониторинг" },
            { "MonitorWindow_BackButton", "←" },
            { "MonitorWindow_BackButton_ToolTip", "Назад" },
            { "MonitorWindow_Column_ProcessName", "Имя" },
            { "MonitorWindow_Column_PID", "PID" },
            { "MonitorWindow_Column_LocalPort", "Локальный порт" },
            { "MonitorWindow_Column_RemoteAddress", "Удалённый IP:порт" },
            { "MonitorWindow_Column_Protocol", "Протокол" },
            { "MonitorWindow_Column_Status", "Статус" },
            { "MonitorWindow_RefreshRateLabel", "Частота обновления:" },
            { "MonitorWindow_RefreshRate_Fast", "Быстро (1 сек)" },
            { "MonitorWindow_RefreshRate_Moderate", "Умеренно (5 сек)" },
            { "MonitorWindow_RefreshRate_Slow", "Медленно (10 сек)" },

            // WhiteListWindow
            { "WhiteListWindow_Title", "Белый список" },
            { "WhiteListWindow_AddButton", "Добавить" },
            { "WhiteListWindow_RemoveButton", "Удалить" },
            { "WhiteListWindow_ClearButton", "Очистить всё" },
            { "WhiteListWindow_AddDialog_Title", "Выберите программу для добавления в белый список" },

            // BlackListWindow
            { "BlackListWindow_Title", "Чёрный список" },
            { "BlackListWindow_AddButton", "Добавить" },
            { "BlackListWindow_RemoveButton", "Удалить" },
            { "BlackListWindow_ClearButton", "Очистить всё" },
            { "BlackListWindow_AddDialog_Title", "Выберите программу для блокировки" },
            { "BlackListWindow_ClearConfirm_Title", "Подтверждение" },
            { "BlackListWindow_ClearConfirm_Message", "Очистить весь чёрный список?" },

            // AboutWindow
            { "AboutWindow_Title", "О программе" },
            { "AboutWindow_AppName", "Yaromir Firewall" },
            { "AboutWindow_Version", "Версия 2.0.0" },
            { "AboutWindow_Copyright", "© 2026 Яромир. Все права защищены." },
            { "AboutWindow_LicenseInfo", "Исходный код доступен под лицензией Apache 2.0." },
            { "AboutWindow_TrademarkInfo", "Логотип и название являются товарными знаками автора." },
            { "AboutWindow_CloseButton", "Закрыть" },

            // PromptWindow
            { "PromptWindow_Title", "Новое подключение" },
            { "PromptWindow_AllowButton", "Разрешить" },
            { "PromptWindow_BlockButton", "Запретить" },
            { "PromptWindow_BlockAndKillButton", "Запретить и завершить" },
            { "PromptWindow_OnceCheckBox", "Только в этот раз" },
            { "PromptWindow_TargetInfo", "Цель: {0}" },

            // App.xaml.cs - Tray
            { "App_TrayToolTip", "Yaromir Firewall — защита подключений" },
            { "App_TrayMenu_Open", "Открыть" },
            { "App_TrayMenu_Exit", "Выход" },
            { "App_DuplicateRun_Title", "Yaromir_Firewall_FINAL1" },
            { "App_DuplicateRun_Message", "Программа уже запущена!" },

            // Common
            { "Common_Yes", "Да" },
            { "Common_No", "Нет" },
            { "Common_OK", "OK" },
            { "Common_Cancel", "Отмена" }
        };

        private readonly Dictionary<string, string> _englishStrings = new Dictionary<string, string>
        {
            // MainWindow
            { "MainWindow_Title", "Yaromir Firewall" },
            { "MainWindow_ThemeButton_ToolTip", "Light theme" },
            { "MainWindow_LangButton_ToolTip_RU", "Русский" },
            { "MainWindow_LangButton_ToolTip_EN", "English" },
            { "MainWindow_AboutButton_ToolTip", "About" },
            { "MainWindow_MinimizeButton", "TRAY" },
            { "MainWindow_OpenMonitorButton", "Open Monitor" },
            { "MainWindow_WhiteListButton", "Whitelist" },
            { "MainWindow_BlackListButton", "Blacklist" },
            { "MainWindow_StatusText", "Active rules: {0}" },
            { "MainWindow_LicenseText", "© 2026 Yaromir. Apache 2.0 License." },

            // MonitorWindow
            { "MonitorWindow_Title", "Monitor" },
            { "MonitorWindow_BackButton", "←" },
            { "MonitorWindow_BackButton_ToolTip", "Back" },
            { "MonitorWindow_Column_ProcessName", "Name" },
            { "MonitorWindow_Column_PID", "PID" },
            { "MonitorWindow_Column_LocalPort", "Local Port" },
            { "MonitorWindow_Column_RemoteAddress", "Remote IP:Port" },
            { "MonitorWindow_Column_Protocol", "Protocol" },
            { "MonitorWindow_Column_Status", "Status" },
            { "MonitorWindow_RefreshRateLabel", "Refresh rate:" },
            { "MonitorWindow_RefreshRate_Fast", "Fast (1 sec)" },
            { "MonitorWindow_RefreshRate_Moderate", "Moderate (5 sec)" },
            { "MonitorWindow_RefreshRate_Slow", "Slow (10 sec)" },

            // WhiteListWindow
            { "WhiteListWindow_Title", "Whitelist" },
            { "WhiteListWindow_AddButton", "Add" },
            { "WhiteListWindow_RemoveButton", "Remove" },
            { "WhiteListWindow_ClearButton", "Clear All" },
            { "WhiteListWindow_AddDialog_Title", "Select program to add to whitelist" },

            // BlackListWindow
            { "BlackListWindow_Title", "Blacklist" },
            { "BlackListWindow_AddButton", "Add" },
            { "BlackListWindow_RemoveButton", "Remove" },
            { "BlackListWindow_ClearButton", "Clear All" },
            { "BlackListWindow_AddDialog_Title", "Select program to block" },
            { "BlackListWindow_ClearConfirm_Title", "Confirmation" },
            { "BlackListWindow_ClearConfirm_Message", "Clear the entire blacklist?" },

            // AboutWindow
            { "AboutWindow_Title", "About" },
            { "AboutWindow_AppName", "Yaromir Firewall" },
            { "AboutWindow_Version", "Version 2.0.0" },
            { "AboutWindow_Copyright", "© 2026 Yaromir. All rights reserved." },
            { "AboutWindow_LicenseInfo", "Source code available under Apache 2.0 license." },
            { "AboutWindow_TrademarkInfo", "Logo and name are trademarks of the author." },
            { "AboutWindow_CloseButton", "Close" },

            // PromptWindow
            { "PromptWindow_Title", "New Connection" },
            { "PromptWindow_AllowButton", "Allow" },
            { "PromptWindow_BlockButton", "Block" },
            { "PromptWindow_BlockAndKillButton", "Block and Kill" },
            { "PromptWindow_OnceCheckBox", "Only once" },
            { "PromptWindow_TargetInfo", "Target: {0}" },

            // App.xaml.cs - Tray
            { "App_TrayToolTip", "Yaromir Firewall — connection protection" },
            { "App_TrayMenu_Open", "Open" },
            { "App_TrayMenu_Exit", "Exit" },
            { "App_DuplicateRun_Title", "Yaromir_Firewall_FINAL1" },
            { "App_DuplicateRun_Message", "Program is already running!" },

            // Common
            { "Common_Yes", "Yes" },
            { "Common_No", "No" },
            { "Common_OK", "OK" },
            { "Common_Cancel", "Cancel" }
        };

        public string Get(string key)
        {
            if (_isRussian)
            {
                return _russianStrings.TryGetValue(key, out var value) ? value : $"[{key}]";
            }
            else
            {
                return _englishStrings.TryGetValue(key, out var value) ? value : $"[{key}]";
            }
        }

        public string Get(string key, params object[] args)
        {
            return string.Format(Get(key), args);
        }

        private void ApplyLanguageToAllWindows()
        {
            if (Application.Current == null) return;

            foreach (Window window in Application.Current.Windows)
            {
                ApplyLanguageToWindow(window);
            }
        }

        private void ApplyLanguageToWindow(Window window)
        {
            switch (window)
            {
                case MainWindow mw:
                    mw.UpdateLanguageFromService();
                    break;
                case MonitorWindow monw:
                    monw.UpdateLanguageFromService();
                    break;
                case WhiteListWindow wlw:
                    wlw.UpdateLanguageFromService();
                    break;
                case BlackListWindow blw:
                    blw.UpdateLanguageFromService();
                    break;
                case AboutWindow aw:
                    aw.UpdateLanguageFromService();
                    break;
                case PromptWindow pw:
                    pw.UpdateLanguageFromService();
                    break;
            }
        }
    }
}
