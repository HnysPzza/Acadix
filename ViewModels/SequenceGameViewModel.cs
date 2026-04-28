using AcadsJulie.Data;
using AcadsJulie.Models;
using AcadsJulie.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcadsJulie.ViewModels
{
    public class SequenceAnswerResult
    {
        public int SelectedOption { get; set; }
        public int CorrectOption { get; set; }
        public bool IsCorrect { get; set; }
    }

    [QueryProperty(nameof(Difficulty), "difficulty")]
    [QueryProperty(nameof(Mode), "mode")]
    public partial class SequenceGameViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private Queue<SequenceQuestion> _questionsQueue = new();

        [ObservableProperty]
        private string _difficulty = "Easy";

        [ObservableProperty]
        private string _mode = "Normal";

        [ObservableProperty]
        private SequenceQuestion? _currentQuestion;

        [ObservableProperty]
        private ObservableCollection<int> _shuffledOptions = new();

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
        private int _correctCount;

        [ObservableProperty]
        private string _hintText = string.Empty;

        [ObservableProperty]
        private bool _showHint;

        [ObservableProperty]
        private bool _isGameOver;

        [ObservableProperty]
        private string _grade = "F";

        [ObservableProperty]
        private double _problemTimeLeft = 15.0;

        [ObservableProperty]
        private double _problemMaxTime = 15.0;

        [ObservableProperty]
        private double _progressValue = 1.0;

        private bool _isProcessingAnswer;
        private DateTime _gameStartTime;
        private int _mistakes;
        private IDispatcherTimer? _problemTimer;

        public event EventHandler<SequenceAnswerResult>? OnRevealAnswer;

        public SequenceGameViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public SequenceGameViewModel() : this(App.DatabaseService)
        {
        }

        partial void OnDifficultyChanged(string value)
        {
            // Do NOT call InitGame() here — fires during construction before Dispatcher is ready.
            // InitGame() is called from SequenceGamePage.OnAppearing() instead.
        }

        public void InitGame()
        {
            Score = 0;
            Round = 0;
            CorrectCount = 0;
            _mistakes = 0;
            IsGameOver = false;
            _isProcessingAnswer = false;
            _gameStartTime = DateTime.Now;

            IEnumerable<SequenceQuestion> filtered = FocusQuestionBank();
            _questionsQueue = new Queue<SequenceQuestion>(filtered);

            _problemTimer = Application.Current?.Dispatcher.CreateTimer();
            if (_problemTimer != null)
            {
                _problemTimer.Interval = TimeSpan.FromMilliseconds(100);
                _problemTimer.Tick += (s, e) =>
                {
                    if (_isProcessingAnswer) return;
                    if (Mode != "Zen")
                    {
                        ProblemTimeLeft -= 0.1;
                        ProgressValue = ProblemTimeLeft / ProblemMaxTime;
                        
                        if (ProblemTimeLeft <= 0)
                        {
                            TimeOut();
                        }
                    }
                };
            }

            NextQuestion();
        }

        private IEnumerable<SequenceQuestion> FocusQuestionBank()
        {
            var all = SequenceQuestionBank.All;
            IEnumerable<SequenceQuestion> pool;

            switch (Difficulty)
            {
                case "Easy":
                    pool = all.Where(q => q.PatternType == "Arithmetic" || q.PatternType == "Geometric");
                    break;
                case "Hard":
                    pool = all.Where(q => q.PatternType != "Arithmetic");
                    break;
                case "Endless":
                case "Medium":
                default:
                    pool = all;
                    break;
            }

            return pool.OrderBy(_ => Random.Shared.Next()).Take(10);
        }

        private void LoadQuestion(SequenceQuestion question)
        {
            ShowHint = false;
            HintText = string.Empty;
            CurrentQuestion = question;
            Round++;
            
            var options = new List<int>(question.Options);
            options = options.OrderBy(_ => Random.Shared.Next()).ToList();
            
            ShuffledOptions.Clear();
            foreach (var opt in options)
                ShuffledOptions.Add(opt);

            ProblemMaxTime = Math.Max(5.0, 15.0 - (Round - 1) * 1.0);
            if (Difficulty == "Hard") ProblemMaxTime -= 2.0;
            else if (Difficulty == "Easy") ProblemMaxTime += 3.0;

            ProblemTimeLeft = ProblemMaxTime;
            ProgressValue = 1.0;

            _isProcessingAnswer = false;
            _problemTimer?.Start();
        }

        [RelayCommand]
        public void Answer(int chosen)
        {
            if (_isProcessingAnswer || CurrentQuestion == null) return;
            _isProcessingAnswer = true;
            _problemTimer?.Stop();

            bool isCorrect = chosen == CurrentQuestion.Answer;

            if (isCorrect)
            {
                if (App.ProfileService.GetProfile().HapticsEnabled)
                    HapticFeedback.Default.Perform(HapticFeedbackType.Click);

                Score += 150;
                CorrectCount++;
            }
            else
            {
                if (App.ProfileService.GetProfile().HapticsEnabled)
                    HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);

                if (Mode == "Endless") _mistakes++;

                HintText = CurrentQuestion.Hint;
                ShowHint = true;
            }

            OnRevealAnswer?.Invoke(this, new SequenceAnswerResult
            {
                SelectedOption = chosen,
                CorrectOption = CurrentQuestion.Answer,
                IsCorrect = isCorrect
            });

            Task.Delay(1200).ContinueWith(_ =>
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

        private void TimeOut()
        {
            if (_isProcessingAnswer || CurrentQuestion == null) return;
            _isProcessingAnswer = true;
            _problemTimer?.Stop();

            if (Mode == "Endless") _mistakes++;

            HintText = CurrentQuestion.Hint;
            ShowHint = true;

            OnRevealAnswer?.Invoke(this, new SequenceAnswerResult
            {
                SelectedOption = -999,
                CorrectOption = CurrentQuestion.Answer,
                IsCorrect = false
            });

            Task.Delay(1200).ContinueWith(_ =>
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
                    IEnumerable<SequenceQuestion> filtered = FocusQuestionBank();
                    _questionsQueue = new Queue<SequenceQuestion>(filtered);
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
            IsGameOver = true;

            if (CorrectCount >= 9) Grade = "S";
            else if (CorrectCount >= 7) Grade = "A";
            else if (CorrectCount >= 5) Grade = "B";
            else if (CorrectCount >= 3) Grade = "C";
            else Grade = "D";

            int duration = (int)(DateTime.Now - _gameStartTime).TotalSeconds;
            double accuracy = Round == 0 ? 0 : (double)CorrectCount / Round * 100;

            await _databaseService.SaveGameResultAsync(new GameResult
            {
                GameName = "Sequence",
                Category = "Logic",
                Score = Score,
                Accuracy = Math.Round(accuracy, 1),
                DurationSeconds = duration,
                PlayedAt = DateTime.Now,
                Difficulty = Difficulty
            });
        }

        public async Task SaveProgressEarlyAsync()
        {
            if (IsGameOver) return;

            int duration = (int)(DateTime.Now - _gameStartTime).TotalSeconds;
            double accuracy = Round <= 1 ? 0 : (double)CorrectCount / (Round - 1) * 100;

            await _databaseService.SaveGameResultAsync(new GameResult
            {
                GameName = "Sequence",
                Category = "Logic",
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
            _problemTimer?.Stop();
        }
    }
}
