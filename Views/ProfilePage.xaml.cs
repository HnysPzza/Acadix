using System.IO;
using AcadsJulie.Services;

namespace AcadsJulie.Views;

public partial class ProfilePage : ContentPage
{
    // Resolved on each use rather than cached in the constructor: App.ResetServices() swaps
    // these singletons out (on account switch or progress reset), and a cached field would keep
    // rendering the previous instance's data.
    private static ProfileService _profileService => App.ProfileService;
    private static ProgressService _progressService => App.ProgressService;
    private static GoalService _goalService => App.GoalService;

    public ProfilePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadProfile();
    }

    private void LoadProfile()
    {
        var profile = _profileService.GetProfile();
        var sessions = _progressService.GetSessions();

        AvatarLabel.Text = profile.AvatarInitials;

        if (!string.IsNullOrEmpty(profile.ProfileImagePath) && File.Exists(profile.ProfileImagePath))
        {
            AvatarImage.Source = ImageSource.FromFile(profile.ProfileImagePath);
            AvatarImage.IsVisible = true;
            AvatarLabel.IsVisible = false;
        }
        else
        {
            AvatarImage.IsVisible = false;
            AvatarLabel.IsVisible = true;
        }

        NameLabel.Text = profile.Name;
        SettingsNameLabel.Text = profile.Name;
        LevelLabel.Text = $"Level {profile.Level}";
        XPLabel.Text = $"{profile.XP} XP";
        var xpProgress = Math.Clamp(profile.XPProgress, 0, 1);
        var xpRemaining = Math.Max(0, profile.XPToNextLevel - profile.XP);
        XPProgress.Progress = xpProgress;
        XPProgressPercentLabel.Text = $"{Math.Round(xpProgress * 100)}%";
        XPProgressLabel.Text = $"{profile.XP} / {profile.XPToNextLevel} XP";
        XPRemainingLabel.Text = $"{xpRemaining} XP left";

        GamesPlayedLabel.Text = sessions.Count.ToString();
        StreakLabel.Text = profile.StreakDays.ToString();
        BadgesLabel.Text = profile.Badges.Count.ToString();

        DiffPrefLabel.Text = profile.DifficultyPreference;
        NotifSwitch.IsToggled = profile.NotificationsEnabled;
        HapticsSwitch.IsToggled = profile.HapticsEnabled;

        var activeGoals = _goalService.GetActiveGoals();
        var activeHabits = _goalService.GetActiveHabits();
        var bestStreak = activeHabits.Count > 0 ? activeHabits.Max(h => h.CurrentStreak) : 0;

        ActiveGoalsLabel.Text = $"{activeGoals.Count} Active Goal{(activeGoals.Count != 1 ? "s" : "")}";
        ActiveHabitsLabel.Text = $"{activeHabits.Count} Active Habit{(activeHabits.Count != 1 ? "s" : "")}";
        HabitStreakLabel.Text = $"Best streak: {bestStreak} day{(bestStreak != 1 ? "s" : "")}";

        LoadBadges(profile.Badges);
    }

    private void LoadBadges(List<string> badges)
    {
        var masterListSize = _progressService.GetMasterAchievements().Count;
        UnlockSummaryLabel.Text = $"{badges.Count} / {masterListSize} Unlocked";
    }

    private async void OnViewAchievementsTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("AchievementsPage");
    }

    private async void OnEditProfileTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("EditProfilePage");
    }

    private async void OnChangeDifficultyTapped(object? sender, EventArgs e)
    {
        var options = new[] { "Easy", "Medium", "Hard", "Adaptive" };
        var result = await DisplayActionSheetAsync("Default Difficulty", "Cancel", null, options);
        if (result != null && result != "Cancel")
        {
            var profile = _profileService.GetProfile();
            profile.DifficultyPreference = result;
            _profileService.SaveProfile(profile);
            DiffPrefLabel.Text = result;
        }
    }

    private void OnNotifToggled(object? sender, ToggledEventArgs e)
    {
        var profile = _profileService.GetProfile();
        profile.NotificationsEnabled = e.Value;
        _profileService.SaveProfile(profile);
    }

    private void OnHapticsToggled(object? sender, ToggledEventArgs e)
    {
        var profile = _profileService.GetProfile();
        profile.HapticsEnabled = e.Value;
        _profileService.SaveProfile(profile);
    }

    private async Task SaveProfileImage(FileResult photo)
    {
        string localFilePath = Path.Combine(FileSystem.AppDataDirectory, photo.FileName);

        using Stream sourceStream = await photo.OpenReadAsync();
        using FileStream localFileStream = File.OpenWrite(localFilePath);
        await sourceStream.CopyToAsync(localFileStream);

        var profile = _profileService.GetProfile();
        profile.ProfileImagePath = localFilePath;
        _profileService.SaveProfile(profile);

        LoadProfile();
    }

    private async void OnResetProgressClicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync(
            "⚠️ Reset Progress",
            "Are you sure? This will clear all your scores, streaks, and achievements. This cannot be undone!",
            "Yes, Reset",
            "Cancel");

        if (confirm)
        {
            // Only this account's data. Preferences.Clear() used to wipe every account on the
            // device plus the auth/sync bookkeeping.
            UserScope.ClearCurrentUserData();
            await DisplayAlertAsync("Done", "Your progress has been reset. Start fresh! 💪", "Let's Go!");
            App.ResetServices();
            App.QueueLeaderboardSync();
            LoadProfile();
        }
    }

    private async void OnDeleteAccountClicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync(
            "Delete account?",
            "This permanently deletes your Acadix account, your scores and your saved tasks. " +
            "This cannot be undone.",
            "Delete",
            "Cancel");

        if (!confirm)
            return;

        // Second confirmation: this action is irreversible, so make it deliberate.
        var finalConfirm = await DisplayAlertAsync(
            "Are you absolutely sure?",
            "Last chance — your account and everything in it will be gone for good.",
            "Yes, delete forever",
            "Keep my account");

        if (!finalConfirm)
            return;

        try
        {
            // Remove the public leaderboard entry while the token is still valid. Non-fatal:
            // if it fails, the account deletion below must still go ahead.
            try
            {
                await App.RankingService.DeleteCurrentUserEntryAsync();
            }
            catch
            {
                // Leaves an orphaned leaderboard row; acceptable versus blocking deletion.
            }

            // Local data first — once the account is gone we lose the UID that scopes it.
            UserScope.ClearCurrentUserData();

            await App.AuthService.DeleteAccountAsync();

            await DisplayAlertAsync("Account deleted", "Your account and data have been removed.", "OK");
            App.NavigateToLogin();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Couldn't delete account", AuthUiMessageMapper.ToUserMessage(ex), "OK");
        }
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync("Log Out", "Are you sure you want to log out of your session?", "Log Out", "Cancel");
        if (confirm)
        {
            await App.AuthService.LogoutAsync();
            App.NavigateToLogin();
        }
    }
}
