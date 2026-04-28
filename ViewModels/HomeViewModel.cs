using AcadsJulie.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AcadsJulie.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly ProfileService _profileService;
    private readonly ProgressService _progressService;
    private readonly DailyChallengeService _challengeService;

    [ObservableProperty] private string _greeting = "Good Morning! ☀️";
    [ObservableProperty] private string _userName = "Brain Trainer";
    [ObservableProperty] private bool _hasUnreadNotifications;
    [ObservableProperty] private string _brainScore = "0";
    [ObservableProperty] private string _streakDays = "0";
    [ObservableProperty] private string _memoryScore = "Best: 0";
    [ObservableProperty] private string _focusScore = "Best: 0";
    [ObservableProperty] private string _logicScore = "Best: 0";
    [ObservableProperty] private string _speedScore = "Best: 0";
    [ObservableProperty] private string _knowledgeScore = "Best: 0";
    [ObservableProperty] private string _challengeTitle = "Loading...";

    public HomeViewModel()
    {
        _profileService = App.ProfileService;
        _progressService = App.ProgressService;
        _challengeService = App.ChallengeService;
    }

    public void Refresh()
    {
        var profile = _profileService.GetProfile();
        var challenge = _challengeService.GetTodayChallenge();

        var hour = DateTime.Now.Hour;
        Greeting = hour < 12 ? "Good Morning! ☀️" :
                   hour < 17 ? "Good Afternoon! 🌤️" : "Good Evening! 🌙";

        UserName = profile.Name;
        BrainScore = profile.BrainScore.ToString();
        StreakDays = profile.StreakDays.ToString();

        MemoryScore = $"Best: {profile.MemoryScore}";
        FocusScore = $"Best: {profile.FocusScore}";
        LogicScore = $"Best: {profile.LogicScore}";
        SpeedScore = $"Best: {profile.SpeedScore}";
        KnowledgeScore = $"Best: {profile.TriviaBestScore}";

        ChallengeTitle = challenge.IsCompleted ? $"✅ {challenge.Title}" : challenge.Title;

        CheckLevelUp(profile);
    }

    private void CheckLevelUp(Models.UserProfile profile)
    {
        int lastSeenLevel = Preferences.Get("LastSeenLevel", 1);
        if (profile.Level > lastSeenLevel)
        {
            HasUnreadNotifications = true;
        }
        else
        {
            HasUnreadNotifications = false;
        }
    }

    [RelayCommand]
    public async Task OpenNotifications()
    {
        if (Application.Current?.MainPage == null) return;
        
        await Application.Current.MainPage.Navigation.PushModalAsync(new Views.NotificationsModalPage());
        
        // Modal will handle marking as read, but we can clear the badge on the Home block now.
        HasUnreadNotifications = false;
    }

    [RelayCommand]
    async Task NavigateToGame(string route)
    {
        if (Application.Current?.MainPage == null) return;

        await Application.Current.MainPage.Navigation.PushModalAsync(new Views.GameModeSelectionPage(route));

        // Logic moved to GameModeSelectionPage.cs
    }

    [RelayCommand]
    async Task NavigateToChallenge()
    {
        await Shell.Current.GoToAsync("DailyChallengePage");
    }

    [RelayCommand]
    async Task NavigateToTrivia()
    {
        if (Application.Current?.MainPage == null) return;
        await Application.Current.MainPage.Navigation.PushAsync(new Views.TriviaSetupPage());
    }
}
