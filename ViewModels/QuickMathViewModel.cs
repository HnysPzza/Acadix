using AcadsJulie.Models;
using AcadsJulie.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcadsJulie.ViewModels
{
    public class MathAnswerResult
    {
        public bool IsCorrect { get; set; }
        public int Chosen { get; set; }
        public int CorrectAnswer { get; set; }
        public int PointsGained { get; set; }
    }

    [QueryProperty(nameof(Difficulty), "difficulty")]
    [QueryProperty(nameof(Mode), "mode")]
    public partial class QuickMathViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private readonly MathProblemGenerator _generator;

        private IDispatcherTimer? _gameTimer;
        private IDispatcherTimer? _problemTimer;
        private IDispatcherTimer? _countdownTimer;

        [ObservableProperty]
        private string _difficulty = "Easy";

        [ObservableProperty]
        private string _mode = "Normal";

        [ObservableProperty]
        private string _problemExpression = string.Empty;

        [ObservableProperty]
        private ObservableCollection<int> _answerOptions = new();

        [ObservableProperty]
        private int _score;

        [ObservableProperty]
        private int _timeLeft;

        [ObservableProperty]
        private int _solved;

        [ObservableProperty]
        private int _combo = 1;

        [ObservableProperty]
        private string _comboStars = "⭐";

        [ObservableProperty]
        private double _problemTimeLeft = 5.0;

        [ObservableProperty]
        private double _problemMaxTime = 5.0;

        [ObservableProperty]
        private double _progressValue = 1.0;

        [ObservableProperty]
        private int _level = 1;

        [ObservableProperty]
        private bool _isGameOver;

        [ObservableProperty]
        private bool _showCountdown;

        [ObservableProperty]
        private int _countdownNumber;

        private int _mistakes;
        private int _totalAttempts;
        private int _bestCombo;
        private int _currentAnswer;
        private bool _isProcessing;

        public event EventHandler<MathAnswerResult>? OnRevealAnswer;

        public QuickMathViewModel() : this(App.DatabaseService)
        {
        }

        public QuickMathViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _generator = new MathProblemGenerator();
        }

        partial void OnDifficultyChanged(string value)
        {
            // Do NOT call InitGame() here — this partial fires during property initialization
            // in the constructor before Application.Current/Dispatcher is ready on Android.
            // InitGame() is called from QuickMathPage.OnAppearing() instead.
        }

        public void InitGame()
        {
            Cleanup();
            IsGameOver = false;
            Score = 0;
            Solved = 0;
            Combo = 1;
            Level = 1;
            ComboStars = "⭐";
            _bestCombo = 0;
            _mistakes = 0;
            _totalAttempts = 0;
            _isProcessing = false;

            TimeLeft = Difficulty switch
            {
                "Easy" => 70,
                "Medium" => 60,
                "Hard" => 45,
                _ => 60
            };

            ProblemMaxTime = Difficulty switch
            {
                "Easy" => 7.0,
                "Medium" => 5.0,
                "Hard" => 4.0,
                _ => 5.0
            };

            StartCountdown();
        }

        private void StartCountdown()
        {
            ShowCountdown = true;
            CountdownNumber = 3;

            _countdownTimer = Application.Current?.Dispatcher.CreateTimer();
            if (_countdownTimer != null)
            {
                _countdownTimer.Interval = TimeSpan.FromSeconds(1);
                _countdownTimer.Tick += (s, e) =>
                {
                    CountdownNumber--;
                    if (CountdownNumber <= 0)
                    {
                        _countdownTimer.Stop();
                        ShowCountdown = false;
                        StartGame();
                    }
                };
                _countdownTimer.Start();
            }
        }

        private void StartGame()
        {
            _gameTimer = Application.Current?.Dispatcher.CreateTimer();
            if (_gameTimer != null)
            {
                _gameTimer.Interval = TimeSpan.FromSeconds(1);
                _gameTimer.Tick += (s, e) =>
                {
                    if (Mode == "Normal")
                    {
                        TimeLeft--;
                        if (TimeLeft <= 0) EndGame();
                    }
                };
                _gameTimer.Start();
            }

            _problemTimer = Application.Current?.Dispatcher.CreateTimer();
            if (_problemTimer != null)
            {
                _problemTimer.Interval = TimeSpan.FromMilliseconds(100); // UI progress update
                _problemTimer.Tick += (s, e) =>
                {
                    if (_isProcessing) return;
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

            LoadProblem();
        }

        private void LoadProblem()
        {
            var problem = _generator.Generate(Difficulty);
            ProblemExpression = problem.Expression;
            _currentAnswer = problem.Answer;
            
            AnswerOptions.Clear();
            foreach (var opt in problem.Options)
            {
                AnswerOptions.Add(opt);
            }

            ProblemTimeLeft = ProblemMaxTime;
            ProgressValue = 1.0;
            _isProcessing = false;
            _problemTimer?.Start();
        }

        private void TimeOut()
        {
            if (_isProcessing) return;
            _isProcessing = true;
            _problemTimer?.Stop();

            ResetCombo();
            _totalAttempts++;
            if (Mode == "Endless") _mistakes++;

            OnRevealAnswer?.Invoke(this, new MathAnswerResult
            {
                IsCorrect = false,
                Chosen = -999,
                CorrectAnswer = _currentAnswer,
                PointsGained = 0
            });

            Task.Delay(500).ContinueWith(_ =>
            {
                if (Mode == "Endless" && _mistakes >= 3)
                {
                    Application.Current?.Dispatcher.Dispatch(EndGame);
                }
                else
                {
                    Application.Current?.Dispatcher.Dispatch(LoadProblem);
                }
            });
        }

        [RelayCommand]
        public void Answer(int chosen)
        {
            if (_isProcessing || TimeLeft <= 0 || ShowCountdown) return;
            _isProcessing = true;
            _problemTimer?.Stop();

            _totalAttempts++;
            bool isCorrect = chosen == _currentAnswer;
            int pts = 0;

            if (isCorrect)
            {
                if (App.ProfileService.GetProfile().HapticsEnabled)
                    HapticFeedback.Default.Perform(HapticFeedbackType.Click);

                Solved++;
                int speedBonus = (int)(ProblemTimeLeft * 20);
                pts = (100 + speedBonus) * Combo;
                Score += pts;
                
                Combo = Math.Min(Combo + 1, 5);
                if (Combo > _bestCombo) _bestCombo = Combo;

                ComboStars = new string('⭐', Combo);

                Level = 1 + (Solved / 5);
                
                double baseMax = Difficulty switch
                {
                    "Easy" => 7.0,
                    "Medium" => 5.0,
                    "Hard" => 4.0,
                    _ => 5.0
                };
                
                ProblemMaxTime = Math.Max(1.5, baseMax - ((Level - 1) * 0.5));
            }
            else
            {
                if (App.ProfileService.GetProfile().HapticsEnabled)
                    HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);

                if (Mode == "Endless") _mistakes++;
                ResetCombo();
            }

            OnRevealAnswer?.Invoke(this, new MathAnswerResult
            {
                IsCorrect = isCorrect,
                Chosen = chosen,
                CorrectAnswer = _currentAnswer,
                PointsGained = pts
            });

            Task.Delay(500).ContinueWith(_ =>
            {
                if (Mode == "Endless" && _mistakes >= 3)
                {
                    Application.Current?.Dispatcher.Dispatch(EndGame);
                }
                else
                {
                    Application.Current?.Dispatcher.Dispatch(LoadProblem);
                }
            });
        }

        private void ResetCombo()
        {
            Combo = 1;
            ComboStars = "⭐";
        }

        private async void EndGame()
        {
            Cleanup();
            IsGameOver = true;

            int duration = Difficulty switch
            {
                "Easy" => 70 - TimeLeft,
                "Medium" => 60 - TimeLeft,
                "Hard" => 45 - TimeLeft,
                _ => 60 - TimeLeft
            };
            
            // Just in case time hits 0
            if (duration < 0) duration = 60;

            double accuracy = _totalAttempts == 0 ? 0 : (double)Solved / _totalAttempts * 100;

            await _databaseService.SaveGameResultAsync(new GameResult
            {
                GameName = "QuickMath",
                Category = "Speed",
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
            
            int duration = Difficulty switch
            {
                "Easy" => 70 - TimeLeft,
                "Medium" => 60 - TimeLeft,
                "Hard" => 45 - TimeLeft,
                _ => 60 - TimeLeft
            };
            if (duration < 0) duration = 0;

            double accuracy = _totalAttempts == 0 ? 0 : (double)Solved / _totalAttempts * 100;

            await _databaseService.SaveGameResultAsync(new GameResult
            {
                GameName = "QuickMath",
                Category = "Speed",
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
            _countdownTimer?.Stop();
            _gameTimer?.Stop();
            _problemTimer?.Stop();
        }
    }
}
