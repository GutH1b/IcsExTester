using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

class DrMemoryResult
{
    public string ProgramOutput { get; set; } = "";
    public bool HasErrors { get; set; } = false;
    public string ErrorDetails { get; set; } = "";

    private static readonly Regex SummaryLineRegex = new Regex(
        @"\s*\d+\s+unique,\s+(\d+)\s+total.*(unaddressable|uninitialized|leak|possible leak)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    public string[] GetFilteredSummaryLines()
    {
        var lines = ErrorDetails.Split('\n');
        List<string> filtered = new List<string>();

        foreach (var line in lines)
        {
            var match = SummaryLineRegex.Match(line);
            if (match.Success)
            {
                int total = int.Parse(match.Groups[1].Value);
                if (total > 0)
                    filtered.Add(line.Trim());
            }
        }

        return filtered.ToArray();
    }

    public static void PrintDrMemoryResult(string[] filteredResult)
    {
        foreach (var line in filteredResult)
        {
            Console.ResetColor(); // reset first

            if (line.Contains("uninitialized access") || line.Contains("leak") && !line.Contains("possible leak"))
                Console.ForegroundColor = ConsoleColor.Red;
            else if (line.Contains("possible leak"))
                Console.ForegroundColor = ConsoleColor.Yellow;

            Console.WriteLine(line);
        }

        Console.ResetColor(); // final reset
        Console.WriteLine();
    }

    public bool HasDefiniteLeaks()
    {
        return GetDefiniteLeakLines().Length > 0;
    }

    public string[] GetDefiniteLeakLines()
    {
        return GetFilteredSummaryLines()
            .Where(l =>
                l.Contains("leak", StringComparison.OrdinalIgnoreCase) &&
               !l.Contains("possible leak", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}
