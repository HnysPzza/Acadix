using AcadsJulie.Models;
using AcadsJulie.Services;

namespace AcadsJulie.Views;

public partial class RankingPage : ContentPage
{
    public RankingPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadLeaderboardAsync();
    }

    private async void OnRetryClicked(object? sender, EventArgs e)
    {
        await LoadLeaderboardAsync();
    }

    private async Task LoadLeaderboardAsync()
    {
        LoadingOverlay.IsVisible = true;
        StatusLabel.IsVisible = false;

        try
        {
            await App.RankingService.SyncCurrentUserAsync();
            var entries = await App.RankingService.GetTopGlobalAsync();
            LeaderboardCollection.ItemsSource = entries;
            UpdateCurrentUser(entries);

            if (entries.Count == 0)
                ShowStatus("No rankings yet. Play a game to publish your Brain Score.");
        }
        catch (Exception ex)
        {
            LeaderboardCollection.ItemsSource = Array.Empty<RankingEntry>();
            UpdateCurrentUser([]);
            ShowStatus(AuthUiMessageMapper.ToUserMessage(ex));
        }
        finally
        {
            LoadingOverlay.IsVisible = false;
        }
    }

    private void UpdateCurrentUser(IReadOnlyCollection<RankingEntry> entries)
    {
        var profile = App.ProfileService.GetProfile();
        var current = entries.FirstOrDefault(e => e.IsCurrentUser);

        CurrentRankLabel.Text = current == null ? "--" : $"#{current.Rank}";
        CurrentNameLabel.Text = profile.Name;
        CurrentScoreLabel.Text = profile.BrainScore.ToString();
        CurrentSubtitleLabel.Text = current == null
            ? "Not in the top 50 yet"
            : $"Level {current.Level} · {current.GamesPlayed} games played";
    }

    private void ShowStatus(string message)
    {
        StatusLabel.Text = message;
        StatusLabel.IsVisible = true;
    }
}
