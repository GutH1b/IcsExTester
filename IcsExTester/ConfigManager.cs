using System.Text.Json;

namespace IcsExTester
{
    static class ConfigManager
    {
        const int DEFAULT_NUMBER_OF_TESTS = 10;
        const int DEFAULT_TIMEOUT_MS = 60000;
        const bool DEFAULT_STOP_ON_FIRST_DIFFERENCE = false;
        const int DEFAULT_EXNUM = 4;
        const string DEFAULT_DR_MEMORY_EXE = @"C:\Program Files\DrMemory\bin\drmemory.exe";
        const bool DEFAULT_CHECK_ALL_MEMORY_FREED = true;
        const string CURRENT_VERSION = "v0.2";

        public static void LoadConfig(ref string exe1, ref string exe2, ref int numberOfTests,
            ref int timeOutMS, ref bool stopOnFirstDifference, ref int exNum,
            ref string drMemoryExe, ref bool checkAllMemoryFreed, ref string version)
        {
            string configPath = Path.Combine(AppContext.BaseDirectory, "config.env");
            if (!File.Exists(configPath))
            {
                Console.WriteLine("Config file not found. Creating a new one...");
                WriteConfigFile(configPath, exe1, exe2);
            }

            try
            {
                string json = File.ReadAllText(configPath);
                using JsonDocument doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                exe1 = root.GetProperty("Exe1").GetString() ?? exe1;
                exe2 = root.GetProperty("Exe2").GetString() ?? exe2;
                numberOfTests = root.TryGetProperty("NumberOfTests", out var nt) ? nt.GetInt32() : DEFAULT_NUMBER_OF_TESTS;
                timeOutMS = root.TryGetProperty("TimeoutMS", out var tm) ? tm.GetInt32() : DEFAULT_TIMEOUT_MS;
                stopOnFirstDifference = root.TryGetProperty("StopOnFirstDifference", out var sod) ? sod.GetBoolean() : DEFAULT_STOP_ON_FIRST_DIFFERENCE;
                exNum = root.TryGetProperty("ExNum", out var ex) ? ex.GetInt32() : DEFAULT_EXNUM;

                drMemoryExe = root.TryGetProperty("DrMemoryExe", out var dr) ? dr.GetString() ?? DEFAULT_DR_MEMORY_EXE : DEFAULT_DR_MEMORY_EXE;
                checkAllMemoryFreed = root.TryGetProperty("CheckAllMemoryFreed", out var camf) ? camf.GetBoolean() : DEFAULT_CHECK_ALL_MEMORY_FREED;
                version = root.TryGetProperty("Version", out var ver) ? ver.GetString() ?? "" : "";

                // Version mismatch
                if (version != CURRENT_VERSION)
                {
                    Console.WriteLine("Updating config with new version fields...");
                    WriteConfigFile(configPath, exe1, exe2);
                }

                // Validate executables
                if (!File.Exists(exe1) || !File.Exists(exe2) || (checkAllMemoryFreed && !File.Exists(drMemoryExe)))
                {
                    Console.WriteLine("Executable paths invalid. Please update config.env");
                    Environment.Exit(1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read config: {ex.Message}");
                WriteConfigFile(configPath, "path to exe1", "path to exe2");
                Environment.Exit(1);
            }
        }

        static void WriteConfigFile(string configPath, string exe1, string exe2)
        {
            var defaultConfig = new Dictionary<string, object>
            {
                { "Exe1", exe1 },
                { "Exe2", exe2 },
                { "NumberOfTests", DEFAULT_NUMBER_OF_TESTS },
                { "TimeoutMS", DEFAULT_TIMEOUT_MS },
                { "StopOnFirstDifference", DEFAULT_STOP_ON_FIRST_DIFFERENCE },
                { "ExNum", DEFAULT_EXNUM },
                { "DrMemoryExe", DEFAULT_DR_MEMORY_EXE },
                { "CheckAllMemoryFreed", DEFAULT_CHECK_ALL_MEMORY_FREED },
                { "Version", CURRENT_VERSION }
            };

            string json = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);

            Console.WriteLine($"Created default config at: {configPath}");
            Console.WriteLine("Please review paths and restart.");
            Console.ReadKey();
            Environment.Exit(0);
        }
    }
}
