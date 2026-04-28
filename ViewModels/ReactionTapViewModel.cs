using AcadsJulie.Models;
using AcadsJulie.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;

namespace AcadsJulie.ViewModels;

[QueryProperty(nameof(Mode), "mode")]
public partial class ReactionTapViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;

    [ObservableProperty] private string _instructionText = "Tap anywhere when GREEN";
    [ObservableProperty] private Color _boxColor = Colors.Red;
    [ObservableProperty] private string _mode = "Normal";
    [ObservableProperty] private bool _isGameOver;
    [ObservableProperty] private int _score;
    [ObservableProperty] private int _timeLeft = 30; // 30 second game
    [ObservableProperty] private string _hudText = "Time: 30s | Score: 0";

    private IDispatcherTimer? _gameTimer;
    private IDispatcherTimer? _waitTimer;
    private Stopwatch _reactionTimer = new();
    private bool _canTap = false;
    private int _attempts = 0;
    private int _mistakes = 0;

    public ReactionTapViewModel()
    {
        _databaseService = App.DatabaseService;
    }

    public void InitGame()
    {
        Score = 0;
        TimeLeft = 30;
        IsGameOver = false;
        _attempts = 0;
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
        if (IsGameOver) return;
        _canTap = false;
        BoxColor = Colors.Red;
        InstructionText = "Wait for Green...";

        _waitTimer?.Stop();
        _waitTimer = Application.Current!.Dispatcher.CreateTimer();
        _waitTimer.Interval = TimeSpan.FromMilliseconds(new Random().Next(1000, 3000));
        _waitTimer.Tick += (s, e) =>
        {
            _waitTimer.Stop();
            if (!IsGameOver)
            {
                BoxColor = Colors.Green;
                InstructionText = "TAP NOW!";
                _canTap = true;
                _reactionTimer.Restart();
            }
        };
        _waitTimer.Start();
    }

    [RelayCommand]
    public void TapArea()
    {
        if (IsGameOver) return;
        _attempts++;

        if (_canTap)
        {
            if (App.ProfileService.GetProfile().HapticsEnabled)
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);

            _reactionTimer.Stop();
            long ms = _reactionTimer.ElapsedMilliseconds;
            int points = ms < 300 ? 50 : ms < 500 ? 30 : 10;
            Score += points;
            UpdateHud();
            StartNextRound();
        }
        else
        {
            if (App.ProfileService.GetProfile().HapticsEnabled)
                HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);

            if (Mode == "Endless") _mistakes++;

            // False start penalty
            Score = Math.Max(0, Score - 10);
            UpdateHud();
            InstructionText = "Too early! -10 pts";

            if (Mode == "Endless" && _mistakes >= 3)
            {
                EndGame();
            }
            else
            {
                StartNextRound(); // Restart wait
            }
        }
    }

    private void UpdateHud() => HudText = $"Time: {TimeLeft}s | Score: {Score}";

    private async void EndGame()
    {
        Cleanup();
        IsGameOver = true;
        InstructionText = $"Game Over! Score: {Score}";
        BoxColor = Colors.LightGray;

        await _databaseService.SaveGameResultAsync(new GameResult
        {
            GameName = "ReactionTap",
            Category = "Speed",
            Score = Score,
            Accuracy = 100, // Not applicable
            DurationSeconds = 30,
            PlayedAt = DateTime.Now,
            Difficulty = "Adaptive"
        });
    }

    public async Task SaveProgressEarlyAsync()
    {
        if (!IsGameOver && _attempts > 0)
        {
            await _databaseService.SaveGameResultAsync(new GameResult
            {
                GameName = "ReactionTap",
                Category = "Speed",
                Score = Score,
                Accuracy = 100,
                DurationSeconds = 30 - TimeLeft,
                PlayedAt = DateTime.Now,
                Difficulty = "Adaptive"
            });
        }
    }

    public void Cleanup()
    {
        _gameTimer?.Stop();
        _waitTimer?.Stop();
        _reactionTimer.Stop();
    }

    [RelayCommand]
    public void RestartGame() => InitGame();
}

