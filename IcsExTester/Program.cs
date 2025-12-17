using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace IcsExTester
{
    class Program
    {
        static List<Process> runningProcesses = new List<Process>();
        static CancellationTokenSource cts = new CancellationTokenSource();

        const int DEFAULT_NUMBER_OF_TESTS = 10;
        const int DEFAULT_TIMEOUT_MS = 60000;
        const bool DEFAULT_STOP_ON_FIRST_DIFFERENCE = false;
        const int DEFAULT_EXNUM = 4;

        // Configurable variables
        static string exe1;
        static string exe2;
        static int numberOfTests;
        static int timeOutMS;
        static bool stopOnFirstDifference;
        static int exNum;

        static void Main(string[] args)
        {
            LoadConfig();

            Console.WriteLine("Starting with parameters:");
            Console.WriteLine($"  Exe1: {exe1}");
            Console.WriteLine($"  Exe2: {exe2}");
            Console.WriteLine($"  NumberOfTests: {numberOfTests}");
            Console.WriteLine($"  TimeoutMS: {(timeOutMS == 0 ? "Unlimited" : timeOutMS.ToString())}");
            Console.WriteLine($"  StopOnFirstDifference: {stopOnFirstDifference}");
            Console.WriteLine($"  ExNum: {exNum}");
            Console.WriteLine();

            AppDomain.CurrentDomain.ProcessExit += (s, e) => KillAllProcesses();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                KillAllProcesses();
                Environment.Exit(1);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                KillAllProcesses();
            };

            ITester tester = null;
            try
            {
                tester = exNum switch
                {
                    4 => new Ex4Tester(),
                    5 => new Ex5Tester(),
                    6 => new Ex6Tester(),
                    _ => throw new Exception("Unsupported ExNum. Must be 4, 5, or 6.")
                };
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Environment.Exit(1);
            }

            bool allPassed = true;
            Console.WriteLine($"Running {numberOfTests} tests for Ex{exNum}...\n");

            for (int test = 1; test <= numberOfTests; test++)
            {
                string input = tester.GenerateRandomTest();

                Console.WriteLine($"=== Test #{test} ===");

                Task<string> task1 = Task.Run(() => RunProcess(exe1, input, timeOutMS));
                Task<string> task2 = Task.Run(() => RunProcess(exe2, input, timeOutMS));

                Task.WaitAll(task1, task2);

                string out1 = task1.Result;
                string out2 = task2.Result;

                bool same = CompareOutputs(out1, out2, test, stopOnFirstDifference);

                if (!same)
                {
                    allPassed = false;
                    Console.WriteLine($"\nMismatch with input:\n{input}");
                    if (stopOnFirstDifference) break;
                }
            }

            if (allPassed)
                Console.WriteLine("\nAll tests completed successfully.\nArgazim Incoming.");

            KillAllProcesses();
            Console.WriteLine("Done");
        }

        static void LoadConfig()
        {
            string configPath = Path.Combine(AppContext.BaseDirectory, "config.env");
            if (!File.Exists(configPath))
            {
                Console.WriteLine("Config file not found. Creating a new one with default values...");

                var defaultConfig = new Dictionary<string, object>
                {
                    { "Exe1", "exe1 path" },
                    { "Exe2", "exe2 path" },
                    { "NumberOfTests", DEFAULT_NUMBER_OF_TESTS },
                    { "TimeoutMS", DEFAULT_TIMEOUT_MS },
                    { "StopOnFirstDifference", DEFAULT_STOP_ON_FIRST_DIFFERENCE },
                    { "ExNum", DEFAULT_EXNUM }
                };

                string json = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);

                Console.WriteLine($"Created default config at: {configPath}");
                Console.WriteLine("Please review and update paths as needed, then restart the application.");
                Environment.Exit(0);
            }

            try
            {
                string json = File.ReadAllText(configPath);
                using JsonDocument doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                exe1 = root.GetProperty("Exe1").GetString();
                exe2 = root.GetProperty("Exe2").GetString();

                numberOfTests = root.TryGetProperty("NumberOfTests", out var nt) ? nt.GetInt32() : DEFAULT_NUMBER_OF_TESTS;
                timeOutMS = root.TryGetProperty("TimeoutMS", out var tm) ? tm.GetInt32() : DEFAULT_TIMEOUT_MS;
                stopOnFirstDifference = root.TryGetProperty("StopOnFirstDifference", out var sod) ? sod.GetBoolean() : DEFAULT_STOP_ON_FIRST_DIFFERENCE;
                exNum = root.TryGetProperty("ExNum", out var ex) ? ex.GetInt32() : DEFAULT_EXNUM;

                if (!File.Exists(exe1))
                {
                    Console.WriteLine($"Exe1 not found at path: {exe1}");
                    Environment.Exit(1);
                }

                if (!File.Exists(exe2))
                {
                    Console.WriteLine($"Exe2 not found at path: {exe2}");
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read config: {ex.Message}");
            }
        }

        static string RunProcess(string exePath, string input, int timeoutMS)
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

                using var p = new Process { StartInfo = psi };
                p.Start();
                runningProcesses.Add(p);

                // Write input
                using var sw = p.StandardInput;
                sw.Write(input);
                if (!input.EndsWith("\n")) sw.Write("\n");

                StringBuilder output = new StringBuilder();
                using var sr = p.StandardOutput;

                // Determine actual timeout
                int actualTimeout = (timeoutMS == 0) ? Timeout.Infinite : timeoutMS;
                bool finished = p.WaitForExit(actualTimeout);

                if (!finished)
                {
                    try { p.Kill(true); } catch { }
                    return $"[TIMEOUT after {timeoutMS} ms]";
                }

                // Read all output
                output.Append(sr.ReadToEnd());
                return output.ToString();
            }
            catch (Exception ex)
            {
                return $"[ERROR: {ex.Message}]";
            }
        }

        static void KillAllProcesses()
        {
            foreach (var p in runningProcesses.ToArray())
            {
                try
                {
                    if (!p.HasExited)
                        p.Kill(true);
                }
                catch { }
            }
            runningProcesses.Clear();
        }

        static bool CompareOutputs(string a, string b, int testNumber, bool stopOnDiff)
        {
            string[] A = a.Replace("\r", "").Split('\n');
            string[] B = b.Replace("\r", "").Split('\n');

            bool equal = true;
            int max = Math.Max(A.Length, B.Length);

            for (int i = 0; i < max; i++)
            {
                string lineA = i < A.Length ? A[i] : "<missing>";
                string lineB = i < B.Length ? B[i] : "<missing>";

                if (lineA != lineB)
                {
                    equal = false;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Difference in test #{testNumber} at line {i + 1}:");
                    Console.WriteLine($"   A: {lineA}");
                    Console.WriteLine($"   B: {lineB}");
                    Console.ResetColor();
                    if (stopOnDiff) return false;
                }
            }

            if (equal)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Test #{testNumber}: OK");
                Console.ResetColor();
            }

            return equal;
        }
    }
}