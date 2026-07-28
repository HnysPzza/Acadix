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
    private readonly TriviaQuestionProvider _questionProvider;
    private IDispatcherTimer? _timer;
    private Queue<TriviaQuestion> _questionsQueue = new();

    // Cancels an in-flight question fetch when the player leaves the page or restarts.
    private CancellationTokenSource? _loadCts;

    [ObservableProperty] private string _field = "General";
    [ObservableProperty] private string _subField = "GeneralKnowledge";
    [ObservableProperty] private string _difficulty = "Easy";
    [ObservableProperty] private string _mode = "Normal";

    [ObservableProperty] private TriviaQuestion? _currentQuestion;
    [ObservableProperty] private ObservableCollection<string> _shuffledOptions = new();
    [ObservableProperty] private string _parrotAssetSource = "parrot_idle.json";
    [ObservableProperty] private string _selectedOption = string.Empty;
    [ObservableProperty] private bool _hasNoQuestions;

    /// <summary>True while questions are being fetched, so the UI can show a loading state.</summary>
    [ObservableProperty] private bool _isLoadingQuestions;

    /// <summary>Short status line shown under the loader / after load (may be empty).</summary>
    [ObservableProperty] private string _sourceNotice = string.Empty;

    /// <summary>Drives visibility of the notice banner (no value converters exist in this project).</summary>
    public bool HasSourceNotice => !string.IsNullOrWhiteSpace(SourceNotice);

    partial void OnSourceNoticeChanged(string value) => OnPropertyChanged(nameof(HasSourceNotice));

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

    /// <summary>Questions fetched per batch in Endless/Zen (also the API's per-request maximum).</summary>
    private const int EndlessBatchSize = 50;

    /// <summary>Start fetching the next batch once the queue drops to this many questions.</summary>
    private const int PrefetchThreshold = 5;

    private int _correctCount;
    private int _mistakes;
    private DateTime _sessionStart;
    private bool _isProcessingAnswer;
    private bool _isPrefetching;

    public event EventHandler<TriviaAnswerResult>? OnRevealAnswer;

    /// <summary>Fired at the end of <see cref="InitGame"/> so the UI can refresh Lottie (e.g. Play Again).</summary>
    public event Action? GameSessionInitialized;

    public TriviaViewModel() : this(App.DatabaseService, App.TriviaQuestionProvider) { }

    public TriviaViewModel(DatabaseService databaseService, TriviaQuestionProvider questionProvider)
    {
        _databaseService = databaseService;
        _questionProvider = questionProvider;
    }

    /// <summary>
    /// Starts a new round. Kept synchronous so existing callers (page load, "Play Again") do not
    /// change; the question fetch runs in the background and the UI shows a loading state until
    /// it completes.
    /// </summary>
    public void InitGame() => _ = InitGameAsync();

    public async Task InitGameAsync()
    {
        // Abandon any fetch still running from a previous round.
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = new CancellationTokenSource();
        var cancellationToken = _loadCts.Token;

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
        SourceNotice = string.Empty;
        _sessionStart = DateTime.Now;

        MaxTime = Difficulty switch
        {
            "Easy" => 12.0,
            "Medium" => 8.0,
            "Hard" => 5.0,
            _ => 8.0
        };

        int numQuestions = Mode == "Normal" ? 10 : EndlessBatchSize;

        // Show the loader and clear the previous question so nothing stale is visible.
        IsLoadingQuestions = true;
        CurrentQuestion = null;
        ShuffledOptions.Clear();
        TotalRounds = 0;

        TriviaQuestionProvider.QuestionBatch batch;
        try
        {
            batch = await _questionProvider.GetQuestionsAsync(
                Field, SubField, Difficulty, numQuestions, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // A newer round took over; let that one drive the UI.
            return;
        }
        finally
        {
            if (!cancellationToken.IsCancellationRequested)
                RunOnMainThread(() => IsLoadingQuestions = false);
        }

        if (cancellationToken.IsCancellationRequested)
            return;

        // The continuation above may resume off the UI thread; everything below touches
        // observable state and the question queue, so finish the setup on the main thread.
        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(() => ApplyLoadedBatch(batch));
            return;
        }

        ApplyLoadedBatch(batch);
    }

    /// <summary>Applies a freshly loaded batch and starts the round. Must run on the UI thread.</summary>
    private void ApplyLoadedBatch(TriviaQuestionProvider.QuestionBatch batch)
    {
        // The session clock should start when the player actually sees question one, not when
        // the network request began.
        _sessionStart = DateTime.Now;

        SourceNotice = BuildSourceNotice(batch);

        TotalRounds = batch.Questions.Count;
        _questionsQueue = new Queue<TriviaQuestion>(batch.Questions);

        if (_questionsQueue.Count == 0)
        {
            HasNoQuestions = true;
            CurrentQuestion = new TriviaQuestion
            {
                QuestionText = batch.UsedOfflineFallback
                    ? "Couldn't load questions. Check your connection and try again."
                    : "No questions available for this category yet. Please try another category."
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

    private static string BuildSourceNotice(TriviaQuestionProvider.QuestionBatch batch)
    {
        if (batch.Questions.Count == 0)
            return string.Empty;

        if (batch.UsedOfflineFallback)
            return "Offline — using Acadix's own questions.";

        if (batch.HistoryRecycled)
            return "You've seen every question here — starting a fresh cycle.";

        return string.Empty;
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
            MaybePrefetchMore();
            return;
        }

        if (Mode == "Normal")
        {
            EndGame();
            return;
        }

        // Endless/Zen ran dry before the prefetch landed — wait for a top-up.
        _ = RefillAndContinueAsync();
    }

    /// <summary>
    /// In Endless/Zen, fetches the next batch in the background once the queue runs low, so the
    /// player never waits on the network mid-round.
    /// </summary>
    private void MaybePrefetchMore()
    {
        if (Mode == "Normal" || _isPrefetching)
            return;

        if (_questionsQueue.Count > PrefetchThreshold)
            return;

        _isPrefetching = true;
        _ = PrefetchAsync();
    }

    private async Task PrefetchAsync()
    {
        try
        {
            var token = _loadCts?.Token ?? CancellationToken.None;
            var batch = await _questionProvider.GetQuestionsAsync(
                Field, SubField, Difficulty, EndlessBatchSize, token);

            if (token.IsCancellationRequested || batch.Questions.Count == 0)
                return;

            // The queue is consumed on the UI thread, so enqueue there too rather than mutating
            // it from this background continuation.
            EnqueueOnMainThread(batch.Questions);
        }
        catch (OperationCanceledException)
        {
            // Round ended while fetching.
        }
        finally
        {
            _isPrefetching = false;
        }
    }

    private void EnqueueOnMainThread(List<TriviaQuestion> questions) =>
        RunOnMainThread(() =>
        {
            foreach (var question in questions)
                _questionsQueue.Enqueue(question);

            TotalRounds += questions.Count;
        });

    /// <summary>
    /// Runs <paramref name="action"/> on the UI thread. The question queue and observable state
    /// are touched from network continuations, which may resume on a background thread.
    /// </summary>
    private static void RunOnMainThread(Action action)
    {
        if (MainThread.IsMainThread)
            action();
        else
            MainThread.BeginInvokeOnMainThread(action);
    }

    /// <summary>Blocking top-up for when the queue empties before a prefetch completes.</summary>
    private async Task RefillAndContinueAsync()
    {
        IsLoadingQuestions = true;
        try
        {
            var token = _loadCts?.Token ?? CancellationToken.None;
            var batch = await _questionProvider.GetQuestionsAsync(
                Field, SubField, Difficulty, EndlessBatchSize, token);

            if (token.IsCancellationRequested)
                return;

            if (batch.Questions.Count == 0)
            {
                RunOnMainThread(EndGame);
                return;
            }

            RunOnMainThread(() =>
            {
                foreach (var question in batch.Questions)
                    _questionsQueue.Enqueue(question);

                TotalRounds += batch.Questions.Count;
                LoadQuestion(_questionsQueue.Dequeue());
            });
        }
        catch (OperationCanceledException)
        {
            // Player left the page.
        }
        finally
        {
            IsLoadingQuestions = false;
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

    public void Cleanup()
    {
        _timer?.Stop();

        // Abandon any in-flight fetch so it cannot resurrect a finished round.
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;
    }
}
