using AcadsJulie.Data;
using AcadsJulie.Models;
using AcadsJulie.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcadsJulie.ViewModels;

public class TriviaAnswerResult
{
    public string SelectedOption { get; set; } = string.Empty;
    public string CorrectOption { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

[QueryProperty(nameof(Field), "field")]
[QueryProperty(nameof(SubField), "subField")]
[QueryProperty(nameof(Difficulty), "difficulty")]
[QueryProperty(nameof(Mode), "mode")]
public partial class TriviaViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;
    private IDispatcherTimer? _timer;
    private Queue<TriviaQuestion> _questionsQueue = new();

    [ObservableProperty] private string _field = "General";
    [ObservableProperty] private string _subField = "GeneralKnowledge";
    [ObservableProperty] private string _difficulty = "Easy";
    [ObservableProperty] private string _mode = "Normal";

    [ObservableProperty] private TriviaQuestion? _currentQuestion;
    [ObservableProperty] private ObservableCollection<string> _shuffledOptions = new();
    [ObservableProperty] private string _parrotAssetSource = "parrot_idle.json";
    [ObservableProperty] private string _selectedOption = string.Empty;
    [ObservableProperty] private bool _hasNoQuestions;

    [ObservableProperty] private int _score;
    [ObservableProperty] private int _round;
    [ObservableProperty] private int _totalRounds = 10;
    public string ScoreDisplay => $"{Score:N0} pts";
    public double RoundProgress => TotalRounds <= 0 ? 0 : (double)Round / TotalRounds;
    public string RoundDisplay => TotalRounds <= 0 ? "0/0" : $"{Round}/{TotalRounds}";
    public string CurrentQuestionText => CurrentQuestion?.QuestionText?.Trim() ?? string.Empty;

    partial void OnScoreChanged(int value) => OnPropertyChanged(nameof(ScoreDisplay));

    partial void OnRoundChanged(int value)
    {
        OnPropertyChanged(nameof(RoundProgress));
        OnPropertyChanged(nameof(RoundDisplay));
    }

    partial void OnTotalRoundsChanged(int value)
    {
        OnPropertyChanged(nameof(RoundProgress));
        OnPropertyChanged(nameof(RoundDisplay));
    }

    partial void OnCurrentQuestionChanged(TriviaQuestion? value) => OnPropertyChanged(nameof(CurrentQuestionText));

    [ObservableProperty] private int _streak;
    [ObservableProperty] private double _timeLeft;
    [ObservableProperty] private double _maxTime = 8.0;
    [ObservableProperty] private bool _isGameOver;

    private int _correctCount;
    private int _mistakes;
    private DateTime _sessionStart;
    private bool _isProcessingAnswer;

    public event EventHandler<TriviaAnswerResult>? OnRevealAnswer;

    /// <summary>Fired at the end of <see cref="InitGame"/> so the UI can refresh Lottie (e.g. Play Again).</summary>
    public event Action? GameSessionInitialized;

    public TriviaViewModel() : this(App.DatabaseService) { }

    public TriviaViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public void InitGame()
    {
        _timer?.Stop();
        Score = 0;
        Round = 0;
        Streak = 0;
        _correctCount = 0;
        _mistakes = 0;
        _isProcessingAnswer = false;
        SelectedOption = string.Empty;
        HasNoQuestions = false;
        IsGameOver = false;
        _sessionStart = DateTime.Now;

        MaxTime = Difficulty switch
        {
            "Easy" => 12.0,
            "Medium" => 8.0,
            "Hard" => 5.0,
            _ => 8.0
        };

        int numQuestions = Mode == "Normal" ? 10 : 50;
        var questions = TriviaQuestionBank.GetQuestions(Field, SubField, Difficulty, numQuestions);
        TotalRounds = questions.Count;
        _questionsQueue = new Queue<TriviaQuestion>(questions);

        if (_questionsQueue.Count == 0)
        {
            HasNoQuestions = true;
            CurrentQuestion = new TriviaQuestion
            {
                QuestionText = "No questions available for this category yet. Please try another category."
            };
            ShuffledOptions.Clear();
            TimeLeft = 0;
            TotalRounds = 0;
            GameSessionInitialized?.Invoke();
            return;
        }

        _timer = Application.Current?.Dispatcher.CreateTimer();
        if (_timer != null)
        {
            _timer.Interval = TimeSpan.FromMilliseconds(100);
            _timer.Tick += (s, e) =>
            {
                if (Mode != "Zen")
                {
                    TimeLeft -= 0.1;
                    if (TimeLeft <= 0) HandleTimeOut();
                }
            };
        }

        NextQuestion();
        GameSessionInitialized?.Invoke();
    }

    private void LoadQuestion(TriviaQuestion question)
    {
        _isProcessingAnswer = false;
        SelectedOption = string.Empty;
        CurrentQuestion = question;
        Round++;
        var options = question.Options.OrderBy(_ => Random.Shared.Next()).ToList();
        ShuffledOptions.Clear();
        foreach (var opt in options) ShuffledOptions.Add(opt);
        TimeLeft = MaxTime;
        _timer?.Start();
    }

    private void HandleTimeOut()
    {
        if (CurrentQuestion == null || _isProcessingAnswer) return;
        _isProcessingAnswer = true;
        _timer?.Stop();
        Streak = 0;
        if (Mode == "Endless") _mistakes++;

        OnRevealAnswer?.Invoke(this, new TriviaAnswerResult
        {
            SelectedOption = "",
            CorrectOption = CurrentQuestion!.CorrectAnswer,
            IsCorrect = false
        });

        Task.Delay(1200).ContinueWith(_ =>
        {
            Application.Current?.Dispatcher.Dispatch(NextQuestion);
        });
    }

    [RelayCommand]
    public void Answer(string chosen)
    {
        if (CurrentQuestion == null || _isProcessingAnswer || string.IsNullOrWhiteSpace(chosen)) return;
        _isProcessingAnswer = true;
        SelectedOption = chosen;
        _timer?.Stop();

        bool isCorrect = chosen == CurrentQuestion.CorrectAnswer;

        if (isCorrect)
        {
            if (App.ProfileService.GetProfile().HapticsEnabled)
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            int timeBonus = (int)(TimeLeft * 10);
            Score += 100 + timeBonus;
            Streak++;
            _correctCount++;
        }
        else
        {
            if (App.ProfileService.GetProfile().HapticsEnabled)
                HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
            if (Mode == "Endless") _mistakes++;
            Streak = 0;
        }

        OnRevealAnswer?.Invoke(this, new TriviaAnswerResult
        {
            SelectedOption = chosen,
            CorrectOption = CurrentQuestion.CorrectAnswer,
            IsCorrect = isCorrect
        });

        Task.Delay(1000).ContinueWith(_ =>
        {
            if (Mode == "Endless" && _mistakes >= 3)
                Application.Current?.Dispatcher.Dispatch(EndGame);
            else
                Application.Current?.Dispatcher.Dispatch(NextQuestion);
        });
    }

    private void NextQuestion()
    {
        _isProcessingAnswer = false;
        if (_questionsQueue.Count > 0)
        {
            LoadQuestion(_questionsQueue.Dequeue());
        }
        else
        {
            if (Mode != "Normal")
            {
                var questions = TriviaQuestionBank.GetQuestions(Field, SubField, Difficulty, 50);
                TotalRounds = questions.Count;
                _questionsQueue = new Queue<TriviaQuestion>(questions);
                if (_questionsQueue.Count > 0)
                    LoadQuestion(_questionsQueue.Dequeue());
                else
                    EndGame();
            }
            else
            {
                EndGame();
            }
        }
    }

    private async void EndGame()
    {
        _timer?.Stop();
        IsGameOver = true;

        int duration = (int)(DateTime.Now - _sessionStart).TotalSeconds;
        double accuracy = Round == 0 ? 0 : (double)_correctCount / Round * 100;
        var subCat = $"{Field}_{SubField}_{Difficulty}";

        await _databaseService.SaveGameResultAsync(new GameResult
        {
            GameName = "Trivia",
            Category = "Trivia",
            SubCategory = subCat,
            Score = Score,
            Accuracy = Math.Round(accuracy, 1),
            DurationSeconds = duration,
            PlayedAt = DateTime.Now,
            Difficulty = Difficulty
        });
    }

    [RelayCommand]
    public void RestartGame() => InitGame();

    public void Cleanup() => _timer?.Stop();
}
