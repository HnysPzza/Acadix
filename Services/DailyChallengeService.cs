using AcadsJulie.Models;
using System.Text.Json;

namespace AcadsJulie.Services
{
    public class DailyChallengeService
    {
        private const string ChallengeKey = "daily_challenge";
        private const string HistoryKey = "challenge_history";

        private static readonly List<(string Title, string Desc, string Category, int Target, int XP)> ChallengeTemplates =
        [
            ("Memory Master", "Score 300+ points in any Memory game", "Memory", 300, 150),
            ("Speed Demon", "Complete a Speed game in under 60 seconds", "Speed", 200, 120),
            ("Logic Legend", "Solve 5 Logic puzzles correctly", "Logic", 250, 130),
            ("Focus Champion", "Achieve 90%+ accuracy in a Focus game", "Focus", 270, 140),
            ("Daily Grind", "Play 3 different games today", "Mixed", 3, 100),
            ("Brain Blast", "Earn 500+ total points across any games", "Mixed", 500, 200),
            ("Memory Sprint", "Complete 2 Memory games back-to-back", "Memory", 400, 160),
            ("Quick Thinker", "Beat your personal best in any Speed game", "Speed", 300, 180),
        ];

        public DailyChallenge GetTodayChallenge()
        {
            var json = ScopedPreferences.Get(ChallengeKey, null);
            if (json != null)
            {
                var stored = JsonSerializer.Deserialize<DailyChallenge>(json);
                if (stored?.Date.Date == DateTime.Today)
                    return stored;
            }
            return GenerateNewChallenge();
        }

        private DailyChallenge GenerateNewChallenge()
        {
            var today = DateTime.Today;
            var idx = today.DayOfYear % ChallengeTemplates.Count;
            var template = ChallengeTemplates[idx];
            var challenge = new DailyChallenge
            {
                Date = today,
                Title = template.Title,
                Description = template.Desc,
                Category = template.Category,
                RewardXP = template.XP,
                TargetScore = template.Target,
                IsCompleted = false,
                CurrentScore = 0,
                BadgeReward = template.Category == "Memory" ? "🧠" :
                              template.Category == "Speed" ? "⚡" :
                              template.Category == "Logic" ? "🧩" :
                              template.Category == "Focus" ? "🎯" : "🌟"
            };
            SaveChallenge(challenge);
            return challenge;
        }

        public void ProcessGameResult(GameResult result)
        {
            var challenge = GetTodayChallenge();
            if (challenge.IsCompleted)
                return;

            switch (challenge.Title)
            {
                case "Daily Grind":
                    SetChallengeProgress(challenge, App.ProgressService.GetSessions()
                        .Where(s => s.PlayedAt.Date == DateTime.Today)
                        .Select(s => s.GameId)
                        .Distinct()
                        .Count());
                    break;

                case "Brain Blast":
                    SetChallengeProgress(challenge, App.ProgressService.GetSessions()
                        .Where(s => s.PlayedAt.Date == DateTime.Today)
                        .Sum(s => s.Score));
                    break;

                case "Memory Sprint":
                    SetChallengeProgress(challenge, App.ProgressService.GetSessions()
                        .Count(s => s.PlayedAt.Date == DateTime.Today && s.Category == "Memory"));
                    break;

                case "Focus Champion":
                    if (result.Category == "Focus" && result.Accuracy >= 90)
                        CompleteChallenge(challenge);
                    break;

                case "Speed Demon":
                    if (result.Category == "Speed" && result.DurationSeconds <= 60)
                        UpdateChallengeProgress(result.Score);
                    break;

                case "Quick Thinker":
                    if (result.Category == "Speed")
                        UpdateChallengeProgress(result.Score);
                    break;

                default:
                    if (challenge.Category == "Mixed" || result.Category == challenge.Category)
                        UpdateChallengeProgress(result.Score);
                    break;
            }
        }

        public void UpdateChallengeProgress(int additionalScore)
        {
            var challenge = GetTodayChallenge();
            challenge.CurrentScore += additionalScore;
            CompleteIfReady(challenge);
            SaveChallenge(challenge);
        }

        private void SetChallengeProgress(DailyChallenge challenge, int score)
        {
            challenge.CurrentScore = Math.Max(challenge.CurrentScore, score);
            CompleteIfReady(challenge);
            SaveChallenge(challenge);
        }

        private void CompleteChallenge(DailyChallenge challenge)
        {
            challenge.CurrentScore = Math.Max(challenge.CurrentScore, challenge.TargetScore);
            CompleteIfReady(challenge);
            SaveChallenge(challenge);
        }

        private void CompleteIfReady(DailyChallenge challenge)
        {
            if (challenge.CurrentScore < challenge.TargetScore || challenge.IsCompleted)
                return;

            challenge.IsCompleted = true;
            App.ProfileService.AddXP(challenge.RewardXP);

            if (!string.IsNullOrWhiteSpace(challenge.BadgeReward))
                App.ProfileService.AddBadge(challenge.BadgeReward);

            AddToHistory(challenge);
        }

        private void SaveChallenge(DailyChallenge challenge)
        {
            ScopedPreferences.Set(ChallengeKey, JsonSerializer.Serialize(challenge));
        }

        private void AddToHistory(DailyChallenge challenge)
        {
            var history = GetHistory();
            if (!history.Any(h => h.Date.Date == challenge.Date.Date))
                history.Insert(0, challenge);
            if (history.Count > 7) history = history.Take(7).ToList();
            ScopedPreferences.Set(HistoryKey, JsonSerializer.Serialize(history));
        }

        public List<DailyChallenge> GetHistory()
        {
            var json = ScopedPreferences.Get(HistoryKey, null);
            return json != null ? JsonSerializer.Deserialize<List<DailyChallenge>>(json) ?? [] : [];
        }

        public TimeSpan TimeUntilMidnight => DateTime.Today.AddDays(1) - DateTime.Now;
    }
}
