using AcadsJulie.Models;
using System.Text.Json;

namespace AcadsJulie.Services
{
    public class ProgressService
    {
        private const string SessionsKey = "game_sessions";
        private List<GameSession>? _cachedSessions;

        public List<GameSession> GetSessions()
        {
            if (_cachedSessions != null) return _cachedSessions;
            var json = Preferences.Get(SessionsKey, null);
            if (json != null)
                _cachedSessions = JsonSerializer.Deserialize<List<GameSession>>(json) ?? [];
            else
                _cachedSessions = [];
            return _cachedSessions;
        }

        public void AddSession(GameSession session)
        {
            var sessions = GetSessions();
            sessions.Add(session);
            // Keep only last 200 sessions
            if (sessions.Count > 200)
                sessions = sessions.Skip(sessions.Count - 200).ToList();
            _cachedSessions = sessions;
            Preferences.Set(SessionsKey, JsonSerializer.Serialize(sessions));
            App.QueueLeaderboardSync();
        }

        public CategoryProgress GetCategoryProgress(string category)
        {
            var sessions = GetSessions().Where(s => s.Category == category).ToList();
            var weeklyScores = new List<int>();
            for (int i = 6; i >= 0; i--)
            {
                var day = DateTime.Today.AddDays(-i);
                var dayScore = sessions.Where(s => s.PlayedAt.Date == day).Sum(s => s.Score);
                weeklyScores.Add(dayScore);
            }
            return new CategoryProgress
            {
                Category = category,
                Score = sessions.Count > 0 ? sessions.Max(s => s.Score) : 0,
                WeeklyScores = weeklyScores,
                GamesPlayed = sessions.Count,
                HighScore = sessions.Count > 0 ? sessions.Max(s => s.Score) : 0
            };
        }

        public List<int> GetWeeklyActivityCounts()
        {
            var sessions = GetSessions();
            var result = new List<int>();
            for (int i = 6; i >= 0; i--)
            {
                var day = DateTime.Today.AddDays(-i);
                result.Add(sessions.Count(s => s.PlayedAt.Date == day));
            }
            return result;
        }

        public double GetAverageCategoryAccuracy(string category)
        {
            var sessions = GetSessions().Where(s => s.Category == category).ToList();
            if (sessions.Count == 0) return 0;
            return sessions.Average(s => s.Accuracy);
        }

        public List<int> GetMonthlyScoreProgression()
        {
            var sessions = GetSessions();
            var result = new List<int>();
            for (int i = 29; i >= 0; i--)
            {
                var day = DateTime.Today.AddDays(-i);
                result.Add(sessions.Where(s => s.PlayedAt.Date == day).Sum(s => s.Score));
            }
            return result;
        }

        public List<GameInfo> GetGamesForCategory(string category)
        {
            return category switch
            {
                "Memory" =>
                [
                    new GameInfo { Id = "CardMatch", Name = "Card Match", Description = "Match pairs of cards as fast as you can", Category = "Memory", Icon = "🃏", Difficulty = GetLastDifficulty("CardMatch") },
                    new GameInfo { Id = "mem2", Name = "Sequence Recall", Description = "Memorize and repeat patterns", Category = "Memory", Icon = "🔢", Difficulty = GetLastDifficulty("mem2"), IsAvailable = false },
                    new GameInfo { Id = "mem3", Name = "Word Memory", Description = "Remember words from a list", Category = "Memory", Icon = "📝", Difficulty = GetLastDifficulty("mem3"), IsAvailable = false },
                ],
                "Focus" =>
                [
                    new GameInfo { Id = "OddOneOut", Name = "Odd One Out", Description = "Find the option that doesn't belong", Category = "Focus", Icon = "🔍", Difficulty = GetLastDifficulty("OddOneOut") },
                    new GameInfo { Id = "foc2", Name = "Color Tap", Description = "Tap only the matching color shapes", Category = "Focus", Icon = "🎯", Difficulty = GetLastDifficulty("foc2"), IsAvailable = false },
                    new GameInfo { Id = "foc3", Name = "Silent Count", Description = "Count specific items ignoring distractors", Category = "Focus", Icon = "🧮", Difficulty = GetLastDifficulty("foc3"), IsAvailable = false },
                ],
                "Logic" =>
                [
                    new GameInfo { Id = "Sequence", Name = "Next in Sequence", Description = "Predict what comes next in the pattern", Category = "Logic", Icon = "🧩", Difficulty = GetLastDifficulty("Sequence") },
                    new GameInfo { Id = "log2", Name = "Number Grid", Description = "Solve grid-based number puzzles", Category = "Logic", Icon = "🔲", Difficulty = GetLastDifficulty("log2"), IsAvailable = false },
                    new GameInfo { Id = "log3", Name = "Balance Scale", Description = "Determine the heavier side", Category = "Logic", Icon = "⚖️", Difficulty = GetLastDifficulty("log3"), IsAvailable = false },
                ],
                "Speed" =>
                [
                    new GameInfo { Id = "QuickMath", Name = "Quick Math", Description = "Solve math problems as fast as possible", Category = "Speed", Icon = "⚡", Difficulty = GetLastDifficulty("QuickMath") },
                    new GameInfo { Id = "spd2", Name = "Reaction Tap", Description = "Tap when the target appears", Category = "Speed", Icon = "👆", Difficulty = GetLastDifficulty("spd2"), IsAvailable = false },
                    new GameInfo { Id = "spd3", Name = "Word Sprint", Description = "Identify words at high speed", Category = "Speed", Icon = "💨", Difficulty = GetLastDifficulty("spd3"), IsAvailable = false },
                ],
                _ => []
            };
        }

        private string GetLastDifficulty(string gameId)
        {
            var lastSession = GetSessions().Where(s => s.GameId == gameId).OrderByDescending(s => s.PlayedAt).FirstOrDefault();
            return lastSession?.Difficulty ?? "Easy";
        }

        public int GetHighScore(string gameId)
        {
            var sessions = GetSessions().Where(s => s.GameId == gameId).ToList();
            return sessions.Count > 0 ? sessions.Max(s => s.Score) : 0;
        }

        public List<Achievement> GetMasterAchievements()
        {
            var list = new List<Achievement>
            {
                new Achievement { Title = "Deadeye", Description = "Achieve a perfect 100% accuracy in any game.", Condition = "Attain 100% accuracy in a Daily Challenge.", Icon = "🎯" },
                new Achievement { Title = "Marathon Runner", Description = "Play 10 games of any type.", Condition = "Play 10 games during a single Daily Challenge requirement.", Icon = "🏃" },
                new Achievement { Title = "Math Genius", Description = "Answer all Math questions quickly and correctly.", Condition = "Score 1000 in Quick Math.", Icon = "🧠" },
                new Achievement { Title = "Eagle Eye", Description = "Find the Odd One Out instantly.", Condition = "Score 800 in Focus games.", Icon = "🦅" },
                new Achievement { Title = "Memory Master", Description = "Complete Card Match flawlessly.", Condition = "Complete Card Match without any mistakes.", Icon = "🃏" },
                new Achievement { Title = "Speed Demon", Description = "Finish a game with high speed.", Condition = "Accumulate over 2000 Speed Score.", Icon = "⚡" },
                new Achievement { Title = "Logic Prodigy", Description = "Solve puzzles in a row without breaking a streak.", Condition = "Achieve a 5 Combo in Logic.", Icon = "🧩" },
                new Achievement { Title = "Consistent Scholar", Description = "Return to AcadsJulie multiple days.", Condition = "Reach a 7 day streak.", Icon = "🔥" },
                new Achievement { Title = "Brain Boss", Description = "Maximize your total Brain Score.", Condition = "Reach a Brain Score of 1000.", Icon = "👑" },
                new Achievement { Title = "World History Master", Description = "Perfect score on World History Hard.", Condition = "Get 100% on World History Hard.", Icon = "🏛️" },
                new Achievement { Title = "Philippine History Scholar", Description = "Perfect score on Philippine History Hard.", Condition = "Get 100% on Philippine History Hard.", Icon = "🇵🇭" },
                new Achievement { Title = "Math Whiz", Description = "Perfect score on Math Hard.", Condition = "Get 100% on Math Hard.", Icon = "🔢" },
                new Achievement { Title = "Science Sage", Description = "Perfect score on Science Hard.", Condition = "Get 100% on Science Hard.", Icon = "🔬" },
                new Achievement { Title = "Space Explorer", Description = "Perfect score on Space Hard.", Condition = "Get 100% on Space Hard.", Icon = "🚀" },
                new Achievement { Title = "Biology Buff", Description = "Perfect score on Biology Hard.", Condition = "Get 100% on Biology Hard.", Icon = "🧬" },
                new Achievement { Title = "Animal Expert", Description = "Perfect score on any Animals subfield Hard.", Condition = "Get 100% on Animals Land/Sea/Air Hard.", Icon = "🦁" },
                new Achievement { Title = "Trivia Novice", Description = "Complete any Trivia category Easy with 80%+.", Condition = "Complete Easy Trivia with 80%+ accuracy.", Icon = "📖" },
                new Achievement { Title = "Knowledge Champion", Description = "Perfect on 3 different Trivia categories.", Condition = "Get 100% on 3 different Trivia categories.", Icon = "🏆" }
            };
            return list;
        }

        public List<TriviaCategoryStat> GetTriviaCategoryStats()
        {
            var sessions = GetSessions().Where(s => s.GameId == "Trivia").ToList();
            var groups = sessions.GroupBy(s => s.SubCategory ?? "General").ToList();
            var result = new List<TriviaCategoryStat>();
            foreach (var g in groups)
            {
                result.Add(new TriviaCategoryStat
                {
                    SubCategory = g.Key,
                    BestScore = g.Max(s => s.Score),
                    GamesPlayed = g.Count(),
                    AverageAccuracy = g.Average(s => s.Accuracy)
                });
            }
            return result.OrderByDescending(r => r.BestScore).ToList();
        }
    }
}
