using AcadsJulie.Models;
using System.Text.Json;

namespace AcadsJulie.Services
{
    public class ProfileService
    {
        private const string ProfileKey = "user_profile";
        private UserProfile? _cachedProfile;

        public UserProfile GetProfile()
        {
            if (_cachedProfile != null) return _cachedProfile;
            var json = ScopedPreferences.Get(ProfileKey, null);
            if (json != null)
            {
                _cachedProfile = JsonSerializer.Deserialize<UserProfile>(json) ?? new UserProfile();
            }
            else
            {
                _cachedProfile = new UserProfile();
            }
            return _cachedProfile;
        }

        public void SaveProfile(UserProfile profile)
        {
            _cachedProfile = profile;
            var json = JsonSerializer.Serialize(profile);
            ScopedPreferences.Set(ProfileKey, json);
            App.QueueLeaderboardSync();
        }

        public void AddXP(int amount)
        {
            if (amount <= 0) return;

            var profile = GetProfile();
            profile.XP += amount;
            while (profile.XP >= profile.XPToNextLevel)
            {
                profile.XP -= profile.XPToNextLevel;
                profile.Level++;
            }
            profile.LastActiveDate = DateTime.Today;
            SaveProfile(profile);
        }

        public bool HasCheckedInToday()
        {
            return GetProfile().LastCheckInDate.Date == DateTime.Today;
        }

        public int CheckInToday()
        {
            var profile = GetProfile();
            var today = DateTime.Today;
            if (profile.LastCheckInDate.Date == today)
                return 0;

            if (profile.LastCheckInDate.Date == today.AddDays(-1))
                profile.StreakDays++;
            else
                profile.StreakDays = 1;

            profile.LastCheckInDate = today;
            profile.LastActiveDate = today;

            var xpEarned = 10 + (profile.StreakDays >= 7 ? 20 : 0);
            SaveProfile(profile);
            AddXP(xpEarned);
            return xpEarned;
        }

        public void AddBadge(string badge)
        {
            var profile = GetProfile();
            if (!profile.Badges.Contains(badge))
            {
                profile.Badges.Add(badge);
                SaveProfile(profile);
            }
        }

        public void UpdateCategoryScore(string category, int score)
        {
            var profile = GetProfile();
            switch (category)
            {
                case "Memory": profile.MemoryScore = Math.Max(profile.MemoryScore, score); break;
                case "Focus": profile.FocusScore = Math.Max(profile.FocusScore, score); break;
                case "Logic": profile.LogicScore = Math.Max(profile.LogicScore, score); break;
                case "Speed": profile.SpeedScore = Math.Max(profile.SpeedScore, score); break;
                case "Trivia": profile.TriviaBestScore = Math.Max(profile.TriviaBestScore, score); break;
            }
            SaveProfile(profile);
        }
    }
}
