using System;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Yaromir_Firewall_FINAL1
{
    public class FirewallService
    {
        private static FirewallService? _instance = null;
        public static FirewallService Instance => _instance ??= new FirewallService();

        // Путь к логу теперь живёт в AppLog (единая точка записи и ротации).

        // ==================== Имена правил ====================
        // Префикс YFW_ у служебных правил, чтобы их нельзя было спутать
        // с правилами программ (Block_<имя>) и с правилами других приложений.
        private const string RuleLocalSubnet = "YFW_Allow_LocalSubnet";
        private const string RuleBlockInternetV4 = "YFW_Block_Internet_v4";
        private const string RuleBlockInternetV6 = "YFW_Block_Internet_v6";

        // Правила старых версий — удаляются при старте (миграция).
        private static readonly string[] LegacyRules =
        {
            "Block_All_Inbound",
            "Allow_LocalSubnet_Inbound"
        };

        // ==================== «Всё, кроме локальной сети» ====================
        // ⚠️ ВАЖНО. В Windows Firewall правило BLOCK ВСЕГДА приоритетнее ALLOW,
        // порядок не важен. Поэтому старая связка
        //     Block_All_Inbound (block, protocol=any)  +  Allow_LocalSubnet_Inbound (allow)
        // не работала: локальная сеть блокировалась вместе со всем остальным,
        // а allow-правило было мёртвым.
        //
        // Правильный способ без своего драйвера — блокировать НЕ «всё»,
        // а только диапазоны публичных адресов. Тогда под правило не попадают
        // 10/8, 172.16/12, 192.168/16, 169.254/16, 127/8 — локальная сеть живёт.
        private const string PublicIpv4Ranges =
            "1.0.0.0-9.255.255.255," +
            "11.0.0.0-100.63.255.255," +
            "100.128.0.0-126.255.255.255," +
            "128.0.0.0-169.253.255.255," +
            "169.255.0.0-172.15.255.255," +
            "172.32.0.0-192.167.255.255," +
            "192.169.0.0-223.255.255.255";

        // Глобальный unicast IPv6 (2000::/3). Не трогаем ::1, fe80::/10 (link-local), fc00::/7 (ULA).
        private const string PublicIpv6Ranges =
            "2000::-3fff:ffff:ffff:ffff:ffff:ffff:ffff";

        // Правила, которые мы реально создали. Источник правды для счётчика в UI —
        // больше не нужно каждые 5 секунд дёргать `netsh show rule name=all`.
        // ConcurrentDictionary: пишут фоновые потоки, читает UI-поток.
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> _appliedRules =
            new System.Collections.Concurrent.ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        // Кэш «имя процесса -> полный путь». Поиск пути — самая дорогая операция,
        // повторять её для одной и той же программы нет смысла.
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _pathCache =
            new System.Collections.Concurrent.ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private bool _started;

        public FirewallService()
        {
            // Каждое обращение к netsh теперь логируется само: успех, код возврата и текст ошибки.
            NetshRunner.Completed += OnNetshCompleted;
        }

        private void OnNetshCompleted(NetshResult result)
        {
            string label = string.IsNullOrEmpty(result.Label) ? "без метки" : result.Label;

            if (result.Success)
            {
                Log($"[+] netsh OK «{label}» ({result.Commands.Count} команд): {string.Join(" | ", result.Commands)}");
            }
            else if (result.Tolerated)
            {
                // Ожидаемый отказ: например delete правила, которого и так не было.
                Log($"[i] netsh «{label}»: {result.FailText} — ожидаемо, игнорируем.");
            }
            else
            {
                Log($"[!] netsh ОТКАЗ «{label}» (код {result.ExitCode}): {result.FailText} | " +
                    $"Команды: {string.Join(" | ", result.Commands)}");
            }
        }

        /// <summary>
        /// Пишет в общий лог приложения (Services/AppLog.cs).
        /// Свой File.AppendAllText убран: он не был защищён от параллельной записи,
        /// и при одновременном обращении из UI-потока и таймера строки могли теряться.
        /// </summary>
        private void Log(string message) => AppLog.Write(message);

        /// <summary>
        /// Включает базовую защиту. UI-поток не блокирует: вся работа с netsh уходит в пул потоков.
        /// </summary>
        public void Start()
        {
            if (_started) return;
            _started = true;

            _ = Task.Run(() =>
            {
                try
                {
                    var commands = new List<string>();

                    // Миграция: вычищаем правила старых версий
                    foreach (var legacy in LegacyRules)
                        commands.Add($"advfirewall firewall delete rule name=\"{legacy}\"");

                    // Пересоздаём служебные правила (delete + add = идемпотентность)
                    foreach (var name in new[] { RuleLocalSubnet, RuleBlockInternetV4, RuleBlockInternetV6 })
                        commands.Add($"advfirewall firewall delete rule name=\"{name}\"");

                    commands.Add($"advfirewall firewall add rule name=\"{RuleLocalSubnet}\" dir=in action=allow remoteip=localsubnet");
                    commands.Add($"advfirewall firewall add rule name=\"{RuleBlockInternetV4}\" dir=in action=block protocol=any remoteip={PublicIpv4Ranges}");
                    commands.Add($"advfirewall firewall add rule name=\"{RuleBlockInternetV6}\" dir=in action=block protocol=any remoteip={PublicIpv6Ranges}");

                    _appliedRules[RuleLocalSubnet] = 0;
                    _appliedRules[RuleBlockInternetV4] = 0;
                    _appliedRules[RuleBlockInternetV6] = 0;

                    // ОДИН процесс netsh.exe на весь пакет вместо прежних пяти
                    var result = NetshRunner.Run(commands, "базовая защита", tolerateFailure: true);

                    if (result.Success)
                        Log("[+] Базовая защита включена: входящие из интернета заблокированы, локальная сеть разрешена.");
                    else
                        Log($"[!] Базовая защита включена НЕ полностью: {result.FailText}");
                }
                catch (Exception ex)
                {
                    Log($"[!] Ошибка блокировки: {ex.Message}");
                }

                ApplyBlacklistCore();
            });
        }

        /// <summary>
        /// Снимает защиту. Вызывается из App.OnExit — здесь синхронно и быстро,
        /// иначе процесс завершится раньше, чем правила удалятся.
        /// </summary>
        public void Stop()
        {
            _started = false;

            try
            {
                var commands = new List<string>
                {
                    $"advfirewall firewall delete rule name=\"{RuleBlockInternetV4}\"",
                    $"advfirewall firewall delete rule name=\"{RuleBlockInternetV6}\"",
                    $"advfirewall firewall delete rule name=\"{RuleLocalSubnet}\""
                };

                foreach (var legacy in LegacyRules)
                    commands.Add($"advfirewall firewall delete rule name=\"{legacy}\"");

                NetshRunner.Run(commands, "снятие базовой защиты", tolerateFailure: true);

                _appliedRules.TryRemove(RuleBlockInternetV4, out _);
                _appliedRules.TryRemove(RuleBlockInternetV6, out _);
                _appliedRules.TryRemove(RuleLocalSubnet, out _);

                Log("[+] Базовая защита отключена.");
            }
            catch (Exception ex)
            {
                Log($"[!] Ошибка отключения защиты: {ex.Message}");
            }
        }

        /// <summary>Публичная обёртка: не блокирует вызывающий (UI) поток.</summary>
        public void ApplyBlacklist() => _ = Task.Run((Action)ApplyBlacklistCore);

        /// <summary>
        /// Пересоздаёт правила чёрного списка ОДНИМ пакетом netsh.
        /// Раньше: 1 процесс на каждое удаление + 2 процесса на каждую программу
        /// (20 программ в списке = 40+ процессов netsh = секунды ожидания).
        /// Теперь: 1 процесс на всё.
        /// </summary>
        private void ApplyBlacklistCore()
        {
            try
            {
                var settings = SettingsManager.Instance;
                // 1. Сносим все правила программ, созданные нами ранее.
                //    Отдельным пакетом и с tolerateFailure: delete несуществующего правила
                //    возвращает код 1, и это НЕ ошибка — иначе строгий пакет ниже
                //    считал бы всю операцию проваленной.
                var deleteCommands = GetBlockRuleNames()
                    .Select(ruleName => $"advfirewall firewall delete rule name=\"{ruleName}\"")
                    .ToList();

                if (deleteCommands.Count > 0)
                    NetshRunner.Run(deleteCommands, "очистка старых правил программ", tolerateFailure: true);

                foreach (var key in _appliedRules.Keys.Where(k => k.StartsWith("Block_", StringComparison.OrdinalIgnoreCase)).ToList())
                    _appliedRules.TryRemove(key, out _);

                // 2. Сначала резолвим ВСЕ пути (это дорогая операция), потом один раз дёргаем netsh
                var commands = new List<string>();
                var created = new List<string>();

                foreach (var exeName in settings.BlackListCopy())
                {
                    string fullPath = FindProcessPath(exeName);
                    if (string.IsNullOrEmpty(fullPath))
                    {
                        Log($"[!] Не удалось найти путь к {exeName} — правило не создано. " +
                            $"Программа будет заблокирована только после того, как её запустят и добавят заново.");
                        continue;
                    }

                    string ruleName = RuleNameFor(exeName);
                    commands.Add($"advfirewall firewall add rule name=\"{ruleName}\" dir=in action=block program=\"{fullPath}\"");
                    commands.Add($"advfirewall firewall add rule name=\"{ruleName}_out\" dir=out action=block program=\"{fullPath}\"");
                    created.Add(ruleName);
                    created.Add(ruleName + "_out");
                }

                if (commands.Count == 0)
                {
                    Log("[i] Чёрный список пуст — правила программ не создавались.");
                    return;
                }

                // Строгая проверка: здесь отказ = реально неприменённое правило
                var result = NetshRunner.Run(commands, "чёрный список");
                if (result.Success)
                {
                    foreach (var name in created)
                        _appliedRules[name] = 0;

                    Log($"[+] Чёрный список применён: {created.Count / 2} программ, {commands.Count} команд, 1 процесс netsh.");
                }
                else
                {
                    Log($"[!] Чёрный список применён НЕ полностью: {result.FailText}");
                }
            }
            catch (Exception ex)
            {
                Log($"[!] Ошибка применения чёрного списка: {ex.Message}");
            }
        }

        /// <summary>Имя inbound-правила блокировки для программы.</summary>
        private static string RuleNameFor(string exeName) =>
            $"Block_{Path.GetFileNameWithoutExtension(exeName)}";

        /// <summary>
        /// Блокирует программу, НЕ блокируя вызывающий поток.
        /// Раньше этот метод вызывался из обработчика кнопки и мог держать UI мёртвым
        /// десятки секунд (поиск пути + несколько процессов netsh подряд).
        /// Если нужен результат и показ ошибки пользователю — зови BlockProgramAsync.
        /// </summary>
        public void BlockProgram(string exeName, bool killRunning)
            => _ = Task.Run(() => BlockProgramAsync(exeName, killRunning));

        /// <summary>
        /// Асинхронная версия: возвращает результат, чтобы UI мог честно сказать
        /// «заблокировано» или «не получилось, вот причина».
        /// </summary>
        public async Task<NetshResult> BlockProgramAsync(string exeName, bool killRunning)
        {
            var settings = SettingsManager.Instance;

            // Все обращения к спискам — через потокобезопасные методы SettingsManager:
            // этот код вызывается и из UI-потока, и из таймера NetworkMonitor.
            settings.AddToBlackList(exeName);

            if (settings.RemoveFromWhiteList(exeName))
                Log($"[i] {exeName} удалён из белого списка (перемещён в чёрный).");

            var result = await AddBlockRuleInternalAsync(exeName).ConfigureAwait(false);

            if (killRunning)
                KillProcessesByName(exeName);

            return result;
        }

        public void UnblockProgram(string exeName, bool killRunning)
            => _ = Task.Run(() => UnblockProgramAsync(exeName, killRunning));

        public async Task<NetshResult> UnblockProgramAsync(string exeName, bool killRunning)
        {
            SettingsManager.Instance.RemoveFromBlackList(exeName);

            var result = await RemoveBlockRuleAsync(exeName).ConfigureAwait(false);

            if (killRunning)
                KillProcessesByName(exeName);

            if (result.Ok)
                Log($"[+] {exeName} разблокирован (удалён из чёрного списка и правила firewall).");
            else
                Log($"[!] {exeName}: разблокировка выполнена не полностью — {result.FailText}");

            return result;
        }

        /// <summary>
        /// Заблокирована ли программа.
        ///
        /// БЫЛО: запуск netsh (50–300 мс) и разбор его текста, где захардкожены
        /// строки "Не найдено правил" и "No rules found". На немецкой, испанской или
        /// серверной Windows метод отвечал НЕПРАВДУ.
        /// СТАЛО: смотрим в собственный реестр созданных правил — мгновенно, на любом языке ОС.
        /// </summary>
        public bool IsBlocked(string exeName)
        {
            if (SettingsManager.Instance.IsBlackListed(exeName))
                return true;

            return _appliedRules.ContainsKey(RuleNameFor(exeName));
        }

        private async Task<NetshResult> AddBlockRuleInternalAsync(string exeName)
        {
            try
            {
                string fullPath = FindProcessPath(exeName);
                if (string.IsNullOrEmpty(fullPath))
                {
                    Log($"[!] Не удалось найти путь к {exeName} — правило НЕ создано.");
                    return new NetshResult
                    {
                        ExitCode = -10,
                        Error = $"Файл {exeName} не найден: сначала запусти программу, потом добавь её в чёрный список",
                        Label = $"блокировка {exeName}"
                    };
                }

                Log($"[i] Путь для {exeName}: {fullPath}");

                string ruleName = RuleNameFor(exeName);

                // delete — терпимо (правила может ещё не быть), add — строго
                await NetshRunner.RunAsync(new[]
                {
                    $"advfirewall firewall delete rule name=\"{ruleName}\"",
                    $"advfirewall firewall delete rule name=\"{ruleName}_out\""
                }, $"подчистка дублей {exeName}", tolerateFailure: true).ConfigureAwait(false);

                var result = await NetshRunner.RunAsync(new[]
                {
                    $"advfirewall firewall add rule name=\"{ruleName}\" dir=in action=block program=\"{fullPath}\"",
                    $"advfirewall firewall add rule name=\"{ruleName}_out\" dir=out action=block program=\"{fullPath}\""
                }, $"блокировка {exeName}").ConfigureAwait(false);

                if (result.Success)
                {
                    _appliedRules[ruleName] = 0;
                    _appliedRules[ruleName + "_out"] = 0;
                    Log($"[+] {exeName} заблокирован.");
                }

                return result;
            }
            catch (Exception ex)
            {
                Log($"[!] Ошибка создания правила для {exeName}: {ex.Message}");
                return new NetshResult { ExitCode = -11, Error = ex.Message, Label = $"блокировка {exeName}" };
            }
        }

        public void RemoveBlockRule(string exeName)
            => _ = Task.Run(() => RemoveBlockRuleAsync(exeName));

        public async Task<NetshResult> RemoveBlockRuleAsync(string exeName)
        {
            try
            {
                string ruleName = RuleNameFor(exeName);

                var result = await NetshRunner.RunAsync(new[]
                {
                    $"advfirewall firewall delete rule name=\"{ruleName}\"",
                    $"advfirewall firewall delete rule name=\"{ruleName}_out\""
                }, $"снятие блокировки {exeName}", tolerateFailure: true).ConfigureAwait(false);

                _appliedRules.TryRemove(ruleName, out _);
                _appliedRules.TryRemove(ruleName + "_out", out _);
                _pathCache.TryRemove(exeName, out _);

                // При tolerateFailure код возврата не показатель: правила могло и не быть.
                // Считаем успехом, если наших правил в реестре больше нет.
                return result;
            }
            catch (Exception ex)
            {
                Log($"[!] Ошибка удаления правила для {exeName}: {ex.Message}");
                return new NetshResult { ExitCode = -12, Error = ex.Message, Label = $"снятие блокировки {exeName}" };
            }
        }

        public void RemoveAllBlockRules()
        {
            var namesBefore = GetBlockRuleNames();
            Log($"[i] Очистка: найдено правил для удаления: {namesBefore.Count} ({string.Join(", ", namesBefore)})");

            if (namesBefore.Count > 0)
            {
                // ОДИН процесс netsh на все удаления вместо N процессов
                NetshRunner.Run(
                    namesBefore.Select(n => $"advfirewall firewall delete rule name=\"{n}\""),
                    "массовое удаление правил программ",
                    tolerateFailure: true);

                foreach (var n in namesBefore)
                    _appliedRules.TryRemove(n, out _);
            }

            _pathCache.Clear();

            var namesAfter = GetBlockRuleNames();
            if (namesAfter.Count == 0)
            {
                Log("[+] Все правила блокировки чёрного списка удалены, проверено повторным запросом.");
            }
            else
            {
                Log($"[!] После очистки остались правила ({namesAfter.Count}): {string.Join(", ", namesAfter)} — удаление не прошло полностью!");
            }
        }

        /// <summary>
        /// Имена всех наших правил блокировки программ.
        ///
        /// БЫЛО: `line.StartsWith("Rule Name:")` — то есть разбор ЗАХАРДКОЖЕННОГО
        /// английского текста. На русской Windows там "Имя правила:", на немецкой
        /// "Regelname:" — метод возвращал пустой список, и старые правила не удалялись.
        ///
        /// СТАЛО: берём текст после первого двоеточия в строке и смотрим, начинается ли
        /// он с "Block_". Метка поля нам больше не важна — работает на любом языке ОС.
        /// </summary>
        private List<string> GetBlockRuleNames()
        {
            var names = new List<string>();
            try
            {
                var res = NetshRunner.Run(
                    new[] { "advfirewall firewall show rule name=all" },
                    "перечисление правил",
                    tolerateFailure: true);

                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (string rawLine in res.Output.Split('\n'))
                {
                    string line = rawLine.Trim();
                    int colon = line.IndexOf(':');
                    if (colon < 0 || colon == line.Length - 1) continue;

                    string value = line.Substring(colon + 1).Trim();

                    if (value.StartsWith("Block_", StringComparison.OrdinalIgnoreCase) &&
                        !value.Equals("Block_All_Inbound", StringComparison.OrdinalIgnoreCase) &&
                        !value.StartsWith("Block_Internet", StringComparison.OrdinalIgnoreCase) &&
                        seen.Add(value))
                    {
                        names.Add(value);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[!] Не удалось перечислить правила: {ex.Message}");
            }

            return names;
        }

        /// <summary>
        /// Ищет полный путь к exe.
        ///
        /// ЧТО УБРАНО: финальный перебор ВСЕГО системного диска
        /// `Directory.GetFiles("C:\\", ..., AllDirectories)`. На живой машине это
        /// 300 тысяч – 1 миллион файлов: метод выполнялся МИНУТАМИ, а вызывался он
        /// из обработчика кнопки — то есть интерфейс намертво вис. Плюс сплошной
        /// обход диска — классический триггер эвристик антивирусов («RiskWare»).
        ///
        /// ЧТО ДОБАВЛЕНО: кэш результатов и IgnoreInaccessible (раньше одно
        /// «нет доступа» к папке обрывало весь поиск исключением).
        /// </summary>
        private string FindProcessPath(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return string.Empty;

            if (_pathCache.TryGetValue(processName, out string? cached) && File.Exists(cached))
                return cached;

            try
            {
                // 1. Самый надёжный источник — уже запущенный процесс
                foreach (var proc in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processName)))
                {
                    try
                    {
                        string path = proc.MainModule?.FileName ?? "";
                        if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        {
                            _pathCache[processName] = path;
                            return path;
                        }
                    }
                    catch { /* нет прав на процесс — пробуем дальше */ }
                    finally { proc.Dispose(); }
                }

                // 2. Типовые места установки. Пути берём из переменных окружения,
                //    а не хардкодим "C:\Program Files" — работает и на нестандартных раскладках.
                var searchPaths = new List<string>
                {
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows)
                };

                var options = new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,          // не ронять поиск на одной недоступной папке
                    MatchCasing = MatchCasing.CaseInsensitive,
                    AttributesToSkip = FileAttributes.ReparsePoint   // не уходить в симлинки и junction
                };

                foreach (var basePath in searchPaths.Where(p => !string.IsNullOrEmpty(p) && Directory.Exists(p)))
                {
                    try
                    {
                        foreach (var file in Directory.EnumerateFiles(basePath, processName, options))
                        {
                            _pathCache[processName] = file;
                            return file;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"[i] Поиск {processName} в {basePath} прерван: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[!] Ошибка поиска пути для {processName}: {ex.Message}");
            }

            Log($"[!] {processName} не найден в типовых папках установки. " +
                $"Полный перебор диска намеренно отключён: запусти программу один раз — " +
                $"путь определится по запущенному процессу.");
            return string.Empty;
        }

        // RunNetsh(string) удалён: он запускал отдельный процесс netsh.exe на каждую команду
        // и, что хуже, звал WaitForExit() ДО ReadToEnd() — при выводе больше 4 КБ
        // это классический дедлок (процесс ждёт, пока мы вычитаем pipe, мы ждём процесс).
        // Вместо него — NetshRunner (пакет команд, асинхронное чтение, таймаут, лог).

        private void KillProcessesByName(string exeName)
        {
            try
            {
                var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exeName));
                foreach (var proc in processes)
                {
                    try
                    {
                        proc.Kill();
                        proc.WaitForExit(2000);
                        Log($"[+] Процесс {proc.ProcessName} (PID: {proc.Id}) завершён.");
                    }
                    catch (Exception ex)
                    {
                        Log($"[!] Не удалось завершить процесс {exeName} (PID {proc.Id}): {ex.Message}");
                    }
                    finally
                    {
                        proc.Dispose();
                    }
                }
            }
            catch { }
        }

        public void KillProcess(int pid)
        {
            try
            {
                using var proc = Process.GetProcessById(pid);
                proc.Kill();
                Log($"[+] Процесс с PID {pid} завершён.");
            }
            catch (Exception ex)
            {
                Log($"[!] Не удалось завершить процесс PID {pid}: {ex.Message}");
            }
        }

        /// <summary>
        /// Сколько правил сейчас активно.
        ///
        /// БЫЛО: каждые 5 секунд из UI-потока (MainWindow.UpdateStatus) запускался netsh
        /// и перечислял ВСЕ правила системы. На живой машине их 300–600, вывод — сотни
        /// килобайт, и всё это блокировало интерфейс на 0,3–1 с КАЖДЫЕ 5 СЕКУНД.
        /// Разбор снова шёл по захардкоженной английской строке "Rule Name:".
        ///
        /// СТАЛО: размер собственного реестра применённых правил.
        /// Ноль процессов, ноль блокировок UI, одинаково на любом языке Windows.
        /// </summary>
        public int GetRuleCount() => _appliedRules.Count;

        /// <summary>
        /// Принудительно пересчитать состояние по фактическим правилам системы
        /// (например, если пользователь вручную полазил в wf.msc).
        /// Одно обращение к netsh, разбор — без привязки к языку Windows.
        /// </summary>
        public void ResyncRuleCache()
        {
            _appliedRules.Clear();

            foreach (var name in GetBlockRuleNames())
                _appliedRules[name] = 0;

            var res = NetshRunner.Run(
                new[] { "advfirewall firewall show rule name=all" },
                "синхронизация кэша правил",
                tolerateFailure: true);

            foreach (string rawLine in res.Output.Split('\n'))
            {
                string line = rawLine.Trim();
                int colon = line.IndexOf(':');
                if (colon < 0 || colon == line.Length - 1) continue;

                string value = line.Substring(colon + 1).Trim();

                foreach (var serviceRule in new[] { RuleLocalSubnet, RuleBlockInternetV4, RuleBlockInternetV6 })
                {
                    if (value.Equals(serviceRule, StringComparison.OrdinalIgnoreCase))
                        _appliedRules[serviceRule] = 0;
                }
            }
        }
    }
}
