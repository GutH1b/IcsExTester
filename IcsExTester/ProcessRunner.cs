using System.Diagnostics;
using System.Text;

namespace IcsExTester
{
    static class ProcessRunner
    {
        const double DRMEM_SLOWDOWN_FACTOR = 10;
        const int DRMEM_GRACE_MS = 1000;
        const int MIN_DRMEM_TIMEOUT = 2000;

        public static List<Process> runningProcesses = new List<Process>();

        static ProcessRunner()
        {
            // Catch unobserved task exceptions
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Console.WriteLine("Unhandled exception in a task: " + e.Exception);
                KillAllProcesses();
                e.SetObserved();
            };
        }

        public static string RunProcess(string exePath, string input, int timeoutMS)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exePath,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var p = new Process { StartInfo = psi };
                p.Start();
                runningProcesses.Add(p);

                using var sw = p.StandardInput;
                sw.Write(input);
                if (!input.EndsWith("\n")) sw.Write("\n");

                int actualTimeout = timeoutMS == 0 ? Timeout.Infinite : timeoutMS;
                bool finished = p.WaitForExit(actualTimeout);

                if (!finished)
                {
                    try { p.Kill(true); } catch { }
                    return $"[TIMEOUT after {timeoutMS} ms]";
                }

                return p.StandardOutput.ReadToEnd();
            }
            catch (Exception ex)
            {
                return $"[ERROR: {ex.Message}]";
            }
        }

        public static DrMemoryResult RunProcessWithDrMemory(string exePath, string input, int timeoutMS, string drMemoryExe, bool checkMemory)
        {
            var result = new DrMemoryResult();
            if (!checkMemory)
            {
                result.ProgramOutput = RunProcess(exePath, input, timeoutMS);
                return result;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = drMemoryExe,
                    Arguments = $"-batch -- \"{exePath}\"",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };

                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                // Capture output asynchronously to avoid deadlocks
                p.OutputDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
                p.ErrorDataReceived += (s, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                using var sw = p.StandardInput;
                sw.Write(input);
                if (!input.EndsWith("\n")) sw.Write("\n");

                if (!p.WaitForExit(timeoutMS))
                {
                    try { p.Kill(true); } catch { }
                    result.HasErrors = true;
                    result.ErrorDetails = $"Dr. Memory process exceeded timeout ({timeoutMS}ms)";
                    return result;
                }

                // Ensure all async events are flushed
                p.WaitForExit();

                result.ProgramOutput = outputBuilder.ToString();
                result.ErrorDetails = outputBuilder.ToString() + "\n" + errorBuilder.ToString();

                // Mark as error if any relevant lines exist
                result.HasErrors = !string.IsNullOrWhiteSpace(result.ErrorDetails);

                return result;
            }
            catch (Exception ex)
            {
                result.HasErrors = true;
                result.ErrorDetails = ex.Message;
                return result;
            }
        }

        public static (string output, int elapsedMs) RunProcessTimed(string exePath, string input, int timeoutMS)
        {
            var sw = Stopwatch.StartNew();
            string output = RunProcess(exePath, input, timeoutMS);
            sw.Stop();
            return (output, (int)sw.ElapsedMilliseconds);
        }

        public static int ComputeDrMemoryTimeout(int normalMs)
        {
            if (normalMs <= 0) normalMs = 10;
            return Math.Max((int)(normalMs * DRMEM_SLOWDOWN_FACTOR) + DRMEM_GRACE_MS, MIN_DRMEM_TIMEOUT);
        }

        public static void KillAllProcesses()
        {
            foreach (var p in runningProcesses.ToArray())
            {
                try
                {
                    if (p == null)
                        continue;

                    if (!p.HasExited)
                    {
                        try
                        {
                            p.Kill(entireProcessTree: true);
                        }
                        catch { }

                        p.WaitForExit(200);
                    }
                }
                catch
                {
                    Console.WriteLine($"Failed to terminate process\n" +
                        $"\tpId: {p.Id}\n\tpName: {p.ProcessName}\nRe-attempting.");
                }
            }

            runningProcesses.Clear();
        }

    }
}
