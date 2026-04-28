using AcadsJulie.Data;
using AcadsJulie.Models;
using AcadsJulie.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcadsJulie.ViewModels
{
    public class FocusAnswerResult
    {
        public string SelectedOption { get; set; } = string.Empty;
        public string CorrectOption { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }

    [QueryProperty(nameof(Difficulty), "difficulty")]
    [QueryProperty(nameof(Mode), "mode")]
    public partial class OddOneOutViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private IDispatcherTimer? _timer;
        private Queue<FocusQuestion> _questionsQueue = new();

        [ObservableProperty]
        private string _difficulty = "Easy";

        [ObservableProperty]
        private string _mode = "Normal";

        [ObservableProperty]
        private FocusQuestion? _currentQuestion;

        [ObservableProperty]
        private ObservableCollection<string> _shuffledOptions = new();

        [ObservableProperty]
        private int _score;

        [ObservableProperty]
        private int _round;

        public double RoundProgress => (double)Round / 10.0;

        partial void OnRoundChanged(int value)
        {
            OnPropertyChanged(nameof(RoundProgress));
        }

        [ObservableProperty]
        private int _streak;

        [ObservableProperty]
        private double _timeLeft;

        [ObservableProperty]
        private double _maxTime = 8.0;

        [ObservableProperty]
        private bool _isGameOver;

        private int _correctCount;
        private int _bestStreak;
        private bool _isProcessingAnswer;
        private int _mistakes;

        public event EventHandler<FocusAnswerResult>? OnRevealAnswer;

        public OddOneOutViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public OddOneOutViewModel() : this(App.DatabaseService)
        {
        }

        partial void OnDifficultyChanged(string value)
        {
            // Do NOT call InitGame() here — fires during construction before Dispatcher is ready.
            // InitGame() is called from OddOneOutPage.OnAppearing() instead.
        }

        public void InitGame()
        {
            _timer?.Stop();
            Score = 0;
            Round = 0;
            Streak = 0;
            _bestStreak = 0;
            _correctCount = 0;
            _mistakes = 0;
            IsGameOver = false;
            _isProcessingAnswer = false;

            MaxTime = Difficulty switch
            {
                "Easy" => 12.0,
                "Medium" => 8.0,
                "Hard" => 5.0,
                _ => 8.0
            };

            int numQuestions = Mode == "Normal" ? 10 : 50;
            var questions = FocusQuestionBank.All.OrderBy(_ => Random.Shared.Next()).Take(numQuestions).ToList();
            _questionsQueue = new Queue<FocusQuestion>(questions);

            _timer = Application.Current?.Dispatcher.CreateTimer();
            if (_timer != null)
            {
                _timer.Interval = TimeSpan.FromMilliseconds(100);
                _timer.Tick += (s, e) =>
                {
                    if (Mode != "Zen")
                    {
                        TimeLeft -= 0.1;
                        if (TimeLeft <= 0)
                            HandleTimeOut();
                    }
                };
            }

            NextQuestion();
        }

        private void LoadQuestion(FocusQuestion question)
        {
            CurrentQuestion = question;
            Round++;
            
            var options = new List<string>(question.Options);
            options = options.OrderBy(_ => Random.Shared.Next()).ToList();
            
            ShuffledOptions.Clear();
            foreach (var opt in options)
                ShuffledOptions.Add(opt);

            TimeLeft = MaxTime;
            _isProcessingAnswer = false;
            _timer?.Start();
        }

        private void HandleTimeOut()
        {
            if (_isProcessingAnswer) return;
            _isProcessingAnswer = true;
            _timer?.Stop();

            Streak = 0;
            if (Mode == "Endless") _mistakes++;
            
            OnRevealAnswer?.Invoke(this, new FocusAnswerResult
            {
                SelectedOption = string.Empty, // timeout
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
            if (_isProcessingAnswer || CurrentQuestion == null) return;
            _isProcessingAnswer = true;
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
                if (Streak > _bestStreak) _bestStreak = Streak;
            }
            else
            {
                if (App.ProfileService.GetProfile().HapticsEnabled)
                    HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);

                if (Mode == "Endless") _mistakes++;
                Streak = 0;
            }

            OnRevealAnswer?.Invoke(this, new FocusAnswerResult
            {
                SelectedOption = chosen,
                CorrectOption = CurrentQuestion.CorrectAnswer,
                IsCorrect = isCorrect
            });

            Task.Delay(1000).ContinueWith(_ =>
            {
                if (Mode == "Endless" && _mistakes >= 3)
                {
                    Application.Current?.Dispatcher.Dispatch(EndGame);
                }
                else
                {
                    Application.Current?.Dispatcher.Dispatch(NextQuestion);
                }
            });
        }

        private void NextQuestion()
        {
            if (_questionsQueue.Count > 0)
            {
                LoadQuestion(_questionsQueue.Dequeue());
            }
            else
            {
                if (Mode != "Normal")
                {
                    // Refill
                    var questions = FocusQuestionBank.All.OrderBy(_ => Random.Shared.Next()).Take(50).ToList();
                    _questionsQueue = new Queue<FocusQuestion>(questions);
                    LoadQuestion(_questionsQueue.Dequeue());
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

            int duration = Round * (int)MaxTime; // Max possible duration for simplicity, or we could track precise time. Let's do estimated.
            double accuracy = Round == 0 ? 0 : (double)_correctCount / Round * 100;

            await _databaseService.SaveGameResultAsync(new GameResult
            {
                GameName = "OddOneOut",
                Category = "Focus",
                Score = Score,
                Accuracy = Math.Round(accuracy, 1),
                DurationSeconds = duration,
                PlayedAt = DateTime.Now,
                Difficulty = Difficulty
            });
        }

        [RelayCommand]
        public void RestartGame()
        {
            InitGame();
        }

        public void Cleanup()
        {
            _timer?.Stop();
        }
    }
}
