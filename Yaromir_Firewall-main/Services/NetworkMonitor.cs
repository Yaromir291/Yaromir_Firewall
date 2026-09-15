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

        public void Start()
        {
            if (_pollTimer != null) return;
            _pollTimer = new System.Threading.Timer(PollCallback, null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));
        }

        public void Stop()
        {
            _pollTimer?.Dispose();
            _pollTimer = null;
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

                foreach (var kvp in byProcess)
                {
                    string exeName = kvp.Key;
                    var conn = kvp.Value;

                    lock (_lock)
                    {
                        if (_decidedThisSession.Contains(exeName)) continue;
                    }

                    if (settings.WhiteList.Contains(exeName) || settings.BlackList.Contains(exeName))
                    {
                        lock (_lock) { _decidedThisSession.Add(exeName); }
                        continue;
                    }

                    // помечаем сразу, чтобы таймер не открыл второй диалог, пока первый ещё не закрыт
                    lock (_lock) { _decidedThisSession.Add(exeName); }

                    string fullPath = "";
                    try
                    {
                        var proc = Process.GetProcessById(conn.Pid);
                        fullPath = proc.MainModule?.FileName ?? "";
                    }
                    catch { }

                    string target = $"{conn.RemoteAddress} ({conn.Protocol})";

                    Application.Current?.Dispatcher.Invoke(() => AskUser(exeName, fullPath, target));
                }
            }
            catch { }
        }

        private void AskUser(string exeName, string fullPath, string target)
        {
            var prompt = new PromptWindow(exeName, string.IsNullOrEmpty(fullPath) ? "—" : fullPath, target);
            prompt.Topmost = true;
            prompt.ShowDialog();

            var settings = SettingsManager.Instance;

            switch (prompt.Result)
            {
                case PromptResult.Allow:
                    if (!prompt.OnlyOnce && !settings.WhiteList.Contains(exeName))
                    {
                        settings.WhiteList.Add(exeName);
                        settings.Save();
                    }
                    break;

                case PromptResult.Block:
                    FirewallService.Instance.BlockProgram(exeName, killRunning: false);
                    break;

                case PromptResult.BlockAndKill:
                    FirewallService.Instance.BlockProgram(exeName, killRunning: true);
                    break;
            }
        }

        public List<ConnectionInfo> GetConnections()
        {
            var result = new List<ConnectionInfo>();

            try
            {
                // Получаем TCP-соединения с PID через Windows API
                var tcpTable = GetExtendedTcpTable();
                foreach (var entry in tcpTable)
                {
                    string processName = "Unknown";
                    try
                    {
                        var process = Process.GetProcessById(entry.ProcessId);
                        processName = process.ProcessName;
                    }
                    catch { }

                    result.Add(new ConnectionInfo
                    {
                        ProcessName = processName,
                        Pid = entry.ProcessId,
                        LocalPort = entry.LocalPort,
                        RemoteAddress = $"{entry.RemoteAddress}:{entry.RemotePort}",
                        Protocol = "TCP",
                        Status = entry.State.ToString()
                    });
                }

                // Получаем UDP-слушатели с PID через Windows API
                var udpTable = GetExtendedUdpTable();
                foreach (var entry in udpTable)
                {
                    string processName = "Unknown";
                    try
                    {
                        var process = Process.GetProcessById(entry.ProcessId);
                        processName = process.ProcessName;
                    }
                    catch { }

                    result.Add(new ConnectionInfo
                    {
                        ProcessName = processName,
                        Pid = entry.ProcessId,
                        LocalPort = entry.LocalPort,
                        RemoteAddress = "0.0.0.0:0",
                        Protocol = "UDP",
                        Status = "Listening"
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка мониторинга: {ex.Message}");
            }

            return result;
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
                            LocalPort = row.LocalPort,
                            RemotePort = row.RemotePort,
                            RemoteAddress = row.RemoteAddress,
                            State = (TcpState)row.State // ЯВНОЕ ПРИВЕДЕНИЕ
                        });
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch { }

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
                            LocalPort = row.LocalPort
                        });
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            catch { }

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
            public int RemoteAddress;
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
            public int RemoteAddress { get; set; }
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