using System.Text;

namespace IcsExTester
{
    internal interface ITester
    {
        Test GenerateRandomTest();

        static string RandomWord(Random rng, string chars, int minLen = 3, int maxLen = 12)
        {
            int len = rng.Next(minLen, maxLen + 1);
            StringBuilder sb = new StringBuilder(len);
            for (int i = 0; i < len; i++)
                sb.Append(chars[rng.Next(chars.Length)]);
            return sb.ToString();
        }
        static string RandomWord(Random rng, int minLen = 3, int maxLen = 12)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ";
            int len = rng.Next(minLen, maxLen + 1);
            StringBuilder sb = new StringBuilder(len);
            for (int i = 0; i < len; i++)
                sb.Append(chars[rng.Next(chars.Length)]);
            return sb.ToString();
        }
        static string RandomWord(Random rng, int len)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ";
            StringBuilder sb = new StringBuilder(len);
            for (int i = 0; i < len; i++)
                sb.Append(chars[rng.Next(chars.Length)]);
            return sb.ToString();
        }

        protected static void AppendLine(StringBuilder sb, string line, string source)
        {
            sb.AppendLine($"[{source}] {line}");
        }
    }

    class Test
    {
        public string input;
        public string informativeInput;

        public Test()
        {
            this.input = "";
            this.informativeInput = "";
        }
        public Test(string input, string informativeInput)
        {
            this.input = input;
            this.informativeInput = informativeInput;
        }
        public Test(string informativeInput)
        {
            this.informativeInput = informativeInput;
            this.input = SanitizeInput(informativeInput);
        }

        private string SanitizeInput(string informativeInput)
        {
            // Remove the source tags like [GenerateAddRoom] or [GeneratePlay]
            var lines = informativeInput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            StringBuilder clean = new StringBuilder();

            foreach (var line in lines)
            {
                int idx = line.IndexOf("] ");
                if (idx >= 0)
                    clean.AppendLine(line[(idx + 2)..]); // Skip past "] "
                else
                    clean.AppendLine(line);
            }

            return clean.ToString().TrimEnd();
        }
    }
}
