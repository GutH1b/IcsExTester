using System.Diagnostics;

namespace IcsExTester
{
    class Program
    {
        #region Config & State

        static string exe1, exe2, drMemoryExe, version;
        static int numberOfTests, timeOutMS, exNum;
        static bool stopOnFirstDifference, checkAllMemoryFreed;
        static bool printOnlyFailures, debugInformativeInput;

        static readonly int[] memoryExercises = { 5, 6 };
        static List<Test>? predefinedTests;

        static readonly Stopwatch runStopwatch = new();
        static int failureFileId = 0;

        #endregion

        static void Main(string[] args)
        {
            LoadConfig();
            PrintStartingParamaters();
            RegisterShutdownHandlers();

            ITester tester = CreateTester();
            string? exeArguments = GetExeArguments(exNum);

            int mismatchCount = 0, timeoutCount = 0, leakCount = 0;
            bool allPassed = true;

            PrepareProgressBar();
            runStopwatch.Start();

            for (int i = 1; i <= numberOfTests; i++)
            {
                bool inputPrintedForTest = false;

                UpdateProgressBar(i - 1);

                Test test = GetTest(tester, i);

                if (!printOnlyFailures)
                    Console.WriteLine($"=== Test #{test} ===");

                if (RunAndHandleTimeout(test, exeArguments, ref timeoutCount, ref allPassed, ref inputPrintedForTest))
                    ShouldStopOnFailure(ref allPassed);

                var (out1, out2, time1, time2) = lastRun;

                bool same = HandleComparison(out1, out2, test, i, ref mismatchCount, ref allPassed, ref inputPrintedForTest);

                PrintTimings(time1, time2);

                if (same)
                    HandleMemoryChecks(test, exeArguments, time1, time2, ref leakCount, ref inputPrintedForTest);

                UpdateProgressBar(i);
            }

            FinishRun(mismatchCount, timeoutCount, leakCount, allPassed);
        }

        #region Main loop helpers

        static (string out1, string out2, int time1, int time2) lastRun;

        static bool RunAndHandleTimeout(Test test, string? exeArguments, ref int timeoutCount, 
            ref bool allPassed, ref bool inputPrintedForTest)
        {
            var t1 = Task.Run(() => ProcessRunner.RunProcessTimed(exe1, test.input, timeOutMS, exeArguments));
            var t2 = Task.Run(() => ProcessRunner.RunProcessTimed(exe2, test.input, timeOutMS, exeArguments));

            Task.WaitAll(t1, t2);

            var (out1, time1) = t1.Result;
            var (out2, time2) = t2.Result;
            lastRun = (out1, out2, time1, time2);

            if (!out1.StartsWith("[TIMEOUT") && !out2.StartsWith("[TIMEOUT"))
                return false;

            HandleFailure("TIMEOUT", ConsoleColor.Yellow, test, ref inputPrintedForTest, true, () =>
            {
                Console.WriteLine($"exe1 timeout: {out1.StartsWith("[TIMEOUT")}");
                Console.WriteLine($"exe2 timeout: {out2.StartsWith("[TIMEOUT")}");
            });

            timeoutCount++;
            allPassed = false;
            SaveFailingTest("timeout", test);
            return stopOnFirstDifference;
        }

        static bool HandleComparison(string out1, string out2, Test test, int index, 
                ref int mismatchCount, ref bool allPassed, ref bool inputPrintedForTest)
        {
            bool same = TestComparer.CompareOutputs(
                out1, out2, index, stopOnFirstDifference, printOnlyFailures);

            if (same)
                return true;

            HandleFailure("OUTPUT MISMATCH", ConsoleColor.Red, test, ref inputPrintedForTest, 
                printInformativeInput: false, extraInfo: null);

            mismatchCount++;
            allPassed = false;
            SaveFailingTest("mismatch", test);
            return false;
        }

        static void HandleMemoryChecks(Test test, string? exeArguments, int time1, int time2, ref int leakCount, ref bool inputPrintedForTest)
        {
            if (!checkAllMemoryFreed || !memoryExercises.Contains(exNum))
                return;

            RunDrMemory(exe1, time1, test, exeArguments, "exe1", ref leakCount, ref inputPrintedForTest);
            RunDrMemory(exe2, time2, test, exeArguments, "exe2", ref leakCount, ref inputPrintedForTest);
        }

        #endregion

        #region Failure handling

        static void HandleFailure(string title, ConsoleColor color, Test test, ref bool inputPrintedForTest,
            bool printInformativeInput, Action? extraInfo)
        {
            ClearProgressBarIfNeeded();

            Console.ForegroundColor = color;
            Console.WriteLine($"\n=== {title} ===");

            extraInfo?.Invoke();

            if (!inputPrintedForTest)
            {
                if (debugInformativeInput && printInformativeInput)
                    Console.WriteLine(test.informativeInput);

                Console.WriteLine(test.input);
                inputPrintedForTest = true;
            }

            Console.ResetColor();
        }

        static void RunDrMemory(string exe, int time, Test test, string? exeArguments, string exeName, 
            ref int leakCount, ref bool inputPrintedForTest)
        {
            int timeout = ProcessRunner.ComputeDrMemoryTimeout(time);

            var result = ProcessRunner.RunProcessWithDrMemory(
                exe, test.input, timeout, drMemoryExe, true, exeArguments);

            // Only act on definite leaks
            var definiteLeaks = result.GetDefiniteLeakLines();
            if (definiteLeaks.Length == 0)
                return;

            leakCount++;
            SaveFailingTest($"leak_{exeName}", test);

            HandleFailure($"DEFINITE MEMORY LEAK ({exeName})", ConsoleColor.Magenta, test, ref inputPrintedForTest, false, () =>
            {
                DrMemoryResult.PrintDrMemoryResult(definiteLeaks);
            });
        }

        #endregion

        #region Progress bar

        static void PrepareProgressBar()
        {
            if (printOnlyFailures)
                Console.WriteLine();
        }

        static void UpdateProgressBar(int current)
        {
            if (!printOnlyFailures || current == 0)
                return;

            double elapsed = runStopwatch.Elapsed.TotalSeconds;
            double rate = current / elapsed;
            double eta = (numberOfTests - current) / rate;

            int width = 40;
            int filled = (int)((double)current / numberOfTests * width);

            string bar = new string('█', filled) + new string('─', width - filled);

            Console.CursorVisible = false;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write($"[{bar}] {current}/{numberOfTests} | {rate:F1} t/s | ETA {TimeSpan.FromSeconds(eta):hh\\:mm\\:ss}");
        }

        static void ClearProgressBarIfNeeded()
        {
            if (!printOnlyFailures)
                return;

            int width = Console.WindowWidth;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', width));
            Console.SetCursorPosition(0, Console.CursorTop);
        }

        #endregion

        #region Setup / teardown

        static void LoadConfig()
        {
            ConfigManager.LoadConfig(
                ref exe1, ref exe2, ref numberOfTests, ref timeOutMS, ref stopOnFirstDifference, 
                ref exNum, ref drMemoryExe, ref checkAllMemoryFreed, ref version, 
                ref printOnlyFailures, ref debugInformativeInput, ref predefinedTests);
        }

        static void RegisterShutdownHandlers()
        {
            AppDomain.CurrentDomain.ProcessExit += (_, __) => ProcessRunner.KillAllProcesses();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                ProcessRunner.KillAllProcesses();
                Environment.Exit(1);
            };
        }

        static ITester CreateTester() => exNum switch
        {
            4 => new Ex4Tester(),
            5 => new Ex5Tester(),
            6 => new Ex6Tester(),
            _ => throw new Exception("Unsupported ExNum")
        };

        static Test GetTest(ITester tester, int index) =>
            predefinedTests != null && index <= predefinedTests.Count
                ? predefinedTests[index - 1]
                : tester.GenerateRandomTest();

        static void FinishRun(int mismatches, int timeouts, int leaks, bool allPassed)
        {
            Console.CursorVisible = true;
            Console.WriteLine();

            Console.WriteLine($"Total mismatches: {mismatches}");
            Console.WriteLine($"Total leaks (sum of both programs): {leaks}");
            Console.WriteLine($"Total timeouts: {timeouts}");

            if (allPassed)
                Console.WriteLine("\nAll tests passed.\nArgazim incoming.");

            ProcessRunner.KillAllProcesses();
        }

        #endregion

        #region Misc

        static string? GetExeArguments(int exNum) =>
            exNum == 6 ? "1000 10" : null;

        static void SaveFailingTest(string reason, Test test)
        {
            Directory.CreateDirectory("Failures");
            failureFileId++;

            File.WriteAllText(
                Path.Combine("Failures", $"{reason}_{failureFileId:D4}.txt"),
                (debugInformativeInput ? test.informativeInput + "\n" : "") + test.input);
        }

        static void PrintStartingParamaters()
        {
            Console.WriteLine("Starting with parameters:");
            Console.WriteLine($"\tExe1: {exe1}");
            Console.WriteLine($"\tExe2: {exe2}");
            Console.WriteLine($"\tNumberOfTests: {numberOfTests}");
            Console.WriteLine($"\tTimeoutMS: {(timeOutMS == 0 ? "Unlimited" : timeOutMS)}");
            Console.WriteLine($"\tStopOnFirstDifference: {stopOnFirstDifference}");
            Console.WriteLine($"\tExNum: {exNum}");
            Console.WriteLine($"\tCheckAllMemoryFreed: {checkAllMemoryFreed}");
            Console.WriteLine($"\tDrMemoryExe: {drMemoryExe}");
            Console.WriteLine($"\tVersion: {version}");
            Console.WriteLine();
        }

        static bool ShouldStopOnFailure(ref bool allPassed)
        {
            if (!stopOnFirstDifference)
                return false;

            allPassed = false;
            return true;
        }

        static void PrintTimings(int time1, int time2)
        {
            if (!printOnlyFailures)
            {
                Console.WriteLine($"Normal time exe1: {time1 / 1000.0}(s)");
                Console.WriteLine($"Normal time exe2: {time2 / 1000.0}(s)");
            }
        }

        #endregion
    }
}
