namespace IcsExTester
{
    static class TestComparer
    {
        public static bool CompareOutputs(string a, string b, int testNumber, bool stopOnDiff)
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
