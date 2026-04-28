namespace AcadsJulie.Models
{
    public class GameSession
    {
        public string GameId { get; set; } = "";
        public string Category { get; set; } = ""; // Memory, Focus, Logic, Speed, Trivia
        public string? SubCategory { get; set; }   // e.g. Trivia_WorldHistory_Hard
        public int Score { get; set; }
        public double Accuracy { get; set; }
        public int DurationSeconds { get; set; }
        public string Difficulty { get; set; } = "Easy";
        public DateTime PlayedAt { get; set; } = DateTime.UtcNow;
    }

    public class GameInfo
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public string Icon { get; set; } = "";
        public string Difficulty { get; set; } = "Easy";
        public int HighScore { get; set; }
        public bool IsAvailable { get; set; } = true;
    }

    public class DailyChallenge
    {
        public DateTime Date { get; set; } = DateTime.Today;
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public bool IsCompleted { get; set; } = false;
        public int RewardXP { get; set; } = 100;
        public string? BadgeReward { get; set; }
        public int TargetScore { get; set; } = 200;
        public int CurrentScore { get; set; } = 0;
        public double Progress => TargetScore > 0 ? Math.Min(1.0, (double)CurrentScore / TargetScore) : 0;
    }

    public class CategoryProgress
    {
        public string Category { get; set; } = "";
        public int Score { get; set; }
        public int MaxPossibleScore { get; set; } = 1000;
        public double ProgressValue => Math.Min(1.0, (double)Score / MaxPossibleScore);
        public List<int> WeeklyScores { get; set; } = [];
        public int GamesPlayed { get; set; }
        public int HighScore { get; set; }
    }
}
