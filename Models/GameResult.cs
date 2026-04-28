namespace AcadsJulie.Models
{
    public class GameResult
    {
        public string GameName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? SubCategory { get; set; }
        public int Score { get; set; }
        public double Accuracy { get; set; }
        public int DurationSeconds { get; set; }
        public DateTime PlayedAt { get; set; } = DateTime.Now;
        public string Difficulty { get; set; } = "Easy";
    }
}
