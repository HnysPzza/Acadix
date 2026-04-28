using AcadsJulie.Models;
using AcadsJulie.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcadsJulie.ViewModels
{
    public partial class AchievementsViewModel : ObservableObject
    {
        private readonly ProfileService _profileService;
        private readonly ProgressService _progressService;

        [ObservableProperty]
        private ObservableCollection<Achievement> _achievements = new();

        [ObservableProperty]
        private int _totalAchievements;

        [ObservableProperty]
        private int _unlockedAchievements;

        [ObservableProperty]
        private double _progressPercentage;

        public AchievementsViewModel() : this(App.ProfileService, App.ProgressService)
        {
        }

        public AchievementsViewModel(ProfileService profileService, ProgressService progressService)
        {
            _profileService = profileService;
            _progressService = progressService;
        }

        public void LoadAchievements()
        {
            var userBadges = _profileService.GetProfile().Badges ?? new List<string>();
            var masterList = _progressService.GetMasterAchievements();

            Achievements.Clear();
            int unlockedCount = 0;

            foreach (var masterBadge in masterList)
            {
                // Check if the exact title exists in the user's unlocked badges list.
                // Alternatively, we could match by ID, but QuestService saves it by Title currently (e.g. "🎯 Deadeye")
                bool hasBadge = userBadges.Any(b => b.Contains(masterBadge.Title));

                masterBadge.IsUnlocked = hasBadge;
                if (hasBadge) unlockedCount++;

                Achievements.Add(masterBadge);
            }

            TotalAchievements = masterList.Count;
            UnlockedAchievements = unlockedCount;
            ProgressPercentage = TotalAchievements > 0 ? (double)UnlockedAchievements / TotalAchievements : 0;
        }

        [RelayCommand]
        private async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        private async Task SelectAchievement(Achievement selected)
        {
            if (selected == null) return;

            if (selected.IsUnlocked)
            {
                await Application.Current.MainPage.DisplayAlert(
                    selected.Title, 
                    $"🏆 Congratulations! You have unlocked this achievement.\n\nDescription: {selected.Description}", 
                    "Awesome!");
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert(
                    $"🔒 {selected.Title} (Locked)", 
                    $"How to unlock:\n{selected.Condition}", 
                    "Got it");
            }
        }
    }
}
