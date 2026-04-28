using AcadsJulie.Models;
using AcadsJulie.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcadsJulie.ViewModels;

[QueryProperty(nameof(Mode), "mode")]
public partial class ColorTapViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;

    [ObservableProperty] private string _mode = "Normal";
    [ObservableProperty] private string _colorWord = "";
    [ObservableProperty] private Color _textColor = Colors.Black;
    [ObservableProperty] private ObservableCollection<string> _options = new();
    [ObservableProperty] private bool _isGameOver;
    [ObservableProperty] private int _score;
    [ObservableProperty] private int _timeLeft = 45;
    [ObservableProperty] private string _hudText = "Time: 45s | Score: 0";

    private IDispatcherTimer? _gameTimer;
    private string _correctAnswer = "";
    private int _attempts;
    private int _correctCount;
    private int _mistakes;

    private readonly string[] _colors = { "Red", "Blue", "Green", "Yellow", "Purple", "Orange" };
    private readonly Dictionary<string, Color> _colorMap = new()
    {
        { "Red", Colors.Red },
        { "Blue", Colors.Blue },
        { "Green", Colors.Green },
        { "Yellow", Colors.Gold },
        { "Purple", Colors.Purple },
        { "Orange", Colors.Orange }
    };

    public ColorTapViewModel()
    {
        _databaseService = App.DatabaseService;
    }

    public void InitGame()
    {
        Score = 0;
        TimeLeft = 45;
        IsGameOver = false;
        _attempts = 0;
        _correctCount = 0;
        _mistakes = 0;
        UpdateHud();

        _gameTimer = Application.Current!.Dispatcher.CreateTimer();
        _gameTimer.Interval = TimeSpan.FromSeconds(1);
        _gameTimer.Tick += (s, e) =>
        {
            if (Mode == "Normal")
            {
                TimeLeft--;
                UpdateHud();
                if (TimeLeft <= 0) EndGame();
            }
        };
        _gameTimer.Start();

        StartNextRound();
    }

    private void StartNextRound()
    {
        var rand = new Random();
        
        string word = _colors[rand.Next(_colors.Length)];
        string actualColorName = _colors[rand.Next(_colors.Length)];
        
        // Sometimes match them to trick the user
        if (rand.Next(10) > 7) actualColorName = word;

        ColorWord = word;
        TextColor = _colorMap[actualColorName];
        _correctAnswer = actualColorName;

        var opt = _colors.OrderBy(x => rand.Next()).Take(4).ToList();
        if (!opt.Contains(_correctAnswer))
        {
            opt[rand.Next(4)] = _correctAnswer;
        }
        
        Options = new ObservableCollection<string>(opt);
    }

    [RelayCommand]
    public void TapColor(string chosenColor)
    {
        if (IsGameOver) return;
        _attempts++;

        if (chosenColor == _correctAnswer)
        {
            if (App.ProfileService.GetProfile().HapticsEnabled)
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);

            Score += 30;
            _correctCount++;
        }
        else
        {
            if (App.ProfileService.GetProfile().HapticsEnabled)
                HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);

            if (Mode == "Endless") _mistakes++;
            Score = Math.Max(0, Score - 15);
        }

        UpdateHud();
        
        if (Mode == "Endless" && _mistakes >= 3)
        {
            EndGame();
        }
        else
        {
            StartNextRound();
        }
    }

    private void UpdateHud() => HudText = $"Time: {TimeLeft}s | Score: {Score}";

    private async void EndGame()
    {
        Cleanup();
        IsGameOver = true;
        ColorWord = "Game Over!";
        TextColor = Colors.Gray;

        double acc = _attempts == 0 ? 0 : (double)_correctCount / _attempts * 100;

        await _databaseService.SaveGameResultAsync(new GameResult
        {
            GameName = "ColorTap",
            Category = "Focus",
            Score = Score,
            Accuracy = Math.Round(acc, 1),
            DurationSeconds = 45,
            PlayedAt = DateTime.Now,
            Difficulty = "Adaptive"
        });
    }

    public async Task SaveProgressEarlyAsync()
    {
        if (!IsGameOver && _attempts > 0)
        {
            double acc = _attempts == 0 ? 0 : (double)_correctCount / _attempts * 100;
            await _databaseService.SaveGameResultAsync(new GameResult
            {
                GameName = "ColorTap",
                Category = "Focus",
                Score = Score,
                Accuracy = Math.Round(acc, 1),
                DurationSeconds = 45 - TimeLeft,
                PlayedAt = DateTime.Now,
                Difficulty = "Adaptive"
            });
        }
    }

    public void Cleanup() => _gameTimer?.Stop();

    [RelayCommand]
    public void RestartGame() => InitGame();
}

