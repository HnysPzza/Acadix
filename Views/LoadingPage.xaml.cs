namespace AcadsJulie.Views;

public partial class LoadingPage : ContentPage
{
    public LoadingPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await StartLoadingAnimationAsync();
    }

    private async Task StartLoadingAnimationAsync()
    {
        var loadingSteps = new[]
        {
            "Preparing your progress",
            "Loading your quests",
            "Syncing leaderboard",
            "Opening Acadix"
        };

        _ = PulseLogoAsync();

        for (var i = 0; i < loadingSteps.Length; i++)
        {
            LoadingText.Text = loadingSteps[i];

            var progress = (double)(i + 1) / loadingSteps.Length;
            await LoadingProgress.ProgressTo(progress, 520, Easing.CubicInOut);
            PercentLabel.Text = $"{Math.Round(progress * 100)}%";
            await Task.Delay(180);
        }

        await Task.Delay(180);
        Application.Current!.MainPage = new AppShell();
    }

    private async Task PulseLogoAsync()
    {
        while (Application.Current?.MainPage is LoadingPage)
        {
            await Task.WhenAll(
                LogoMark.ScaleTo(1.05, 520, Easing.CubicInOut),
                OuterGlow.FadeTo(0.72, 520, Easing.CubicInOut));

            await Task.WhenAll(
                LogoMark.ScaleTo(1, 520, Easing.CubicInOut),
                OuterGlow.FadeTo(1, 520, Easing.CubicInOut));
        }
    }
}
