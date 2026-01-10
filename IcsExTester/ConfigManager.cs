using System.IO;
using System.Text;
using System.Text.Json;

namespace IcsExTester
{
    static class ConfigManager
    {
        const int DEFAULT_NUMBER_OF_TESTS = 10;
        const int DEFAULT_TIMEOUT_MS = 15000;
        const bool DEFAULT_STOP_ON_FIRST_DIFFERENCE = false;
        const int DEFAULT_EXNUM = -1;
        const string DEFAULT_DR_MEMORY_EXE = @"C:\Program Files (x86)\Dr. Memory\bin64\drmemory.exe";
        const bool DEFAULT_CHECK_ALL_MEMORY_FREED = false;
        const string CURRENT_VERSION = "v0.3";
        const bool DEFAULT_PRINT_ONLY_FAILURES = false;
        const bool DEFAULT_DEBUG_INFORMATIVE_INPUT = false;

        public static void LoadConfig(
    ref string exe1, ref string exe2, ref int numberOfTests,
    ref int timeOutMS, ref bool stopOnFirstDifference, ref int exNum,
    ref string drMemoryExe, ref bool checkAllMemoryFreed, ref string version,
    ref bool printOnlyFailures, ref bool debugInformativeInput,
    ref List<Test>? predefinedTests)
        {
            string configPath = Path.Combine(AppContext.BaseDirectory, "config.env");

            if (!File.Exists(configPath))
                CreateDefaultConfigAndExit(configPath);

            Dictionary<string, object> patchedConfig;
            List<string> errors = new();

            try
            {
                patchedConfig = LoadAndPatchConfig(configPath, out version);

                // --- Read values ---
                exe1 = GetConfigValue<string>(patchedConfig, "Exe1");
                exe2 = GetConfigValue<string>(patchedConfig, "Exe2");
                numberOfTests = GetConfigValue<int>(patchedConfig, "NumberOfTests");
                timeOutMS = GetConfigValue<int>(patchedConfig, "TimeoutMS");
                stopOnFirstDifference = GetConfigValue<bool>(patchedConfig, "StopOnFirstDifference");
                exNum = GetConfigValue<int>(patchedConfig, "ExNum");
                drMemoryExe = GetConfigValue<string>(patchedConfig, "DrMemoryExe");
                checkAllMemoryFreed = GetConfigValue<bool>(patchedConfig, "CheckAllMemoryFreed");
                printOnlyFailures = GetConfigValue<bool>(patchedConfig, "PrintOnlyFailures");
                debugInformativeInput = GetConfigValue<bool>(patchedConfig, "DebugInformativeInput");

                string predefinedPath = GetConfigValue<string>(patchedConfig, "PredefinedTestsPath");
                if (!string.IsNullOrWhiteSpace(predefinedPath) && File.Exists(predefinedPath))
                {
                    predefinedTests = LoadPredefinedTests(predefinedPath);
                }
            }
            catch (JsonException je)
            {
                HandleCorruptConfig(configPath, je);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read config: {ex.Message}");
                Environment.Exit(1);
            }


            if (errors.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Config errors detected:");
                errors.ForEach(e => Console.WriteLine(" - " + e));
                Console.ResetColor();
                Environment.Exit(1);
            }
        }

        private static void CreateDefaultConfigAndExit(string path)
        {
            Console.WriteLine("Config file not found. Creating default config...");
            var cfg = GetDefaultConfig("path to exe1", "path to exe2");
            WriteConfig(path, cfg, exitAfter: true);
            Console.WriteLine("Please update config.env and restart.");
            Environment.Exit(0);
        }

        private static Dictionary<string, object> LoadAndPatchConfig(string path, out string version)
        {
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var rawProps = doc.RootElement.EnumerateObject().ToList();
            var raw = rawProps.ToDictionary(p => p.Name, p => p.Value);

            var defaults = GetDefaultConfig();
            var patchedConfig = new Dictionary<string, object>();
            bool needsRewrite = false;

            // --- Order check (QoL only) ---
            var desiredOrder = defaults.Keys.ToList();
            var actualOrder = rawProps.Select(p => p.Name).ToList();

            bool orderCorrect = IsOrderCorrect(actualOrder, desiredOrder);

            // Also rewrite if extra unknown keys exist
            bool hasUnknownKeys = actualOrder.Any(k => !defaults.ContainsKey(k));

            if (!orderCorrect || hasUnknownKeys)
            {
                Console.WriteLine("Config key order normalized.");
                needsRewrite = true;
            }

            foreach (var kv in defaults)
            {
                if (raw.TryGetValue(kv.Key, out var val))
                {
                    patchedConfig[kv.Key] = val.ValueKind switch
                    {
                        JsonValueKind.String => val.GetString(),
                        JsonValueKind.Number => val.GetInt32(),
                        JsonValueKind.True or JsonValueKind.False => val.GetBoolean(),
                        _ => kv.Value
                    };
                }
                else
                {
                    patchedConfig[kv.Key] = kv.Value;
                    needsRewrite = true;
                }
            }

            version = raw.TryGetValue("Version", out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString()! : "";

            if (version != CURRENT_VERSION)
            {
                Console.WriteLine($"Config version upgraded: {version} → {CURRENT_VERSION}");
                patchedConfig["Version"] = CURRENT_VERSION;
                needsRewrite = true;
            }

            if (needsRewrite)
                WriteConfig(path, patchedConfig);

            return patchedConfig;
        }

        private static T GetConfigValue<T>(Dictionary<string, object> config, string key)
        {
            if (!config.TryGetValue(key, out var value))
                throw new Exception($"Config key missing: {key}");

            return (T)Convert.ChangeType(value, typeof(T));
        }

        private static List<Test> LoadPredefinedTests(string path)
        {
            const string DELIMITER = "---";

            var tests = new List<Test>();
            var sb = new StringBuilder();
            int testIndex = 0;

            foreach (var rawLine in File.ReadLines(path))
            {
                var line = rawLine.TrimEnd();

                if (line == DELIMITER)
                {
                    if (sb.Length > 0)
                    {
                        testIndex++;
                        tests.Add(new Test
                        {
                            input = sb.ToString().TrimEnd(),
                            informativeInput = $"Predefined test #{testIndex}"
                        });
                        sb.Clear();
                    }
                    continue;
                }

                sb.AppendLine(rawLine);
            }

            // Handle last test if no trailing delimiter
            if (sb.Length > 0)
            {
                testIndex++;
                tests.Add(new Test
                {
                    input = sb.ToString().TrimEnd(),
                    informativeInput = $"Predefined test #{testIndex}"
                });
            }

            if (tests.Count == 0)
                throw new Exception("Predefined tests file contains no tests. Expected '---' delimiters.");

            return tests;
        }

        static void WriteConfig(string path, Dictionary<string, object> config, bool exitAfter = false)
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(path, json);

            if (exitAfter)
            {
                Console.WriteLine($"Created default config at: {path}");
                Console.WriteLine("Please review paths and restart.");
                Console.ReadKey();
                Environment.Exit(0);
            }
        }

        static Dictionary<string, object> GetDefaultConfig(string exe1 = "", string exe2 = "")
        {
            return new()
            {
                // Executables
                ["Exe1"] = exe1,
                ["Exe2"] = exe2,
                ["DrMemoryExe"] = DEFAULT_DR_MEMORY_EXE,

                // Test settings
                ["NumberOfTests"] = DEFAULT_NUMBER_OF_TESTS,
                ["TimeoutMS"] = DEFAULT_TIMEOUT_MS,
                ["ExNum"] = DEFAULT_EXNUM,
                ["StopOnFirstDifference"] = DEFAULT_STOP_ON_FIRST_DIFFERENCE,

                // Memory options
                ["CheckAllMemoryFreed"] = DEFAULT_CHECK_ALL_MEMORY_FREED,

                // Output options
                ["PrintOnlyFailures"] = DEFAULT_PRINT_ONLY_FAILURES,
                ["DebugInformativeInput"] = DEFAULT_DEBUG_INFORMATIVE_INPUT,

                // Predefined tests
                ["PredefinedTestsPath"] = "",

                // Config version
                ["Version"] = CURRENT_VERSION
            };
        }

        static bool IsOrderCorrect(IReadOnlyList<string> actualOrder, IReadOnlyList<string> desiredOrder)
        {
            int idx = 0;
            foreach (var key in actualOrder)
            {
                if (idx >= desiredOrder.Count)
                    return false;

                if (key != desiredOrder[idx])
                    return false;

                idx++;
            }

            return true;
        }

        static void HandleCorruptConfig(string path, Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Failed to read config file.");
            Console.WriteLine();
            Console.WriteLine($"Reason: {ex.Message}");
            Console.WriteLine();
            Console.WriteLine("The config file appears to be corrupted or empty.");
            Console.WriteLine("You may want to delete the following file and restart the program:");
            Console.WriteLine(path);
            Console.ResetColor();

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            Environment.Exit(1);
        }
    }
}
