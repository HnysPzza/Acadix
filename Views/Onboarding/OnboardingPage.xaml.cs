using AcadsJulie.Services;
using AcadsJulie.Views;

namespace AcadsJulie.Views.Onboarding;

public record OnboardingSlide(string Icon, string Title, string Description);

public partial class OnboardingPage : ContentPage
{
    private readonly ProfileService _profileService;
    private int _currentSlide = 0;
    private readonly List<OnboardingSlide> _slides =
    [
        new("🧠", "Train Your Brain", "Sharpen memory, focus, logic\nand speed with fun daily exercises."),
        new("📈", "Track Your Progress", "Watch your brain score grow\nwith beautiful charts and streaks."),
        new("⚡", "Daily Challenges", "New challenges every day keep\nyour brain engaged and rewarded."),
        new("🏆", "Earn Achievements", "Collect badges, level up,\nand become a Brain Champion!")
    ];

    public OnboardingPage()
    {
        InitializeComponent();
        _profileService = App.ProfileService;
        SlidesCarousel.ItemsSource = _slides;
        SlidesCarousel.CurrentItem = _slides[0];
    }

    private void OnSlideChanged(object? sender, CurrentItemChangedEventArgs e)
    {
        _currentSlide = _slides.IndexOf((OnboardingSlide)SlidesCarousel.CurrentItem!);
        UpdateButtons();
    }

    private void UpdateButtons()
    {
        var isLast = _currentSlide == _slides.Count - 1;
        NextBtn.Text = isLast ? "Get Started 🚀" : "Next →";
        NameInputFrame.IsVisible = isLast;
        SkipBtn.IsVisible = !isLast;
    }

    private void OnNextClicked(object? sender, EventArgs e)
    {
        if (_currentSlide < _slides.Count - 1)
        {
            _currentSlide++;
            SlidesCarousel.CurrentItem = _slides[_currentSlide];
            UpdateButtons();
        }
        else
        {
            CompleteOnboarding();
        }
    }

    private void OnSkipClicked(object? sender, EventArgs e)
    {
        CompleteOnboarding();
    }

    private async void CompleteOnboarding()
    {
        var profile = _profileService.GetProfile();
        var name = NameEntry.Text?.Trim();
        if (!string.IsNullOrEmpty(name))
            profile.Name = name;
        profile.OnboardingCompleted = true;
        _profileService.SaveProfile(profile);
        await App.RankingService.EnsureInitialSyncAsync();

        // Navigate to main app
        Application.Current!.Windows[0].Page = new LoadingPage();
    }
}
