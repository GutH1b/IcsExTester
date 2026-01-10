namespace IcsExTester
{
    static class TestComparer
    {
        public static bool CompareOutputs(string a, string b, int testNumber, bool stopOnDiff, bool printOnlyFailures)
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
                    if (equal && stopOnDiff)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow; // Set text color to yellow
                        Console.WriteLine("A:");
                        for (int p = 0; p < lineA.Length; p++)
                        {
                            Console.Write(lineA[p]);
                            if (p != lineA.Length - 1)
                                Console.Write("|"); // Add '|' between characters
                        }
                        Console.WriteLine();
                       
                        Console.ReadKey();

                        Console.WriteLine("B:");
                        for (int p = 0; p < lineB.Length; p++)
                        {
                            Console.Write(lineB[p]);
                            if (p != lineB.Length - 1)
                                Console.Write("|"); // Add '|' between characters
                        }
                        Console.WriteLine();
                        Console.ResetColor(); // Reset color back to default
                        Console.ReadKey();
                    }

                    equal = false;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Difference in test #{testNumber} at line {i + 1}:");
                    
                    Console.WriteLine($"   A: {lineA}");
                    Console.WriteLine($"   B: {lineB}");

                    Console.ResetColor();
                    if (stopOnDiff) return false;
                }
            }

            if (!printOnlyFailures && equal)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Test #{testNumber}: OK");
                Console.ResetColor();
            }

            return equal;
        }
    }
}
