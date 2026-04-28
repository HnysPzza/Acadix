using AcadsJulie.Models;
using AcadsJulie.Services;

namespace AcadsJulie.Views;

public partial class RecommendationsPage : ContentPage
{
    private readonly CareerRecommendationService _recommendationService;
    private readonly ProfileService _profileService;

    public RecommendationsPage()
    {
        InitializeComponent();
        _recommendationService = App.CareerRecommendationService;
        _profileService = App.ProfileService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var stage = _profileService.GetProfile().EducationStage;
        UpdateStageSelection(stage);
        LoadReport(stage);
    }

    private void LoadReport(string stage)
    {
        var report = _recommendationService.GenerateReport(stage);
        RecommendationsHeader.Text = report.HasEnoughData ? "Recommendations" : "Recommendations locked";

        AptitudesCollection.ItemsSource = report.Aptitudes.Select(a => new AptitudeDisplay
        {
            Domain = a.Domain,
            DisplayScore = a.DisplayScore,
            Progress = a.Score / 100.0
        }).ToList();

        RecommendationsCollection.ItemsSource = report.Recommendations;
    }

    private void OnJuniorHighTapped(object? sender, EventArgs e)
    {
        SelectStage("JuniorHigh");
    }

    private void OnSeniorHighTapped(object? sender, EventArgs e)
    {
        SelectStage("SeniorHigh");
    }

    private void OnCollegeTapped(object? sender, EventArgs e)
    {
        SelectStage("College");
    }

    private void SelectStage(string stage)
    {
        var profile = _profileService.GetProfile();
        profile.EducationStage = stage;
        _profileService.SaveProfile(profile);
        UpdateStageSelection(stage);
        LoadReport(stage);
    }

    private void UpdateStageSelection(string stage)
    {
        var selectedFill = GetColor("MobilePrimaryMuted", "#D8F3EC");
        var defaultFill = GetColor("MobileSurfaceRaised", "#EEF7F3");
        var selectedStroke = GetColor("MobilePrimary", "#0F766E");
        var defaultStroke = GetColor("MobileStroke", "#C7D7D2");

        JuniorHighBorder.BackgroundColor = stage == "JuniorHigh" ? selectedFill : defaultFill;
        JuniorHighBorder.Stroke = stage == "JuniorHigh" ? selectedStroke : defaultStroke;

        SeniorHighBorder.BackgroundColor = stage == "SeniorHigh" ? selectedFill : defaultFill;
        SeniorHighBorder.Stroke = stage == "SeniorHigh" ? selectedStroke : defaultStroke;

        CollegeBorder.BackgroundColor = stage == "College" ? selectedFill : defaultFill;
        CollegeBorder.Stroke = stage == "College" ? selectedStroke : defaultStroke;
    }

    private static Color GetColor(string key, string fallback)
    {
        return Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : Color.FromArgb(fallback);
    }

    private async void OnOpenAssistantClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("StudyAssistantPage");
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private class AptitudeDisplay
    {
        public string Domain { get; set; } = string.Empty;
        public string DisplayScore { get; set; } = string.Empty;
        public double Progress { get; set; }
    }
}
