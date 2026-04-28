using AcadsJulie.Models;
using AcadsJulie.Services;

namespace AcadsJulie.Views;

[QueryProperty(nameof(Category), "category")]
public partial class SkillCategoryPage : ContentPage
{
    private readonly ProgressService _progressService;
    private string _selectedDifficulty = "Easy";
    private string _category = "Memory";

    public string Category
    {
        get => _category;
        set
        {
            _category = value;
            SetupCategory();
        }
    }

    public SkillCategoryPage()
    {
        InitializeComponent();
        _progressService = App.ProgressService;
    }

    private void SetupCategory()
    {
        var color = GetCategoryColor();
        var icon = _category switch
        {
            "Memory" => "🧠",
            "Focus"  => "🎯",
            "Logic"  => "🧩",
            "Speed"  => "⚡",
            _        => "🎮"
        };

        HeaderBg.Color = color;
        CategoryIconLabel.Text = icon;
        CategoryNameLabel.Text = _category;

        var prog = _progressService.GetCategoryProgress(_category);
        GamesPlayedLabel.Text = $"{prog.GamesPlayed} games played";
        HighScoreLabel.Text = $"Best: {prog.HighScore}";

        LoadGames();
    }

    private void LoadGames()
    {
        GamesStack.Children.Clear();
        var games = _progressService.GetGamesForCategory(_category);

        foreach (var game in games)
        {
            var card = CreateGameCard(game);
            GamesStack.Children.Add(card);
        }
    }

    private View CreateGameCard(GameInfo game)
    {
        var highScore = _progressService.GetHighScore(game.Id);
        var accentColor = GetCategoryColor();

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
            ColumnSpacing = 14
        };

        // Icon box
        var iconBox = new Frame
        {
            WidthRequest = 56,
            HeightRequest = 56,
            CornerRadius = 14,
            BackgroundColor = accentColor.WithAlpha(0.12f),
            Padding = 0,
            Content = new Label { Text = game.Icon, FontSize = 28, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        };

        // Info
        var infoStack = new VerticalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center };
        infoStack.Children.Add(new Label { Text = game.Name, FontSize = 15, FontAttributes = FontAttributes.Bold, TextColor = GetColor("TextPrimary", "#121A1C") });
        infoStack.Children.Add(new Label { Text = game.Description, FontSize = 12, TextColor = GetColor("TextSecondary", "#59686A"), LineBreakMode = LineBreakMode.TailTruncation, MaxLines = 1 });

        var badgeRow = new HorizontalStackLayout { Spacing = 8 };
        var diffBadge = new Frame
        {
            Padding = new Thickness(8, 3),
            CornerRadius = 10,
            BackgroundColor = accentColor.WithAlpha(0.12f),
            Content = new Label { Text = _selectedDifficulty, FontSize = 10, TextColor = accentColor }
        };
        badgeRow.Children.Add(diffBadge);

        if (highScore > 0)
        {
            badgeRow.Children.Add(new Label
            {
                Text = $"🏆 {highScore}",
                FontSize = 11,
                TextColor = GetColor("TextSecondary", "#59686A"),
                VerticalOptions = LayoutOptions.Center
            });
        }
        infoStack.Children.Add(badgeRow);

        // Play / Lock button
        View actionView;
        if (game.IsAvailable)
        {
            var playCmd = new Command(async () => await OnPlayGame(game));
            var playBtn = new Frame
            {
                Padding = new Thickness(14, 10),
                CornerRadius = 14,
                BackgroundColor = accentColor,
                Content = new Label { Text = "Play", TextColor = Colors.White, FontSize = 13, FontAttributes = FontAttributes.Bold }
            };
            playBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = playCmd });
            
            // Allow clicking the hero icon box and the entire game card
            iconBox.GestureRecognizers.Add(new TapGestureRecognizer { Command = playCmd });
            frame.GestureRecognizers.Add(new TapGestureRecognizer { Command = playCmd });

            actionView = playBtn;
        }
        else
        {
            actionView = new Label { Text = "🔒", FontSize = 22, VerticalOptions = LayoutOptions.Center };
        }

        grid.Add(iconBox, 0);
        grid.Add(infoStack, 1);
        grid.Add(actionView, 2);
        frame.Content = grid;
        return frame;
    }

    private async Task OnPlayGame(GameInfo game)
    {
        await Shell.Current.GoToAsync($"Games/{game.Id}?difficulty={_selectedDifficulty}");
    }

    private async void OnBackTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void SetDifficultySelection(string difficulty)
    {
        _selectedDifficulty = difficulty;
        var activeColor = GetCategoryColor();
        var inactiveFill = GetColor("MobileSurfaceRaised", "#EEF7F3");
        EasyBtn.BackgroundColor = difficulty == "Easy" ? activeColor : inactiveFill;
        MediumBtn.BackgroundColor = difficulty == "Medium" ? activeColor : inactiveFill;
        HardBtn.BackgroundColor = difficulty == "Hard" ? activeColor : inactiveFill;
        AdaptiveBtn.BackgroundColor = difficulty == "Adaptive" ? activeColor : inactiveFill;

        var activeLabel = Colors.White;
        var inactiveLabel = GetColor("TextSecondary", "#59686A");

        ((Label)EasyBtn.Content).TextColor = difficulty == "Easy" ? activeLabel : inactiveLabel;
        ((Label)MediumBtn.Content).TextColor = difficulty == "Medium" ? activeLabel : inactiveLabel;
        ((Label)HardBtn.Content).TextColor = difficulty == "Hard" ? activeLabel : inactiveLabel;
        ((Label)AdaptiveBtn.Content).TextColor = difficulty == "Adaptive" ? activeLabel : inactiveLabel;

        LoadGames();
    }

    private void OnEasyTapped(object? sender, EventArgs e) => SetDifficultySelection("Easy");
    private void OnMediumTapped(object? sender, EventArgs e) => SetDifficultySelection("Medium");
    private void OnHardTapped(object? sender, EventArgs e) => SetDifficultySelection("Hard");
    private void OnAdaptiveTapped(object? sender, EventArgs e) => SetDifficultySelection("Adaptive");

    private Color GetCategoryColor()
    {
        return _category switch
        {
            "Memory" => GetColor("MobileIndigo", "#4856D6"),
            "Focus"  => GetColor("MobileSuccess", "#16A34A"),
            "Logic"  => GetColor("MobileCoral", "#F9735B"),
            "Speed"  => GetColor("MobileAmber", "#F6A623"),
            _        => GetColor("MobilePrimary", "#0F766E")
        };
    }

    private static Color GetColor(string key, string fallback)
    {
        return Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : Color.FromArgb(fallback);
    }
}
