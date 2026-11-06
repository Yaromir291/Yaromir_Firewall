using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Yaromir_Firewall_FINAL1
{
    /// <summary>
    /// Результат одного запуска netsh.exe.
    /// </summary>
    public sealed class NetshResult
    {
        public int ExitCode { get; init; }
        public string Output { get; init; } = string.Empty;
        public string Error { get; init; } = string.Empty;
        public IReadOnlyList<string> Commands { get; init; } = Array.Empty<string>();

        /// <summary>Человеческая метка пакета — попадает в лог, чтобы было понятно, ЧТО именно упало.</summary>
        public string Label { get; init; } = string.Empty;

        /// <summary>
        /// true = отказ ожидаемый (например, delete правила, которого и так нет).
        /// В лог пишется как информация, а не как авария.
        /// </summary>
        public bool Tolerated { get; init; }

        /// <summary>netsh завершился с кодом 0.</summary>
        public bool Success => ExitCode == 0;

        /// <summary>
        /// То, что должны проверять вызывающие: «всё в порядке ИЛИ отказ ожидаемый».
        /// Пример ожидаемого отказа — delete правила, которого и так не существует
        /// (netsh вернёт код 1). Без этого свойства UI показывал бы ложные ошибки
        /// при обычной разблокировке программы.
        /// </summary>
        public bool Ok => Success || Tolerated;

        /// <summary>Человекочитаемая причина отказа (для лога и для MessageBox).</summary>
        public string FailText
        {
            get
            {
                string text = string.IsNullOrWhiteSpace(Error) ? Output : Error;
                text = text.Trim();
                return text.Length == 0 ? $"netsh вернул код {ExitCode}" : text;
            }
        }
    }

    /// <summary>
    /// Запускает netsh ОДИН раз на целый пакет команд (netsh -f script.nsh).
    ///
    /// Зачем это нужно (раньше было 1 процесс netsh.exe на 1 правило):
    ///   1. Старт процесса netsh стоит 30–80 мс. Применение чёрного списка из 20 программ
    ///      раньше = 40 процессов = 2–4 секунды зависшего интерфейса. Теперь = 1 процесс.
    ///   2. Вывод читается асинхронно, ДО WaitForExit — классический дедлок при
    ///      переполнении pipe-буфера (4 КБ) исключён.
    ///   3. Есть таймаут: если netsh завис, мы не висим вместе с ним вечно.
    ///   4. Ошибки возвращаются наружу (событие Completed + NetshResult), а не тонут в catch { }.
    /// </summary>
    public static class NetshRunner
    {
        // Параллельные обращения к Base Filtering Engine Windows не любит —
        // очередь команд сериализуем.
        private static readonly SemaphoreSlim Gate = new SemaphoreSlim(1, 1);

        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(20);

        /// <summary>Срабатывает после КАЖДОГО запуска netsh — сюда подключается логгер.</summary>
        public static event Action<NetshResult>? Completed;

        /// <summary>
        /// Асинхронно выполнить пакет команд netsh. UI-поток не блокирует.
        /// </summary>
        public static async Task<NetshResult> RunAsync(
            IEnumerable<string> commands,
            string label = "",
            bool tolerateFailure = false,
            CancellationToken ct = default)
        {
            var list = commands
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .ToList();

            if (list.Count == 0)
                return new NetshResult { ExitCode = 0, Commands = list, Label = label, Tolerated = tolerateFailure };

            await Gate.WaitAsync(ct).ConfigureAwait(false);

            // netsh -f понимает скрипт в Unicode (UTF-16 LE с BOM).
            // Это важно: пути к программам могут содержать кириллицу ("C:\Программы\...").
            string script = Path.Combine(Path.GetTempPath(), $"yfw_{Guid.NewGuid():N}.nsh");

            try
            {
                File.WriteAllText(
                    script,
                    string.Join(Environment.NewLine, list) + Environment.NewLine,
                    Encoding.Unicode);

                return await ExecuteAsync(script, list, label, tolerateFailure, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var res = new NetshResult { ExitCode = -3, Error = ex.Message, Commands = list, Label = label, Tolerated = tolerateFailure };
                Completed?.Invoke(res);
                return res;
            }
            finally
            {
                Gate.Release();
                TryDelete(script);
            }
        }

        /// <summary>
        /// Синхронная обёртка. Использовать ТОЛЬКО там, где уже нет UI-потока
        /// (например, App.OnExit). В обработчиках кнопок — только RunAsync.
        /// </summary>
        public static NetshResult Run(IEnumerable<string> commands, string label = "", bool tolerateFailure = false)
        {
            try
            {
                return RunAsync(commands, label, tolerateFailure).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                return new NetshResult { ExitCode = -4, Error = ex.Message, Label = label, Tolerated = tolerateFailure };
            }
        }

        private static async Task<NetshResult> ExecuteAsync(
            string script, List<string> list, string label, bool tolerateFailure, CancellationToken ct)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $"-f \"{script}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(DefaultTimeout);

            using var process = new Process { StartInfo = psi };

            try
            {
                if (!process.Start())
                    return Finish(new NetshResult { ExitCode = -1, Error = "Не удалось запустить netsh.exe", Commands = list, Label = label, Tolerated = tolerateFailure });

                // ВАЖНО: начинаем вычитывать потоки СРАЗУ, до ожидания завершения.
                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);

                string stdout = await stdoutTask.ConfigureAwait(false);
                string stderr = await stderrTask.ConfigureAwait(false);

                return Finish(new NetshResult
                {
                    ExitCode = process.ExitCode,
                    Output = stdout,
                    Error = stderr,
                    Commands = list,
                    Label = label,
                    Tolerated = tolerateFailure
                });
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                return Finish(new NetshResult
                {
                    ExitCode = -2,
                    Error = $"netsh не завершился за {DefaultTimeout.TotalSeconds:0} с — процесс убит",
                    Commands = list,
                    Label = label,
                    Tolerated = tolerateFailure
                });
            }
            catch (Exception ex)
            {
                return Finish(new NetshResult { ExitCode = -3, Error = ex.Message, Commands = list, Label = label, Tolerated = tolerateFailure });
            }
        }

        private static NetshResult Finish(NetshResult result)
        {
            try { Completed?.Invoke(result); } catch { /* подписчик не должен ронять раннер */ }
            return result;
        }

        private static void TryKill(Process p)
        {
            try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }
}
