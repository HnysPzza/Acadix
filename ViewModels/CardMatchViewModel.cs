using AcadsJulie.Models;
using AcadsJulie.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcadsJulie.ViewModels
{
    [QueryProperty(nameof(Difficulty), "difficulty")]
    [QueryProperty(nameof(Mode), "mode")]
    public partial class CardMatchViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private IDispatcherTimer? _timer;

        public ObservableCollection<CardItem> Cards { get; } = new ObservableCollection<CardItem>();

        [ObservableProperty]
        private string _difficulty = "Easy";

        [ObservableProperty]
        private string _mode = "Normal";

        [ObservableProperty]
        private int _moves;

        [ObservableProperty]
        private int _matchedPairs;

        [ObservableProperty]
        private int _timeLeft;

        [ObservableProperty]
        private bool _isGameOver;

        [ObservableProperty]
        private int _score;

        private CardItem? _firstCard;
        private CardItem? _secondCard;
        private bool _isProcessingMatch;
        private int _mistakes;

        public CardMatchViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public CardMatchViewModel() : this(App.DatabaseService)
        {
        }

        partial void OnDifficultyChanged(string value)
        {
            // Do NOT call InitGame() here — fires during construction before Dispatcher is ready.
            // InitGame() is called from CardMatchPage.OnAppearing() instead.
        }

        public void InitGame()
        {
            _timer?.Stop();
            Moves = 0;
            MatchedPairs = 0;
            IsGameOver = false;
            Score = 0;
            _mistakes = 0;
            _firstCard = null;
            _secondCard = null;
            _isProcessingMatch = false;

            // Set time based on difficulty
            TimeLeft = Difficulty switch
            {
                "Easy" => 90,
                "Medium" => 60,
                "Hard" => 45,
                _ => 60
            };

            var emojis = new List<string> { "🦊", "🐬", "🌸", "🍄", "⚡", "🎸", "🦋", "🔮" };
            
            // Hard difficulty can have more if requested, but for now we stick to 8 pairs
            var requiredPairs = Difficulty == "Hard" ? 10 : 8;
            if (requiredPairs > emojis.Count)
            {
                emojis.Add("🍎");
                emojis.Add("🚀");
            }

            var selectedEmojis = emojis.Take(requiredPairs).ToList();
            var deck = new List<CardItem>();
            int id = 0;
            
            foreach (var emoji in selectedEmojis)
            {
                deck.Add(new CardItem { Id = id++, Emoji = emoji });
                deck.Add(new CardItem { Id = id++, Emoji = emoji });
            }

            var shuffled = deck.OrderBy(_ => Random.Shared.Next()).ToList();
            Cards.Clear();
            foreach (var card in shuffled)
                Cards.Add(card);

            _timer = Application.Current?.Dispatcher.CreateTimer();
            if (_timer != null)
            {
                _timer.Interval = TimeSpan.FromSeconds(1);
                _timer.Tick += (s, e) =>
                {
                    if (Mode == "Normal")
                    {
                        TimeLeft--;
                        if (TimeLeft <= 0)
                            EndGame(false);
                    }
                };
                _timer.Start();
            }
        }

        [RelayCommand]
        public void FlipCard(CardItem card)
        {
            if (card == null || card.IsFlipped || card.IsMatched || _isProcessingMatch)
                return;

            card.IsFlipped = true;

            if (_firstCard == null)
            {
                _firstCard = card;
            }
            else if (_secondCard == null)
            {
                _secondCard = card;
                _isProcessingMatch = true;
                
                // Trigger event for UI to know it needs to animate or delay logic via Task
                Task.Delay(700).ContinueWith(_ => 
                {
                    Application.Current?.Dispatcher.Dispatch(CheckMatch);
                });
            }
        }

        private void CheckMatch()
        {
            if (_firstCard == null || _secondCard == null) return;

            Moves++;

            if (_firstCard.Emoji == _secondCard.Emoji)
            {
                if (App.ProfileService.GetProfile().HapticsEnabled)
                    HapticFeedback.Default.Perform(HapticFeedbackType.Click);

                _firstCard.IsMatched = true;
                _secondCard.IsMatched = true;
                MatchedPairs++;

                if (MatchedPairs == Cards.Count / 2)
                {
                    EndGame(true);
                }
            }
            else
            {
                if (App.ProfileService.GetProfile().HapticsEnabled)
                    HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);

                if (Mode == "Endless") _mistakes++;

                _firstCard.IsFlipped = false;
                _secondCard.IsFlipped = false;
            }

            _firstCard = null;
            _secondCard = null;
            _isProcessingMatch = false;

            if (Mode == "Endless" && _mistakes >= 3)
            {
                EndGame(false);
            }
        }

        private async void EndGame(bool won)
        {
            _timer?.Stop();
            IsGameOver = true;

            int duration = Difficulty switch
            {
                "Easy" => 90 - TimeLeft,
                "Medium" => 60 - TimeLeft,
                "Hard" => 45 - TimeLeft,
                _ => 60 - TimeLeft
            };

            Score = won ? (MatchedPairs * 100) - (Moves * 5) + (TimeLeft * 10) : MatchedPairs * 50;
            Score = Math.Max(0, Score);

            double accuracy = Moves == 0 ? 0 : (double)MatchedPairs / Moves * 100;

            await _databaseService.SaveGameResultAsync(new GameResult
            {
                GameName = "CardMatch",
                Category = "Memory",
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

        // Must clean up timer when navigating away
        public void Cleanup()
        {
            _timer?.Stop();
        }
    }
}
