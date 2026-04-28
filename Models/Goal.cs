namespace AcadsJulie.Models;

public class Goal
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Academic";
    public GoalType Type { get; set; } = GoalType.Daily;
    public int TargetValue { get; set; }
    public int CurrentValue { get; set; }
    public string Unit { get; set; } = "times";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }
    public bool IsCompleted { get; set; }
    public string IconEmoji { get; set; } = "🎯";
    
    public double ProgressPercentage => TargetValue > 0 ? (double)CurrentValue / TargetValue * 100 : 0;
    public string ProgressLabel => $"{CurrentValue}/{TargetValue} {Unit}";
}

public enum GoalType
{
    Daily,
    Weekly,
    Monthly,
    Custom
}

public class Habit
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Academic";
    public string IconEmoji { get; set; } = "✅";
    public List<DateTime> CompletedDates { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;
    
    public int CurrentStreak
    {
        get
        {
            if (CompletedDates.Count == 0) return 0;
            
            var sortedDates = CompletedDates.Select(d => d.Date).Distinct().OrderByDescending(d => d).ToList();
            var streak = 0;
            var checkDate = DateTime.Today;
            
            foreach (var date in sortedDates)
            {
                if (date == checkDate || date == checkDate.AddDays(-1))
                {
                    streak++;
                    checkDate = date.AddDays(-1);
                }
                else
                {
                    break;
                }
            }
            
            return streak;
        }
    }
    
    public bool CompletedToday => CompletedDates.Any(d => d.Date == DateTime.Today);
    public int TotalCompletions => CompletedDates.Count;
}

public class HabitLog
{
    public string HabitId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Notes { get; set; } = string.Empty;
}
