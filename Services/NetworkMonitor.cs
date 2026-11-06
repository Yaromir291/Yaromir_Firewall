using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace Yaromir_Firewall_FINAL1
{
    public class NetworkMonitor
    {
        private static NetworkMonitor? _instance = null;
        public static NetworkMonitor Instance => _instance ??= new NetworkMonitor();

        private System.Threading.Timer? _pollTimer;
        private readonly HashSet<string> _decidedThisSession = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly object _lock = new object();

        // Очередь диалогов: за один опрос может найтись несколько неизвестных программ.
        // Показывать их надо ПО ОЧЕРЕДИ, а не стопкой модальных окон друг на друге.
        private readonly Queue<PromptRequest> _promptQueue = new Queue<PromptRequest>();
        private readonly object _promptLock = new object();
        private bool _promptActive;
        private const int MaxPromptQueue = 20;

        // Кэш PID -> имя процесса на 30 секунд. Без него каждые 3 секунды создавались
        // сотни объектов Process и ни один не освобождался — утечка дескрипторов.
        private readonly System.Collections.Concurrent.ConcurrentDictionary<int, PidName> _pidNames =
            new System.Collections.Concurrent.ConcurrentDictionary<int, PidName>();

        private readonly struct PidName
        {
            public PidName(string name, DateTime expires) { Name = name; Expires = expires; }
            public string Name { get; }
            public DateTime Expires { get; }
        }

        private readonly struct PromptRequest
        {
            public PromptRequest(string exe, string path, string target)
            {
                ExeName = exe;
                FullPath = path;
                Target = target;
            }

            public string ExeName { get; }
            public string FullPath { get; }
            public string Target { get; }
        }

        public void Start()
        {
            lock (_lock)
            {
                if (_pollTimer != null) return;
                _pollTimer = new System.Threading.Timer(PollCallback, null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));
            }

            AppLog.Info("Мониторинг подключений запущен (опрос каждые 3 с).");
        }

        public void Stop()
        {
            lock (_lock)
            {
                _pollTimer?.Dispose();
                _pollTimer = null;
            }

            lock (_promptLock)
            {
                _promptQueue.Clear();
                _promptActive = false;
            }
        }

        private void PollCallback(object? state)
        {
            try
            {
                var settings = SettingsManager.Instance;
                var connections = GetConnections();

                // берём по одному соединению на процесс, чтобы не спрашивать несколько раз за проход
                var byProcess = new Dictionary<string, ConnectionInfo>(StringComparer.OrdinalIgnoreCase);
                foreach (var c in connections)
                {
                    if (c.ProcessName == "Unknown" || c.Pid <= 4) continue;

                    string exeName = c.ProcessName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                        ? c.ProcessName : c.ProcessName + ".exe";

                    if (!byProcess.ContainsKey(exeName))
                        byProcess[exeName] = c;
                }

                // Списки читаем ОДИН раз и работаем с копиями: их в это же время
                // может менять UI-поток (окна белого/чёрного списка).
                var white = settings.WhiteListCopy();
                var black = settings.BlackListCopy();

                foreach (var kvp in byProcess)
                {
                    string exeName = kvp.Key;
                    var conn = kvp.Value;

                    lock (_lock)
                    {
                        if (_decidedThisSession.Contains(exeName)) continue;

                        // помечаем сразу, чтобы таймер не открыл второй диалог за тот же проход
                        _decidedThisSession.Add(exeName);
                    }

                    if (white.Contains(exeName) || black.Contains(exeName))
                        continue;

                    string fullPath = "";
                    try
                    {
                        using var proc = Process.GetProcessById(conn.Pid);
                        fullPath = proc.MainModule?.FileName ?? "";
                    }
                    catch (Exception ex)
                    {
                        // Обычно Win32Exception «Отказано в доступе» для системных процессов
                        // или ArgumentException, если процесс уже завершился. Это не авария.
                        AppLog.Info($"Не удалось получить путь {exeName} (PID {conn.Pid}): {ex.Message}");
                    }

                    string target = $"{conn.RemoteAddress} ({conn.Protocol})";
                    EnqueuePrompt(new PromptRequest(exeName, fullPath, target));
                }
            }
            catch (Exception ex)
            {
                // БЫЛО: catch { } — опрос молча умирал, и защита переставала работать
                // до перезапуска приложения, не оставив в логе ни строчки.
                AppLog.Error("Опрос подключений", ex);
            }
        }

        /// <summary>Ставит запрос в очередь и, если диалог сейчас не открыт, запускает показ.</summary>
        private void EnqueuePrompt(PromptRequest request)
        {
            bool startNow = false;

            lock (_promptLock)
            {
                if (_promptQueue.Count >= MaxPromptQueue)
                {
                    AppLog.Warn($"Очередь диалогов переполнена ({MaxPromptQueue}) — {request.ExeName} отложен.");
                    UndoDecision(request.ExeName);
                    return;
                }

                _promptQueue.Enqueue(request);

                if (!_promptActive)
                {
                    _promptActive = true;
                    startNow = true;
                }
            }

            if (startNow)
                PumpPrompts();
        }

        /// <summary>Показывает диалоги по одному, на UI-потоке, не блокируя поток таймера.</summary>
        private void PumpPrompts()
        {
            var app = Application.Current;
            if (app == null)
            {
                lock (_promptLock)
                {
                    _promptQueue.Clear();
                    _promptActive = false;
                }
                return;
            }

            // БЫЛО: Dispatcher.Invoke(...) — СИНХРОННО. Поток пула стоял заблокированным
            // всё время, пока открыт модальный диалог (то есть потенциально вечно).
            // СТАЛО: BeginInvoke — поток таймера освобождается сразу.
            _ = app.Dispatcher.BeginInvoke(new Action(async () =>
            {
                PromptRequest item;

                lock (_promptLock)
                {
                    if (_promptQueue.Count == 0)
                    {
                        _promptActive = false;
                        return;
                    }

                    item = _promptQueue.Dequeue();
                }

                try
                {
                    await AskUserAsync(item).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    UndoDecision(item.ExeName);
                    AppLog.Error($"Диалог перехвата {item.ExeName}", ex);
                }

                PumpPrompts();   // следующий в очереди
            }));
        }

        private async System.Threading.Tasks.Task AskUserAsync(PromptRequest request)
        {
            var prompt = new PromptWindow(
                request.ExeName,
                string.IsNullOrEmpty(request.FullPath) ? "—" : request.FullPath,
                request.Target)
            {
                Topmost = true
            };

            bool? dialogResult = prompt.ShowDialog();
            var settings = SettingsManager.Instance;

            // Окно закрыли крестиком / Alt+F4 — решение НЕ принято.
            // БЫЛО: Result по умолчанию = Block, и программа молча улетала в чёрный список навсегда.
            if (dialogResult != true || prompt.Result == PromptResult.None)
            {
                UndoDecision(request.ExeName);
                AppLog.Info($"{request.ExeName}: диалог закрыт без решения — спросим при следующем подключении.");
                return;
            }

            switch (prompt.Result)
            {
                case PromptResult.Allow:
                    if (!prompt.OnlyOnce)
                    {
                        settings.AddToWhiteList(request.ExeName);
                        AppLog.Ok($"{request.ExeName} добавлен в белый список (по выбору пользователя).");
                    }
                    else
                    {
                        AppLog.Info($"{request.ExeName} разрешён разово — будет спрошен снова.");
                    }
                    break;

                case PromptResult.Block:
                    await ReportBlockAsync(request.ExeName, killRunning: false).ConfigureAwait(true);
                    break;

                case PromptResult.BlockAndKill:
                    await ReportBlockAsync(request.ExeName, killRunning: true).ConfigureAwait(true);
                    break;
            }
        }

        /// <summary>Блокирует и честно пишет в лог, получилось ли.</summary>
        private static async System.Threading.Tasks.Task ReportBlockAsync(string exeName, bool killRunning)
        {
            var result = await FirewallService.Instance
                .BlockProgramAsync(exeName, killRunning)
                .ConfigureAwait(true);

            if (result.Success)
                AppLog.Ok($"{exeName} заблокирован (по решению пользователя).");
            else
                AppLog.Warn($"{exeName}: заблокировать НЕ удалось — {result.FailText}");
        }

        /// <summary>Снимает пометку «решение принято», чтобы программу спросили ещё раз.</summary>
        private void UndoDecision(string exeName)
        {
            lock (_lock)
            {
                _decidedThisSession.Remove(exeName);
            }
        }

        public List<ConnectionInfo> GetConnections()
        {
            var result = new List<ConnectionInfo>();

            try
            {
                // Получаем TCP-соединения с PID через Windows API
                foreach (var entry in GetExtendedTcpTable())
                {
                    result.Add(new ConnectionInfo
                    {
                        ProcessName = GetProcessName(entry.ProcessId),
                        Pid = entry.ProcessId,
                        LocalPort = entry.LocalPort,
                        RemoteAddress = $"{FormatIp(entry.RemoteAddress)}:{entry.RemotePort}",
                        Protocol = "TCP",
                        Status = entry.State.ToString()
                    });
                }

                // Получаем UDP-слушатели с PID через Windows API
                foreach (var entry in GetExtendedUdpTable())
                {
                    result.Add(new ConnectionInfo
                    {
                        ProcessName = GetProcessName(entry.ProcessId),
                        Pid = entry.ProcessId,
                        LocalPort = entry.LocalPort,
                        RemoteAddress = "*:*",
                        Protocol = "UDP",
                        Status = "Listening"
                    });
                }
            }
            catch (Exception ex)
            {
                AppLog.Error("Чтение таблицы подключений", ex);
            }

            return result;
        }

        /// <summary>
        /// Имя процесса по PID с кэшем на 30 секунд.
        ///
        /// БЫЛО: Process.GetProcessById(...) на каждую строку таблицы БЕЗ Dispose —
        /// сотни неосвобождённых объектов каждые 3 секунды.
        /// </summary>
        private string GetProcessName(int pid)
        {
            if (_pidNames.TryGetValue(pid, out var cached) && cached.Expires > DateTime.UtcNow)
                return cached.Name;

            string name = "Unknown";
            try
            {
                using var process = Process.GetProcessById(pid);
                name = process.ProcessName;
            }
            catch
            {
                // Процесс уже завершился или нет прав — для короткоживущих PID это норма
            }

            _pidNames[pid] = new PidName(name, DateTime.UtcNow.AddSeconds(30));
            return name;
        }

        /// <summary>
        /// DWORD из MIB_TCPROW — это 4 байта адреса в сетевом порядке.
        ///
        /// БЫЛО: RemoteAddress = $"{entry.RemoteAddress}:{entry.RemotePort}",
        /// то есть в монитор попадал СЫРОЙ INTEGER: вместо 142.250.74.174:443
        /// пользователь видел 2920215758:49571.
        /// </summary>
        private static string FormatIp(uint raw)
        {
            try
            {
                return new System.Net.IPAddress(raw).ToString();
            }
            catch
            {
                return raw.ToString();
            }
        }

        /// <summary>
        /// Порты в MIB_TCPROW/MIB_UDPROW лежат в сетевом (big-endian) порядке, а читаем мы
        /// их как native int на little-endian машине — байты надо поменять местами.
        /// Без этого порт 443 отображался как 47617.
        /// </summary>
        private static int NetworkToHostPort(int raw)
        {
            int port = raw & 0xFFFF;
            return ((port & 0xFF) << 8) | ((port >> 8) & 0xFF);
        }

        // ==================== Windows API для TCP ====================
        private static List<TcpEntry> GetExtendedTcpTable()
        {
            var result = new List<TcpEntry>();

            try
            {
                const int AF_INET = 2;
                const int TCP_TABLE_OWNER_PID_ALL = 4;

                var bufferSize = 0;
                var error = NativeMethods.GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, false, AF_INET, TCP_TABLE_OWNER_PID_ALL, 0);

                if (error != 0 && error != 122) // 122 = ERROR_INSUFFICIENT_BUFFER
                    return result;

                var buffer = Marshal.AllocHGlobal(bufferSize);
                try
                {
                    error = NativeMethods.GetExtendedTcpTable(buffer, ref bufferSize, false, AF_INET, TCP_TABLE_OWNER_PID_ALL, 0);
                    if (error != 0)
                        return result;

                    var table = Marshal.PtrToStructure<TcpTable>(buffer);
                    var entries = table.Entries;
                    for (int i = 0; i < entries; i++)
                    {
                        var rowPtr = IntPtr.Add(buffer, Marshal.SizeOf<TcpTable>() + i * Marshal.SizeOf<TcpRow>());
                        var row = Marshal.PtrToStructure<TcpRow>(rowPtr);
                        result.Add(new TcpEntry
                        {
                            ProcessId = row.ProcessId,
                            LocalPort = NetworkToHostPort(row.LocalPort),
                            RemotePort = NetworkToHostPort(row.RemotePort),
                            RemoteAddress = row.RemoteAddress,   // сырой DWORD -> FormatIp()
                            State = (TcpState)row.State // ЯВНОЕ ПРИВЕДЕНИЕ
                        });
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch (Exception ex)
            {
                AppLog.Error("Чтение таблицы TCP/UDP (P/Invoke)", ex);
            }

            return result;
        }

        // ==================== Windows API для UDP ====================
        private static List<UdpEntry> GetExtendedUdpTable()
        {
            var result = new List<UdpEntry>();

            try
            {
                const int AF_INET = 2;
                const int UDP_TABLE_OWNER_PID = 1;

                var bufferSize = 0;
                var error = NativeMethods.GetExtendedUdpTable(IntPtr.Zero, ref bufferSize, false, AF_INET, UDP_TABLE_OWNER_PID, 0);

                if (error != 0 && error != 122)
                    return result;

                var buffer = Marshal.AllocHGlobal(bufferSize);
                try
                {
                    error = NativeMethods.GetExtendedUdpTable(buffer, ref bufferSize, false, AF_INET, UDP_TABLE_OWNER_PID, 0);
                    if (error != 0)
                        return result;

                    var table = Marshal.PtrToStructure<UdpTable>(buffer);
                    var entries = table.Entries;
                    for (int i = 0; i < entries; i++)
                    {
                        var rowPtr = IntPtr.Add(buffer, Marshal.SizeOf<UdpTable>() + i * Marshal.SizeOf<UdpRow>());
                        var row = Marshal.PtrToStructure<UdpRow>(rowPtr);
                        result.Add(new UdpEntry
                        {
                            ProcessId = row.ProcessId,
                            LocalPort = NetworkToHostPort(row.LocalPort)
                        });
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch (Exception ex)
            {
                AppLog.Error("Чтение таблицы TCP/UDP (P/Invoke)", ex);
            }

            return result;
        }

        // ==================== Структуры для P/Invoke ====================
        [StructLayout(LayoutKind.Sequential)]
        private struct TcpTable
        {
            public int Entries;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TcpRow
        {
            public int State;
            public int LocalAddress;
            public int LocalPort;
            public uint RemoteAddress;
            public int RemotePort;
            public int ProcessId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct UdpTable
        {
            public int Entries;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct UdpRow
        {
            public int LocalAddress;
            public int LocalPort;
            public int ProcessId;
        }

        private class TcpEntry
        {
            public int ProcessId { get; set; }
            public int LocalPort { get; set; }
            public int RemotePort { get; set; }
            public uint RemoteAddress { get; set; }
            public TcpState State { get; set; }
        }

        private class UdpEntry
        {
            public int ProcessId { get; set; }
            public int LocalPort { get; set; }
        }

        private static class NativeMethods
        {
            [DllImport("iphlpapi.dll", SetLastError = true)]
            public static extern int GetExtendedTcpTable(
                IntPtr pTcpTable,
                ref int pdwSize,
                bool bOrder,
                int ulAf,
                int TableClass,
                int reserved);

            [DllImport("iphlpapi.dll", SetLastError = true)]
            public static extern int GetExtendedUdpTable(
                IntPtr pUdpTable,
                ref int pdwSize,
                bool bOrder,
                int ulAf,
                int TableClass,
                int reserved);
        }
    }
}