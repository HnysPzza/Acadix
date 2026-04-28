namespace AcadsJulie.Models
{
    public class UserProfile
    {
        public string Name { get; set; } = "Brain Trainer";
        public string AvatarInitials => Name.Length >= 2 ? Name[..2].ToUpper() : Name.ToUpper();
        public string? ProfileImagePath { get; set; }
        public int Level { get; set; } = 1;
        public int XP { get; set; } = 0;
        public int XPToNextLevel => Level * 500;
        public double XPProgress => (double)XP / XPToNextLevel;
        public int StreakDays { get; set; } = 0;
        public DateTime LastActiveDate { get; set; } = DateTime.MinValue;
        public DateTime LastCheckInDate { get; set; } = DateTime.MinValue;
        public bool NotificationsEnabled { get; set; } = true;
        public bool HapticsEnabled { get; set; } = true;
        public string DifficultyPreference { get; set; } = "Adaptive";
        public int AgeGroup { get; set; } = 0; // 0=Not set, 1=Elementary, 2=HighSchool, 3=College
        public string EducationStage { get; set; } = "JuniorHigh";
        public string PreferredTrack { get; set; } = string.Empty;
        public List<string> Badges { get; set; } = [];
        public bool OnboardingCompleted { get; set; } = false;
        public int MemoryScore { get; set; } = 0;
        public int FocusScore { get; set; } = 0;
        public int LogicScore { get; set; } = 0;
        public int SpeedScore { get; set; } = 0;
        public int TriviaBestScore { get; set; } = 0;
        public int BrainScore => (MemoryScore + FocusScore + LogicScore + SpeedScore) / 4;
    }
}
