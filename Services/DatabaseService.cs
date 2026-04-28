using AcadsJulie.Models;
using System.Threading.Tasks;

namespace AcadsJulie.Services
{
    public class DatabaseService
    {
        public Task SaveGameResultAsync(GameResult result)
        {
            var session = new GameSession
            {
                GameId = result.GameName,
                Category = result.Category,
                SubCategory = result.SubCategory,
                Score = result.Score,
                Accuracy = result.Accuracy,
                DurationSeconds = result.DurationSeconds,
                Difficulty = result.Difficulty,
                PlayedAt = result.PlayedAt
            };

            App.ProgressService.AddSession(session);
            App.ProfileService.UpdateCategoryScore(result.Category, result.Score);

            App.ProfileService.AddXP(result.Score / 10);

            App.ChallengeService.ProcessGameResult(result);
            App.QuestService.ProcessGameResult(result);

            if (result.GameName == "Trivia")
            {
                EvaluateTriviaAchievements(result);
            }

            App.QueueLeaderboardSync();
            return Task.CompletedTask;
        }

        private static void EvaluateTriviaAchievements(GameResult result)
        {
            var subCat = result.SubCategory ?? "";
            var isPerfect = result.Accuracy >= 99.9;
            var isHard = result.Difficulty.Equals("Hard", StringComparison.OrdinalIgnoreCase);
            var isEasy = result.Difficulty.Equals("Easy", StringComparison.OrdinalIgnoreCase);
            var goodAccuracy = result.Accuracy >= 80;

            if (isPerfect && isHard)
            {
                if (subCat.Contains("WorldHistory")) App.ProfileService.AddBadge("🏛️ World History Master");
                if (subCat.Contains("PhilippineHistory")) App.ProfileService.AddBadge("🇵🇭 Philippine History Scholar");
                if (subCat.Contains("Math")) App.ProfileService.AddBadge("🔢 Math Whiz");
                if (subCat.Contains("Science")) App.ProfileService.AddBadge("🔬 Science Sage");
                if (subCat.Contains("Space")) App.ProfileService.AddBadge("🚀 Space Explorer");
                if (subCat.Contains("Biology")) App.ProfileService.AddBadge("🧬 Biology Buff");
                if (subCat.Contains("Animals")) App.ProfileService.AddBadge("🦁 Animal Expert");
            }

            if (isEasy && goodAccuracy) App.ProfileService.AddBadge("📖 Trivia Novice");

            var triviaSessions = App.ProgressService.GetSessions()
                .Where(s => s.GameId == "Trivia" && s.Accuracy >= 99.9).Select(s => s.SubCategory ?? "").Distinct().Count();
            if (triviaSessions >= 3) App.ProfileService.AddBadge("🏆 Knowledge Champion");
        }
    }
}
