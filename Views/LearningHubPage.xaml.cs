using AcadsJulie.Services;

namespace AcadsJulie.Views;

public partial class LearningHubPage : ContentPage
{
    private readonly CareerRecommendationService _recommendationService;
    private readonly TaskService _taskService;
    private readonly ProfileService _profileService;

    public LearningHubPage()
    {
        InitializeComponent();
        _recommendationService = App.CareerRecommendationService;
        _taskService = App.TaskService;
        _profileService = App.ProfileService;

        // Add tap gesture recognizers for card animations
        SetupCardAnimations();
    }

    private void SetupCardAnimations()
    {
        // Add press animations to cards
        AddCardPressAnimation(PlannerCard, PlannerIcon);
        AddCardPressAnimation(GuidanceCard, GuidanceIcon);
        AddCardPressAnimation(LibraryCard, LibraryIcon);
        AddCardPressAnimation(CoursesCard, CoursesIcon);
    }

    private void AddCardPressAnimation(Border card, Label icon)
    {
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += async (s, e) =>
        {
            // Scale down animation
            await card.ScaleTo(0.95, 100, Easing.CubicOut);
            await icon.ScaleTo(1.2, 100, Easing.CubicOut);

            // Scale back up
            await card.ScaleTo(1, 150, Easing.SpringOut);
            await icon.ScaleTo(1, 150, Easing.SpringOut);
        };
        card.GestureRecognizers.Add(tapGesture);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshContent();
    }

    private void RefreshContent()
    {
        var profile = _profileService.GetProfile();
        var report = _recommendationService.GenerateReport(profile.EducationStage);
        var top = report.Recommendations.FirstOrDefault();
        var tasks = _taskService.GetTasks();
        var completedTasks = tasks.Count(t => t.IsCompleted);
        var totalTasks = tasks.Count;

        TopRecommendationLabel.Text = top == null
            ? "Your current performance is being used to suggest directions."
            : $"{top.Title} • {Math.Round(top.MatchPercent)}% match";

        if (!report.HasEnoughData)
        {
            var remaining = Math.Max(0, report.MinimumSessionsRequired - report.SessionsAnalyzed);
            TopRecommendationLabel.Text = $"Complete {remaining} more learning session{(remaining == 1 ? "" : "s")} to unlock guidance.";
        }

        // Update stats
        DailyStreakLabel.Text = $"🔥 {profile.StreakDays}";
        TasksDoneLabel.Text = $"✅ {completedTasks}/{totalTasks}";
        MinutesLearnedLabel.Text = $"⏱️ {completedTasks * 15}"; // Estimated minutes
    }

    private async void OnOpenPlannerClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("PlannerPage");
    }

    private async void OnOpenGuidanceClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("RecommendationsPage");
    }

    private async void OnOpenAssistantClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("StudyAssistantPage");
    }

    private async void OnOpenCoursesClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("CoursesPage");
    }

    private async void OnOpenLibraryClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("ContentLibraryPage");
    }

    private void OnRefreshClicked(object? sender, EventArgs e)
    {
        RefreshContent();
    }
}
