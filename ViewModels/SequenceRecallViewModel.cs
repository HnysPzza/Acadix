using AcadsJulie.Models;
using AcadsJulie.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AcadsJulie.ViewModels;

[QueryProperty(nameof(Mode), "mode")]
public partial class SequenceRecallViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;

    [ObservableProperty] private string _displaySequence = "";
    [ObservableProperty] private string _userScript = "";
    [ObservableProperty] private string _mode = "Normal";
    [ObservableProperty] private bool _isShowingSequence = true;
    [ObservableProperty] private bool _isGameOver;
    [ObservableProperty] private int _score;
    [ObservableProperty] private int _timeLeft = 45;
    [ObservableProperty] private int _level = 3; // start length
    [ObservableProperty] private string _hudText = "Time: 45s | Score: 0";

    private IDispatcherTimer? _gameTimer;
    private string _currentSequence = "";
    private int _attempts;
    private int _correctCount;
    private int _mistakes;

    public SequenceRecallViewModel()
    {
        _databaseService = App.DatabaseService;
    }

    public void InitGame()
    {
        Score = 0;
        TimeLeft = 45;
        Level = 3;
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
        _currentSequence = "";
        for (int i = 0; i < Level; i++)
        {
            _currentSequence += rand.Next(0, 10).ToString();
        }

        UserScript = "";
        DisplaySequence = string.Join(" ", _currentSequence.ToCharArray());
        IsShowingSequence = true;

        IDispatcherTimer waitTimer = Application.Current!.Dispatcher.CreateTimer();
        waitTimer.Interval = TimeSpan.FromSeconds(2.5);
        waitTimer.Tick += (s, e) =>
        {
            waitTimer.Stop();
            if (!IsGameOver)
            {
                DisplaySequence = "???";
                IsShowingSequence = false;
            }
        };
        waitTimer.Start();
    }

    [RelayCommand]
    public void TypeNumber(string num)
    {
        if (IsGameOver || IsShowingSequence) return;

        UserScript += num;

        if (App.ProfileService.GetProfile().HapticsEnabled)
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);

        // Check if full answer entered
        if (UserScript.Length == _currentSequence.Length)
        {
            _attempts++;
            if (UserScript == _currentSequence)
            {
                Score += 20 * Level;
                _correctCount++;
                Level++; // increase length
            }
            else
            {
                if (App.ProfileService.GetProfile().HapticsEnabled)
                    HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);

                if (Mode == "Endless") _mistakes++;

                Score = Math.Max(0, Score - 10);
                Level = Math.Max(3, Level - 1); // decrease length
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
    }

    [RelayCommand]
    public void DeleteNumber()
    {
        if (IsGameOver || IsShowingSequence || string.IsNullOrEmpty(UserScript)) return;
        UserScript = UserScript[..^1];
        
        if (App.ProfileService.GetProfile().HapticsEnabled)
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
    }

    private void UpdateHud() => HudText = $"Time: {TimeLeft}s | Score: {Score} | Lvl: {Level - 2}";

    private async void EndGame()
    {
        Cleanup();
        IsGameOver = true;
        DisplaySequence = "Game Over!";
        
        double acc = _attempts == 0 ? 0 : (double)_correctCount / _attempts * 100;

        await _databaseService.SaveGameResultAsync(new GameResult
        {
            GameName = "SequenceRecall",
            Category = "Memory",
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
                GameName = "SequenceRecall",
                Category = "Memory",
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

