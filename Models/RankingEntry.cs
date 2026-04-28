namespace AcadsJulie.Models;

public class RankingEntry
{
    public int Rank { get; set; }
    public string Uid { get; set; } = string.Empty;
    public string DisplayName { get; set; } = "Student";
    public string Email { get; set; } = string.Empty;
    public int BrainScore { get; set; }
    public int Level { get; set; }
    public int XP { get; set; }
    public int StreakDays { get; set; }
    public int GamesPlayed { get; set; }
    public int TriviaBestScore { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsCurrentUser { get; set; }
}
