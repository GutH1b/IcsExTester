using System.Text;

namespace IcsExTester
{
    internal class Ex5Tester : ITester
    {
        #region Parameters
        const int MIN_ACTIONS = 1;
        const int MAX_ACTIONS = 15;

        const int TVSHOW_NAME_MIN_LEN = 5;
        const int TVSHOW_NAME_MAX_LEN = 15;

        const int SEASON_NAME_MIN_LEN = 5;
        const int SEASON_NAME_MAX_LEN = 15;

        const int EPISODE_NAME_MIN_LEN = 5;
        const int EPISODE_NAME_MAX_LEN = 15;

        const int EPISODE_HOURS_MAX = 99;
        const int EPISODE_MINUTES_MAX = 59;
        const int EPISODE_SECONDS_MAX = 59;

        const int INVALID_LENGTH_PROBABILITY_PERCENT = 25;

        const int INVALID_HOURS_MAX = 999;
        const int INVALID_MINUTES_MAX = 999;
        const int INVALID_SECONDS_MAX = 999;

        const int MAIN_MENU_ADD = 1;
        const int MAIN_MENU_DELETE = 2;
        const int MAIN_MENU_PRINT = 3;
        const int MAIN_MENU_EXIT = 4;

        const int ADD_TVSHOW = 1;
        const int ADD_SEASON = 2;
        const int ADD_EPISODE = 3;

        const int DELETE_TVSHOW = 1;
        const int DELETE_SEASON = 2;
        const int DELETE_EPISODE = 3;

        const double DELETE_NONSENSE_QUOTIENT = 0.1;

        const int PRINT_TVSHOW = 1;
        const int PRINT_EPISODE = 2;
        const int PRINT_ARRAY = 3;
        #endregion

        static Random rng = new Random();
        public Test GenerateRandomTest()
        {
            StringBuilder sb = new StringBuilder();

            var shows = new List<string>();
            var seasons = new Dictionary<string, List<string>>();
            var episodes = new Dictionary<(string show, string season), List<(string name, string length)>>();

            int completed = 0;
            int target = rng.Next(MIN_ACTIONS, MAX_ACTIONS + 1);

            while (completed < target)
            {
                if (HandleRandomAction(sb, shows, seasons, episodes))
                    completed++;
            }

            sb.AppendLine(MAIN_MENU_EXIT.ToString());
            return new Test(sb.ToString());
        }

        static bool HandleRandomAction(StringBuilder sb, List<string> shows,
            Dictionary<string, List<string>> seasons,
            Dictionary<(string show, string season), List<(string name, string length)>> episodes)
        {
            // Determine feasible main actions
            var feasibleActions = new List<int>();
            feasibleActions.Add(MAIN_MENU_ADD); 
            //if (shows.Count > 0 || episodes.Count > 0)
                feasibleActions.Add(MAIN_MENU_DELETE);
            feasibleActions.Add(MAIN_MENU_PRINT);

            int mainChoice = feasibleActions[rng.Next(feasibleActions.Count)];

            return mainChoice switch
            {
                MAIN_MENU_ADD => HandleAdd(sb, shows, seasons, episodes),
                MAIN_MENU_DELETE => HandleDelete(sb, shows, seasons, episodes),
                MAIN_MENU_PRINT => HandlePrint(sb, shows, seasons, episodes),
                _ => false
            };
        }

        static bool HandleAdd(StringBuilder sb, List<string> shows,
            Dictionary<string, List<string>> seasons,
            Dictionary<(string show, string season), List<(string name, string length)>> episodes)
        {
            var feasibleAdds = new List<int> { ADD_TVSHOW };
            if (shows.Any(s => true)) feasibleAdds.Add(ADD_SEASON);
            if (shows.Any(s => seasons[s].Count > 0)) feasibleAdds.Add(ADD_EPISODE);

            if (feasibleAdds.Count == 0) return false;

            int choice = feasibleAdds[rng.Next(feasibleAdds.Count)];
            sb.AppendLine(MAIN_MENU_ADD.ToString());
            sb.AppendLine(choice.ToString());

            return choice switch
            {
                ADD_TVSHOW => AddTVShow(sb, shows, seasons),
                ADD_SEASON => AddSeason(sb, shows, seasons),
                ADD_EPISODE => AddEpisode(sb, shows, seasons, episodes),
                _ => false
            };
        }

        static bool HandleDelete(StringBuilder sb, List<string> shows,
            Dictionary<string, List<string>> seasons,
            Dictionary<(string show, string season), List<(string name, string length)>> episodes)
        {
            var feasibleDeletes = new List<int>();
            //if (shows.Count > 0) 
                feasibleDeletes.Add(DELETE_TVSHOW);
            //if (shows.Any(s => seasons[s].Count > 0)) 
                feasibleDeletes.Add(DELETE_SEASON);
            //if (episodes.Any(kv => kv.Value.Count > 0)) 
                feasibleDeletes.Add(DELETE_EPISODE);

            //if (feasibleDeletes.Count == 0) return false;

            int choice = feasibleDeletes[rng.Next(feasibleDeletes.Count)];
            sb.AppendLine(MAIN_MENU_DELETE.ToString());
            sb.AppendLine(choice.ToString());

            switch (choice)
            {
                case DELETE_TVSHOW: DeleteTVShow(sb, shows, seasons, episodes); break;
                case DELETE_SEASON: DeleteSeason(sb, shows, seasons, episodes); break;
                case DELETE_EPISODE: DeleteEpisode(sb, shows, seasons, episodes); break;
            }

            return true;
        }

        static bool HandlePrint(StringBuilder sb, List<string> shows,
            Dictionary<string, List<string>> seasons,
            Dictionary<(string show, string season), List<(string name, string length)>> episodes)
        {
            var possiblePrints = new List<int> { PRINT_ARRAY };
            if (shows.Count > 0) possiblePrints.Add(PRINT_TVSHOW);
            if (episodes.Any(kv => kv.Value.Count > 0)) possiblePrints.Add(PRINT_EPISODE);

            int choice = possiblePrints[rng.Next(possiblePrints.Count)];
            sb.AppendLine(MAIN_MENU_PRINT.ToString());
            sb.AppendLine(choice.ToString());

            switch (choice)
            {
                case PRINT_TVSHOW:
                    sb.AppendLine(shows[rng.Next(shows.Count)]);
                    break;

                case PRINT_EPISODE:
                    var validKeys = episodes.Where(kv => kv.Value.Count > 0).Select(kv => kv.Key).ToList();
                    var key = validKeys[rng.Next(validKeys.Count)];
                    sb.AppendLine(key.show);
                    sb.AppendLine(key.season);
                    sb.AppendLine(episodes[key][rng.Next(episodes[key].Count)].name);
                    break;

                case PRINT_ARRAY:
                    break;
            }

            return true;
        }

        #region Add/Delete Helpers

        static bool AddTVShow(StringBuilder sb, List<string> shows, Dictionary<string, List<string>> seasons)
        {
            string name = ITester.RandomWord(rng, TVSHOW_NAME_MIN_LEN, TVSHOW_NAME_MAX_LEN);
            if (!shows.Contains(name))
            {
                shows.Add(name);
                shows.Sort(StringComparer.Ordinal);
                seasons[name] = new List<string>();
            }
            sb.AppendLine(name);
            return true;
        }

        static bool AddSeason(StringBuilder sb, List<string> shows, Dictionary<string, List<string>> seasons)
        {
            string show = shows[rng.Next(shows.Count)];
            sb.AppendLine(show);

            string seasonName = ITester.RandomWord(rng, SEASON_NAME_MIN_LEN, SEASON_NAME_MAX_LEN);
            var showSeasons = seasons[show];
            if (!showSeasons.Contains(seasonName))
                showSeasons.Add(seasonName);

            sb.AppendLine(seasonName);
            sb.AppendLine(rng.Next(showSeasons.Count + 1).ToString());
            return true;
        }

        static bool AddEpisode(StringBuilder sb, List<string> shows, Dictionary<string, List<string>> seasons,
            Dictionary<(string show, string season), List<(string name, string length)>> episodes)
        {
            string show = shows.First(s => seasons[s].Count > 0);
            string season = seasons[show][rng.Next(seasons[show].Count)];

            sb.AppendLine(show);
            sb.AppendLine(season);

            string epName = ITester.RandomWord(rng, EPISODE_NAME_MIN_LEN, EPISODE_NAME_MAX_LEN);
            sb.AppendLine(epName);

            if (ShouldGenerateInvalidLength())
                sb.AppendLine(GenerateInvalidLength());

            sb.AppendLine(GenerateValidLength());
            sb.AppendLine(rng.Next(0, 10).ToString());

            var key = (show, season);
            if (!episodes.ContainsKey(key))
                episodes[key] = new List<(string, string)>();
            episodes[key].Add((epName, GenerateValidLength()));

            return true;
        }

        static void DeleteTVShow(StringBuilder sb, List<string> shows, Dictionary<string, List<string>> seasons,
    Dictionary<(string show, string season), List<(string name, string length)>> episodes)
        {
            bool tryNonsense = shows.Count == 0 || rng.NextDouble() < DELETE_NONSENSE_QUOTIENT;

            string show;
            if (tryNonsense)
            {
                // Remove nonsense from shows
                show = "NONSENSE_SHOW_";
                sb.AppendLine(show);
            }
            else
            {
                show = shows[rng.Next(shows.Count)];
                sb.AppendLine(show);

                shows.Remove(show);
                seasons.Remove(show);

                var keysToRemove = episodes.Keys.Where(k => k.show == show).ToList();
                foreach (var key in keysToRemove)
                    episodes.Remove(key);
            }
        }

        static void DeleteSeason(StringBuilder sb, List<string> shows, Dictionary<string, List<string>> seasons,
            Dictionary<(string show, string season), List<(string name, string length)>> episodes)
        {
            // Only consider shows with seasons
            var validShows = shows.Where(s => seasons.ContainsKey(s) && seasons[s].Count > 0).ToList();
            bool tryNonsense = validShows.Count == 0 || rng.NextDouble() < DELETE_NONSENSE_QUOTIENT;

            string show, season;
            if (tryNonsense)
            {
                show = "NONSENSE_SHOW_";
                season = "NONSENSE_SEASON_";
                sb.AppendLine(show);
                sb.AppendLine(season);
            }
            else
            {
                show = validShows[rng.Next(validShows.Count)];
                season = seasons[show][rng.Next(seasons[show].Count)];

                sb.AppendLine(show);
                sb.AppendLine(season);

                seasons[show].Remove(season);
                episodes.Remove((show, season));
            }
        }

        static void DeleteEpisode(StringBuilder sb, List<string> shows, Dictionary<string, List<string>> seasons,
            Dictionary<(string show, string season), List<(string name, string length)>> episodes)
        {
            var validKeys = episodes.Where(kv => kv.Value.Count > 0).Select(kv => kv.Key).ToList();
            bool tryNonsense = validKeys.Count == 0 || rng.NextDouble() < DELETE_NONSENSE_QUOTIENT;

            string show, season, epName;
            if (tryNonsense)
            {
                show = "NONSENSE_SHOW_";
                season = "NONSENSE_SEASON_";
                epName = "NONSENSE_EP_";

                sb.AppendLine(show);
                sb.AppendLine(season);
                sb.AppendLine(epName);
            }
            else
            {
                var key = validKeys[rng.Next(validKeys.Count)];
                show = key.show;
                season = key.season;

                int epIndex = rng.Next(episodes[key].Count);
                epName = episodes[key][epIndex].name;
                episodes[key].RemoveAt(epIndex);

                sb.AppendLine(show);
                sb.AppendLine(season);
                sb.AppendLine(epName);
            }
        }

        #endregion

        #region Length Helpers

        static bool ShouldGenerateInvalidLength() => rng.Next(0, 100) < INVALID_LENGTH_PROBABILITY_PERCENT;

        static string GenerateValidLength()
        {
            return $"{rng.Next(0, EPISODE_HOURS_MAX + 1):D2}:{rng.Next(0, EPISODE_MINUTES_MAX + 1):D2}:{rng.Next(0, EPISODE_SECONDS_MAX + 1):D2}";
        }

        static string GenerateInvalidLength()
        {
            int type = rng.Next(0, 7);

            return type switch
            {
                0 => ITester.RandomWord(rng, 1, 99),
                1 => $"{rng.Next(0, 99)}-{rng.Next(0, 99)}-{rng.Next(0, 99)}",
                2 => $"{rng.Next(0, 99)}:{rng.Next(0, 99)}",
                3 => $"{rng.Next(0, 99)}:{rng.Next(0, 99)}:{rng.Next(0, 99)}:{rng.Next(0, 99)}",
                4 => $"{rng.Next(0, INVALID_HOURS_MAX)}:{rng.Next(EPISODE_MINUTES_MAX + 1, INVALID_MINUTES_MAX)}:{rng.Next(EPISODE_SECONDS_MAX + 1, INVALID_SECONDS_MAX)}",
                5 => $"-{rng.Next(1, 10)}:{rng.Next(0, 59):D2}:{rng.Next(0, 59):D2}",
                _ => ""
            };
        }

        #endregion
    }
}
