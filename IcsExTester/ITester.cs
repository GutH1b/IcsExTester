using System.Text;

namespace IcsExTester
{
    internal interface ITester
    {
        string GenerateRandomTest();

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
    }
}
