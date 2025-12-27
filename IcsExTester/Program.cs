using System.Diagnostics;

namespace IcsExTester
{
    class Program
    {
        static string exe1, exe2, drMemoryExe, version;
        static int numberOfTests, timeOutMS, exNum;
        static bool stopOnFirstDifference, checkAllMemoryFreed;
        static int[] memoryExercises = { 5 };

        static void Main(string[] args)
        {
            ConfigManager.LoadConfig(ref exe1, ref exe2, ref numberOfTests, ref timeOutMS,
                ref stopOnFirstDifference, ref exNum, ref drMemoryExe, ref checkAllMemoryFreed, ref version);

            PrintStartingParamaters();

            AppDomain.CurrentDomain.ProcessExit += (s, e) => ProcessRunner.KillAllProcesses();
            Console.CancelKeyPress += (s, e) => { e.Cancel = true; ProcessRunner.KillAllProcesses(); Environment.Exit(1); };

            ITester tester = exNum switch
            {
                4 => new Ex4Tester(),
                5 => new Ex5Tester(),
                6 => new Ex6Tester(),
                _ => throw new Exception("Unsupported ExNum")
            };

            bool allPassed = true;

            for (int test = 1; test <= numberOfTests; test++)
            {
                string input = tester.GenerateRandomTest();
                Console.WriteLine($"=== Test #{test} ===");

                // --- Normal runs in parallel ---
                var normalTask1 = Task.Run(() => ProcessRunner.RunProcessTimed(exe1, input, timeOutMS));
                var normalTask2 = Task.Run(() => ProcessRunner.RunProcessTimed(exe2, input, timeOutMS));

                Task.WaitAll(normalTask1, normalTask2);

                var (out1, time1) = normalTask1.Result;
                var (out2, time2) = normalTask2.Result;

                bool same = TestComparer.CompareOutputs(out1, out2, test, stopOnFirstDifference);

                if (!same)
                {
                    allPassed = false;
                    Console.WriteLine($"\nMismatch with input:\n{input}");
                    if (stopOnFirstDifference) break;
                }
                Console.WriteLine($"Normal time exe1: {time1/1000.0}(s)");
                Console.WriteLine($"Normal time exe2: {time2/1000.0}(s)");

                if (same && checkAllMemoryFreed && memoryExercises.Contains(exNum))
                {
                    int drTimeout1 = ProcessRunner.ComputeDrMemoryTimeout(time1);
                    int drTimeout2 = ProcessRunner.ComputeDrMemoryTimeout(time2);

                    var drTask1 = Task.Run(() => ProcessRunner.RunProcessWithDrMemory(exe1, input, drTimeout1, drMemoryExe, true));
                    var drTask2 = Task.Run(() => ProcessRunner.RunProcessWithDrMemory(exe2, input, drTimeout2, drMemoryExe, true));

                    Task.WaitAll(drTask1, drTask2);

                    var drmOut1 = drTask1.Result;
                    var drmOut2 = drTask2.Result;

                    string[] res1 = drmOut1.GetFilteredSummaryLines();
                    string[] res2 = drmOut2.GetFilteredSummaryLines();
                    if (drmOut1.HasErrors)
                    {
                        Console.WriteLine("Dr. Memory reported errors for exe1:");
                        DrMemoryResult.PrintDrMemoryResult(res1);
                    }
                    if (drmOut2.HasErrors)
                    {
                        Console.WriteLine($"Dr. Memory reported errors for exe2:");
                        DrMemoryResult.PrintDrMemoryResult(res2);
                    }
                    PrintLeaksWithInput(drmOut1, "exe1", input);
                    PrintLeaksWithInput(drmOut2, "exe2", input);
                }
            }

            if (allPassed) Console.WriteLine("\nAll tests passed.");
            ProcessRunner.KillAllProcesses();
        }

        static void PrintStartingParamaters()
        {
            Console.WriteLine("Starting with parameters:");
            Console.WriteLine($"\tExe1: {exe1}");
            Console.WriteLine($"\tExe2: {exe2}");
            Console.WriteLine($"\tNumberOfTests: {numberOfTests}");
            Console.WriteLine($"\tTimeoutMS: {(timeOutMS == 0 ? "Unlimited" : timeOutMS.ToString())}");
            Console.WriteLine($"\tStopOnFirstDifference: {stopOnFirstDifference}");
            Console.WriteLine($"\tExNum: {exNum}");
            Console.WriteLine($"\tCheckAllMemoryFreed: {checkAllMemoryFreed}");
            Console.WriteLine($"\tDrMemoryExe: {drMemoryExe}");
            Console.WriteLine($"\tVersion: {version}");
            Console.WriteLine();
        }
        static void PrintLeaksWithInput(DrMemoryResult drmResult, string exeName, string input)
        {
            var filteredLines = drmResult.GetFilteredSummaryLines();

            // only keep lines that actually mention "leak" or "possible leak"
            var leakLines = filteredLines
                .Where(line => line.Contains("leak", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (leakLines.Length > 0)
            {
                Console.WriteLine($"\nDr. Memory reported memory issues for {exeName} with input:\n{input}");
                DrMemoryResult.PrintDrMemoryResult(leakLines);
            }
        }
    }
}
