using System;

namespace AcadsJulie.Models
{
    public class DailyQuest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        // Tracking
        public int TargetAmount { get; set; }
        public int CurrentAmount { get; set; }
        public int RewardXP { get; set; }
        public string RewardBadge { get; set; } = string.Empty;
        
        // Quest Conditions
        public string RequiredGame { get; set; } = "Any"; // "ColorTap", "OddOneOut", "Any"
        public string RequiredCategory { get; set; } = "Any"; // "Memory", "Logic", "Speed", "Focus", "Any"
        public string ConditionType { get; set; } = "PlayCount"; // "PlayCount", "AccuracyTarget", "ScoreTarget"
        public int ConditionTarget { get; set; } // e.g., 100 for 100% accuracy, 500 for 500 score
        
        // Status
        public bool IsCompleted => CurrentAmount >= TargetAmount;
        public bool IsClaimed { get; set; }
        public DateTime GeneratedDate { get; set; } = DateTime.Today;
        
        public double ProgressPercent => TargetAmount == 0 ? 0 : Math.Min(1.0, (double)CurrentAmount / TargetAmount);
    }
}
