using System;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Collections.Generic;

namespace Yaromir_Firewall_FINAL1
{
    public class FirewallService
    {
        private static FirewallService? _instance = null;
        public static FirewallService Instance => _instance ??= new FirewallService();

        private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "firewall_log.txt");

        private bool _running = false;
        private Thread? _worker = null;

        private void Log(string message)
        {
            try
            {
                File.AppendAllText(LogPath, $"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}");
            }
            catch { }
            Console.WriteLine(message);
        }

        public void Start()
        {
            if (_running) return;
            _running = true;
            _worker = new Thread(WorkerLoop);
            _worker.IsBackground = true;
            _worker.Start();

            try
            {
                // Сначала узкое разрешающее правило для локальной сети/точки доступа —
                // оно более специфично, чем широкий блок ниже, и Windows Firewall
                // применит именно его для трафика внутри локальной подсети.
                RunNetsh("advfirewall firewall delete rule name=\"Allow_LocalSubnet_Inbound\"");
                RunNetsh("advfirewall firewall add rule name=\"Allow_LocalSubnet_Inbound\" dir=in action=allow remoteip=localsubnet");

                RunNetsh("advfirewall firewall delete rule name=\"Block_All_Inbound\"");
                RunNetsh("advfirewall firewall add rule name=\"Block_All_Inbound\" dir=in action=block protocol=any");

                Log("[+] Базовая защита включена: интернет заблокирован, локальная сеть разрешена.");
            }
            catch (Exception ex)
            {
                Log($"[!] Ошибка блокировки: {ex.Message}");
            }

            ApplyBlacklist();
        }

        public void Stop()
        {
            _running = false;
            _worker?.Join(1000);

            try
            {
                RunNetsh("advfirewall firewall delete rule name=\"Block_All_Inbound\"");
                RunNetsh("advfirewall firewall delete rule name=\"Allow_LocalSubnet_Inbound\"");
                Log("[+] Базовая защита отключена.");
            }
            catch { }
        }

        private void WorkerLoop()
        {
            while (_running)
            {
                try { Thread.Sleep(1000); }
                catch { }
            }
        }

        public void ApplyBlacklist()
        {
            try
            {
                var settings = SettingsManager.Instance;

                foreach (var ruleName in GetBlockRuleNames())
                {
                    RunNetsh($"advfirewall firewall delete rule name=\"{ruleName}\"");
                }

                foreach (var name in settings.BlackList)
                {
                    AddBlockRuleInternal(name);
                }
            }
            catch (Exception ex)
            {
                Log($"[!] Ошибка применения чёрного списка: {ex.Message}");
            }
        }

        public void BlockProgram(string exeName, bool killRunning)
        {
            var settings = SettingsManager.Instance;

            if (!settings.BlackList.Contains(exeName))
            {
                settings.BlackList.Add(exeName);
                settings.Save();
            }

            AddBlockRuleInternal(exeName);

            if (killRunning)
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
                        catch { }
                    }
                }
                catch { }
            }
        }

        private void AddBlockRuleInternal(string exeName)
        {
            try
            {
                string fullPath = FindProcessPath(exeName);
                if (string.IsNullOrEmpty(fullPath))
                {
                    Log($"[!] Не удалось найти путь к {exeName}, правило не создано.");
                    return;
                }

                Log($"[i] Путь для {exeName}: {fullPath}");

                string ruleName = $"Block_{Path.GetFileNameWithoutExtension(exeName)}";
                RunNetsh($"advfirewall firewall add rule name=\"{ruleName}\" dir=in action=block program=\"{fullPath}\"");
                RunNetsh($"advfirewall firewall add rule name=\"{ruleName}_out\" dir=out action=block program=\"{fullPath}\"");
                Log($"[+] {exeName} заблокирован.");
            }
            catch (Exception ex)
            {
                Log($"[!] Ошибка создания правила для {exeName}: {ex.Message}");
            }
        }

        public void RemoveBlockRule(string exeName)
        {
            try
            {
                string ruleName = $"Block_{Path.GetFileNameWithoutExtension(exeName)}";
                RunNetsh($"advfirewall firewall delete rule name=\"{ruleName}\"");
                RunNetsh($"advfirewall firewall delete rule name=\"{ruleName}_out\"");
                Log($"[+] Правило для {exeName} удалено.");
            }
            catch (Exception ex)
            {
                Log($"[!] Ошибка удаления правила для {exeName}: {ex.Message}");
            }
        }

        public void RemoveAllBlockRules()
        {
            var namesBefore = GetBlockRuleNames();
            Log($"[i] Очистка: найдено правил для удаления: {namesBefore.Count} ({string.Join(", ", namesBefore)})");

            foreach (var ruleName in namesBefore)
            {
                RunNetsh($"advfirewall firewall delete rule name=\"{ruleName}\"");
            }

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

        private List<string> GetBlockRuleNames()
        {
            var names = new List<string>();
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "advfirewall firewall show rule name=all",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using (var p = Process.Start(psi))
                using (var reader = p!.StandardOutput)
                {
                    string? line;
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("Rule Name:"))
                        {
                            string name = line.Substring("Rule Name:".Length).Trim();
                            if (name.StartsWith("Block_", StringComparison.OrdinalIgnoreCase) &&
                                !name.Equals("Block_All_Inbound", StringComparison.OrdinalIgnoreCase) &&
                                seen.Add(name))
                            {
                                names.Add(name);
                            }
                        }
                    }
                }
            }
            catch { }
            return names;
        }

        private string FindProcessPath(string processName)
        {
            try
            {
                var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processName));
                foreach (var proc in processes)
                {
                    try
                    {
                        string path = proc.MainModule?.FileName ?? "";
                        if (!string.IsNullOrEmpty(path) && File.Exists(path))
                            return path;
                    }
                    catch { }
                }

                string[] searchPaths = {
                    @"C:\Program Files",
                    @"C:\Program Files (x86)",
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"D:\Program Files",
                    @"D:\Program Files (x86)"
                };

                foreach (var basePath in searchPaths)
                {
                    if (string.IsNullOrEmpty(basePath) || !Directory.Exists(basePath)) continue;

                    try
                    {
                        var files = Directory.GetFiles(basePath, processName, SearchOption.AllDirectories);
                        if (files.Length > 0)
                            return files[0];
                    }
                    catch { }
                }

                string systemDrive = Environment.GetEnvironmentVariable("SystemDrive") ?? "C:";
                try
                {
                    var files = Directory.GetFiles(systemDrive + "\\", processName, SearchOption.AllDirectories);
                    if (files.Length > 0)
                        return files[0];
                }
                catch { }
            }
            catch { }

            return string.Empty;
        }

        private void RunNetsh(string arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        process.WaitForExit();
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();

                        if (process.ExitCode != 0)
                        {
                            Log($"[!] netsh ошибка (код {process.ExitCode}): {error} | Команда: {arguments}");
                        }
                        else
                        {
                            Log($"[+] netsh OK: {arguments}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[!] Исключение netsh: {ex.Message}");
            }
        }

        public void KillProcess(int pid)
        {
            try
            {
                var proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch { }
        }

        /// <summary>
        /// Считает активные правила, созданные приложением: все Block_* (включая
        /// глобальный Block_All_Inbound) плюс правило локального исключения.
        /// </summary>
        public int GetRuleCount()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "advfirewall firewall show rule name=all",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                using (var p = Process.Start(psi))
                using (var reader = p!.StandardOutput)
                {
                    int count = 0;
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("Rule Name:"))
                        {
                            string name = line.Substring("Rule Name:".Length).Trim();
                            if (name.StartsWith("Block_", StringComparison.OrdinalIgnoreCase) ||
                                name.Equals("Allow_LocalSubnet_Inbound", StringComparison.OrdinalIgnoreCase))
                                count++;
                        }
                    }
                    return count;
                }
            }
            catch { return 0; }
        }
    }
}