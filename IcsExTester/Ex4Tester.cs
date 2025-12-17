using System.Text;

namespace IcsExTester
{
    internal class Ex4Tester : ITester
    {
        private Random rng = new Random();

        public string GenerateRandomTest()
        {
            StringBuilder sb = new StringBuilder();
            int numberOfActions = rng.Next(1, 4);

            for (int action = 0; action < numberOfActions; action++)
            {
                int menu = rng.Next(1, 6);
                sb.AppendLine(menu.ToString());

                switch (menu)
                {
                    case 1: GenerateTask1(sb); break;
                    case 2: GenerateTask2(sb); break;
                    case 3: GenerateTask3(sb); break;
                    case 4: GenerateTask4(sb); break;
                    case 5: GenerateTask5(sb); break;
                }
            }

            sb.AppendLine("6"); // exit program
            return sb.ToString();
        }

        void GenerateTask1(StringBuilder sb)
        {
            int len = rng.Next(1, 250);
            const string chars = "abcdefghijklmnopqrstuvwxyz123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ     ";
            StringBuilder phrase = new StringBuilder();
            for (int i = 0; i < len; i++) phrase.Append(chars[rng.Next(chars.Length)]);
            sb.AppendLine(phrase.ToString());
        }

        void GenerateTask2(StringBuilder sb)
        {
            int length = rng.Next(1, 40);
            sb.AppendLine(length.ToString());
            bool pal = rng.Next(2) == 0;
            char[] arr = new char[length];
            for (int i = 0; i < length; i++) arr[i] = (char)('a' + rng.Next(26));
            if (pal) for (int i = 0; i < length / 2; i++) arr[length - 1 - i] = arr[i];
            sb.AppendLine(new string(arr));
        }

        void GenerateTask3(StringBuilder sb)
        {
            int s = rng.Next(1, 4), v = rng.Next(1, 4), o = rng.Next(1, 4);
            sb.AppendLine(s.ToString());
            for (int i = 0; i < s; i++) sb.AppendLine(RandomWord());
            sb.AppendLine(v.ToString());
            for (int i = 0; i < v; i++) sb.AppendLine(RandomWord());
            sb.AppendLine(o.ToString());
            for (int i = 0; i < o; i++) sb.AppendLine(RandomWord());
        }

        string RandomWord()
        {
            int len = rng.Next(1, 20);
            StringBuilder w = new StringBuilder();
            for (int i = 0; i < len; i++) w.Append((char)('a' + rng.Next(26)));
            return w.ToString();
        }

        void GenerateTask4(StringBuilder sb)
        {
            int size = rng.Next(2, 20);
            sb.AppendLine(size.ToString());
            int highest = rng.Next(1, size * size + 1);
            int[,] grid = new int[size, size];
            List<(int r, int c)> cells = new List<(int, int)>();
            for (int r = 0; r < size; r++) for (int c = 0; c < size; c++) cells.Add((r, c));
            cells = cells.OrderBy(_ => rng.Next()).ToList();
            for (int i = 0; i < highest; i++)
            {
                var (r, c) = cells[i];
                grid[r, c] = i + 1;
            }
            for (int r = 0; r < size; r++)
            {
                StringBuilder row = new StringBuilder();
                for (int c = 0; c < size; c++)
                {
                    row.Append(grid[r, c]);
                    if (c + 1 < size) row.Append(' ');
                }
                sb.AppendLine(row.ToString());
            }
        }

        void GenerateTask5(StringBuilder sb)
        {
            int[,] board = new int[9, 9];
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    if (rng.Next(5) == 0)
                        for (int attempt = 0; attempt < 10; attempt++)
                        {
                            int n = rng.Next(1, 10);
                            if (IsSafe(board, r, c, n)) { board[r, c] = n; break; }
                        }

            for (int r = 0; r < 9; r++)
            {
                StringBuilder line = new StringBuilder();
                for (int c = 0; c < 9; c++)
                {
                    line.Append(board[r, c]);
                    if (c < 8) line.Append(' ');
                }
                sb.AppendLine(line.ToString());
            }
        }

        static bool IsSafe(int[,] board, int row, int col, int num)
        {
            for (int i = 0; i < 9; i++)
            {
                if (board[row, i] == num) return false;
                if (board[i, col] == num) return false;
            }

            int startRow = row / 3 * 3;
            int startCol = col / 3 * 3;
            for (int r = startRow; r < startRow + 3; r++)
                for (int c = startCol; c < startCol + 3; c++)
                    if (board[r, c] == num) return false;

            return true;
        }
    }
}
