using AcadsJulie.Models;
using AcadsJulie.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcadsJulie.ViewModels;

[QueryProperty(nameof(Mode), "mode")]
public partial class NumberGridViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;

    [ObservableProperty] private ObservableCollection<int> _sequence = new();
    [ObservableProperty] private ObservableCollection<int> _options = new();
    [ObservableProperty] private bool _isGameOver;
    [ObservableProperty] private int _score;
    [ObservableProperty] private int _timeLeft = 45;
    [ObservableProperty] private string _mode = "Normal";
    [ObservableProperty] private int _level = 1;
    [ObservableProperty] private string _hudText = "Time: 45s | Score: 0";

    private IDispatcherTimer? _gameTimer;
    private int _correctAnswer;
    private int _attempts;
    private int _correctCount;
    private int _mistakes;

    public NumberGridViewModel()
    {
        _databaseService = App.DatabaseService;
    }

    public void InitGame()
    {
        Score = 0;
        TimeLeft = 45;
        Level = 1;
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

        GeneratePuzzle();
    }

    private void GeneratePuzzle()
    {
        var rand = new Random();
        int step = rand.Next(1, 4 + Level);
        int start = rand.Next(1, 20);

        Sequence.Clear();
        for (int i = 0; i < 3; i++)
        {
            Sequence.Add(start + (i * step));
        }
        
        _correctAnswer = start + (3 * step);

        var opts = new List<int> { _correctAnswer };
        while (opts.Count < 4)
        {
            int fake = _correctAnswer + rand.Next(-10, 11);
            if (!opts.Contains(fake) && fake > 0) opts.Add(fake);
        }
        
        Options = new ObservableCollection<int>(opts.OrderBy(x => rand.Next()));
    }

    [RelayCommand]
    public void SelectOption(int answer)
    {
        if (IsGameOver) return;
        _attempts++;

        if (answer == _correctAnswer)
        {
            if (App.ProfileService.GetProfile().HapticsEnabled)
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);

            Score += 20 * Level;
            _correctCount++;
            if (_correctCount % 3 == 0) Level++;
        }
        else
        {
            if (App.ProfileService.GetProfile().HapticsEnabled)
                HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);

            if (Mode == "Endless") _mistakes++;
            Score = Math.Max(0, Score - 5);
        }
        
        UpdateHud();

        if (Mode == "Endless" && _mistakes >= 3)
        {
            EndGame();
        }
        else
        {
            GeneratePuzzle();
        }
    }

    private void UpdateHud() => HudText = $"Time: {TimeLeft}s | Score: {Score} | Lvl: {Level}";

    private async void EndGame()
    {
        Cleanup();
        IsGameOver = true;
        double acc = _attempts == 0 ? 0 : (double)_correctCount / _attempts * 100;

        await _databaseService.SaveGameResultAsync(new GameResult
        {
            GameName = "NumberGrid",
            Category = "Logic",
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
                GameName = "NumberGrid",
                Category = "Logic",
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

