using AcadsJulie.Models;
using AcadsJulie.Services;

namespace AcadsJulie.Views;

public partial class DailyChallengePage : ContentPage
{
    private readonly DailyChallengeService _challengeService;
    private System.Timers.Timer? _countdownTimer;

    public DailyChallengePage()
    {
        InitializeComponent();
        _challengeService = App.ChallengeService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadChallenge();
        StartCountdown();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _countdownTimer?.Stop();
        _countdownTimer?.Dispose();
        _countdownTimer = null;
    }

    private void LoadChallenge()
    {
        var challenge = _challengeService.GetTodayChallenge();

        ChallengeTitleLabel.Text = challenge.Title;
        ChallengeDescLabel.Text = challenge.Description;
        RewardXPLabel.Text = $"+{challenge.RewardXP} XP";
        BadgeIconLabel.Text = challenge.BadgeReward ?? "🏆";
        ProgressLabel.Text = $"{challenge.CurrentScore} / {challenge.TargetScore}";
        ChallengeProgress.Progress = challenge.Progress;

        var icon = challenge.Category switch
        {
            "Memory" => "🧠",
            "Focus"  => "🎯",
            "Logic"  => "🧩",
            "Speed"  => "⚡",
            _        => "🌟"
        };
        ChallengeIconLabel.Text = icon;

        if (challenge.IsCompleted)
        {
            StatusFrame.BackgroundColor = GetColor("MobileSuccessMuted", "#DDF8EC");
            StatusIcon.Text = "✅";
            StatusLabel.Text = "Challenge Completed! Awesome work! 🎉";
            StatusLabel.TextColor = GetColor("MobileSuccess", "#16A34A");
        }
        else
        {
            StatusFrame.BackgroundColor = GetColor("MobileSurfaceRaised", "#EEF7F3");
            StatusIcon.Text = "⏱️";
        }

        LoadHistory();
    }

    private void LoadHistory()
    {
        HistoryStack.Children.Clear();
        var history = _challengeService.GetHistory();

        if (history.Count == 0)
        {
            HistoryStack.Children.Add(new Frame
            {
                Padding = new Thickness(20, 16),
                CornerRadius = 20,
                BackgroundColor = GetColor("MobileSurface", "#F7FBF8"),
                BorderColor = GetColor("MobileStroke", "#C7D7D2"),
                Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.08f, Radius = 12 },
                Content = new Label
                {
                    Text = "Complete today's challenge to start your history! 🔥",
                    TextColor = GetColor("TextSecondary", "#59686A"),
                    FontSize = 14,
                    HorizontalOptions = LayoutOptions.Center
                }
            });
            return;
        }

        foreach (var ch in history)
        {
            var card = CreateHistoryCard(ch);
            HistoryStack.Children.Add(card);
        }
    }

    private View CreateHistoryCard(DailyChallenge ch)
    {
        var frame = new Frame
        {
            Padding = new Thickness(16),
            CornerRadius = 20,
            BackgroundColor = GetColor("MobileSurface", "#F7FBF8"),
            BorderColor = GetColor("MobileStroke", "#C7D7D2"),
            Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.08f, Radius = 12 }
        };
        var grid = new Grid
        {
            ColumnDefinitions =
            [
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            ],
            ColumnSpacing = 12
        };

        var iconBox = new Frame
        {
            WidthRequest = 44, HeightRequest = 44, CornerRadius = 12,
            BackgroundColor = ch.IsCompleted ? GetColor("MobileSuccessMuted", "#DDF8EC") : GetColor("MobileAmberMuted", "#FFF0C2"),
            Padding = 0,
            Content = new Label
            {
                Text = ch.IsCompleted ? "✅" : "⬛",
                FontSize = 22,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };

        var info = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
        info.Children.Add(new Label { Text = ch.Title, FontSize = 14, FontAttributes = FontAttributes.Bold, TextColor = GetColor("TextPrimary", "#121A1C") });
        info.Children.Add(new Label { Text = ch.Date.ToString("MMM dd, yyyy"), FontSize = 12, TextColor = GetColor("TextMuted", "#7E8D90") });

        var xpLabel = new Label
        {
            Text = ch.IsCompleted ? $"+{ch.RewardXP} XP" : "—",
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = ch.IsCompleted ? GetColor("MobileSuccess", "#16A34A") : GetColor("TextMuted", "#7E8D90"),
            VerticalOptions = LayoutOptions.Center
        };

        grid.Add(iconBox, 0); grid.Add(info, 1); grid.Add(xpLabel, 2);
        frame.Content = grid;
        return frame;
    }

    private void StartCountdown()
    {
        _countdownTimer?.Stop();
        _countdownTimer?.Dispose();
        _countdownTimer = new System.Timers.Timer(1000);
        _countdownTimer.Elapsed += (s, e) =>
        {
            var challenge = _challengeService.GetTodayChallenge();
            if (!challenge.IsCompleted)
            {
                var remaining = _challengeService.TimeUntilMidnight;
                Dispatcher.Dispatch(() =>
                {
                    StatusLabel.Text = $"Time remaining: {remaining:hh\\:mm\\:ss}";
                });
            }
        };
        _countdownTimer.Start();
    }

    private static Color GetColor(string key, string fallback)
    {
        return Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : Color.FromArgb(fallback);
    }
}
