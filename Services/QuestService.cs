using AcadsJulie.Models;
using System.Text.Json;

namespace AcadsJulie.Services
{
    public class QuestService
    {
        private const string QuestsKey = "AcadsJulie_DailyQuests";
        private readonly ProfileService _profileService;

        public QuestService(ProfileService profileService)
        {
            _profileService = profileService;
        }

        public List<DailyQuest> GetTodayQuests()
        {
            var json = ScopedPreferences.Get(QuestsKey, string.Empty);
            List<DailyQuest>? quests = null;

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    quests = JsonSerializer.Deserialize<List<DailyQuest>>(json);
                }
                catch (JsonException ex)
                {
                    // Corrupt quest data: fall through and regenerate rather than crash, but
                    // leave a trace so it is not silently invisible during debugging.
                    System.Diagnostics.Debug.WriteLine($"[QuestService] Could not read saved quests: {ex.Message}");
                }
            }

            // If no quests exist or they are from a previous day, generate new ones
            if (quests == null || quests.Count == 0 || quests[0].GeneratedDate < DateTime.Today)
            {
                quests = GenerateQuests();
                SaveQuests(quests);
            }

            return quests;
        }

        public void SaveQuests(List<DailyQuest> quests)
        {
            var json = JsonSerializer.Serialize(quests);
            ScopedPreferences.Set(QuestsKey, json);
        }

        private List<DailyQuest> GenerateQuests()
        {
            var r = new Random();
            var quests = new List<DailyQuest>();

            // Quest 1: Play X games of a specific Category
            var categories = new[] { "Memory", "Focus", "Logic", "Speed", "Trivia" };
            string cat = categories[r.Next(categories.Length)];
            int playTarget = r.Next(2, 5);
            quests.Add(new DailyQuest
            {
                Title = $"{cat} Master",
                Description = $"Play {playTarget} games in the {cat} category.",
                TargetAmount = playTarget,
                RewardXP = playTarget * 50,
                RequiredCategory = cat,
                ConditionType = "PlayCount",
                GeneratedDate = DateTime.Today
            });

            // Quest 2: Score X points in a specific Game
            var games = new[] { "QuickMath", "Sequence", "OddOneOut", "CardMatch", "ReactionTap", "NumberGrid", "SequenceRecall", "ColorTap", "Trivia" };
            string game = games[r.Next(games.Length)];
            int scoreTarget = r.Next(3, 8) * 100; // 300 to 700
            quests.Add(new DailyQuest
            {
                Title = $"{game} Challenger",
                Description = $"Score {scoreTarget} points in a single {game} session.",
                TargetAmount = 1,
                RewardXP = 150,
                RequiredGame = game,
                ConditionType = "ScoreTarget",
                ConditionTarget = scoreTarget,
                GeneratedDate = DateTime.Today
            });

            // Quest 3: Harder Achievement Quest
            if (r.NextDouble() > 0.5)
            {
                quests.Add(new DailyQuest
                {
                    Title = "Sharpshooter 🎯",
                    Description = "Achieve a perfect 100% accuracy in any game.",
                    TargetAmount = 1,
                    RewardXP = 200,
                    RewardBadge = "🎯 Deadeye",
                    ConditionType = "AccuracyTarget",
                    ConditionTarget = 100,
                    GeneratedDate = DateTime.Today
                });
            }
            else
            {
                quests.Add(new DailyQuest
                {
                    Title = "Iron Marathon 🏃",
                    Description = "Play 10 games of any type.",
                    TargetAmount = 10,
                    RewardXP = 300,
                    RewardBadge = "🏃 Marathon Runner",
                    ConditionType = "PlayCount",
                    GeneratedDate = DateTime.Today
                });
            }

            return quests;
        }

        public void ProcessGameResult(GameResult result)
        {
            var quests = GetTodayQuests();
            bool updated = false;

            foreach (var q in quests)
            {
                if (q.IsCompleted) continue;

                bool isMatch = true;
                
                // Check Category requirement
                if (q.RequiredCategory != "Any" && q.RequiredCategory != result.Category)
                    isMatch = false;

                // Check Game requirement
                if (q.RequiredGame != "Any" && q.RequiredGame != result.GameName)
                    isMatch = false;

                if (!isMatch) continue;

                // Process based on condition
                if (q.ConditionType == "PlayCount")
                {
                    q.CurrentAmount++;
                    updated = true;
                }
                else if (q.ConditionType == "ScoreTarget" && result.Score >= q.ConditionTarget)
                {
                    q.CurrentAmount++;
                    updated = true;
                }
                else if (q.ConditionType == "AccuracyTarget" && result.Accuracy >= q.ConditionTarget)
                {
                    q.CurrentAmount++;
                    updated = true;
                }
            }

            if (updated)
            {
                SaveQuests(quests);
            }
        }

        public bool ClaimReward(string questId)
        {
            var quests = GetTodayQuests();
            var q = quests.FirstOrDefault(x => x.Id == questId);
            
            if (q != null && q.IsCompleted && !q.IsClaimed)
            {
                q.IsClaimed = true;
                SaveQuests(quests);

                _profileService.AddXP(q.RewardXP);
                
                // Achievement Logic
                if (!string.IsNullOrEmpty(q.RewardBadge))
                {
                    _profileService.AddBadge(q.RewardBadge);
                }
                
                return true;
            }
            return false;
        }
    }
}
