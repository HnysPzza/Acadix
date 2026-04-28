using AcadsJulie.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;

namespace AcadsJulie.Views.Games
{
    public partial class QuickMathPage : ContentPage
    {
        private readonly QuickMathViewModel _vm;
        private readonly List<Button> _optionButtons = new();

        private bool _isFirstLoad = true;

        public QuickMathPage()
        {
            InitializeComponent();
            _vm = new QuickMathViewModel();
            BindingContext = _vm;

            _vm.AnswerOptions.CollectionChanged += OnOptionsChanged;
            _vm.OnRevealAnswer += OnRevealAnswer;
            _vm.PropertyChanged += OnVmPropertyChanged;

            this.Loaded += (s, e) => 
            {
                if (_isFirstLoad) 
                {
                    _isFirstLoad = false;
                    _vm.InitGame();
                }
            };
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // InitGame deferred to Loaded event for Android layout safety
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _vm.Cleanup();
        }

        private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(QuickMathViewModel.CountdownNumber))
            {
                Dispatcher.Dispatch(async () =>
                {
                    try 
                    {
                        if (CountdownLabel.IsLoaded) 
                        {
                            await CountdownLabel.ScaleToAsync(1.3, 0);
                            await CountdownLabel.ScaleToAsync(1.0, 200, Easing.BounceOut);
                        }
                    } catch { }
                });
            }
        }

        protected override bool OnBackButtonPressed()
        {
            if (!_vm.IsGameOver)
            {
                Dispatcher.Dispatch(async () =>
                {
                    var quit = await DisplayAlertAsync("Quit game?", "Progress will be saved up to this point.", "Quit", "Cancel");
                    if (quit)
                    {
                        await _vm.SaveProgressEarlyAsync();
                        _vm.Cleanup();
                        await Shell.Current.GoToAsync("..");
                    }
                });
                return true;
            }
            return base.OnBackButtonPressed();
        }

        private async void OnBackClicked(object sender, EventArgs e)
        {
            if (!_vm.IsGameOver)
            {
                var quit = await DisplayAlertAsync("Quit game?", "Progress will be saved up to this point.", "Quit", "Cancel");
                if (quit)
                {
                    await _vm.SaveProgressEarlyAsync();
                    _vm.Cleanup();
                    await Shell.Current.GoToAsync("..");
                }
            }
            else
            {
                await Shell.Current.GoToAsync("..");
            }
        }

        private void OnPlayAgainClicked(object sender, EventArgs e)
        {
            // Handled by viewmodel
        }

        private void OnOptionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset || e.Action == NotifyCollectionChangedAction.Add)
            {
                Dispatcher.Dispatch(BuildOptionsGrid);
            }
        }

        private void BuildOptionsGrid()
        {
            OptionsGrid.Children.Clear();
            _optionButtons.Clear();

            int row = 0;
            int col = 0;

            foreach (var option in _vm.AnswerOptions)
            {
                var button = new Button
                {
                    Text = option.ToString(),
                    BackgroundColor = Color.FromArgb("#22FFFFFF"),
                    TextColor = Colors.White,
                    FontSize = 22,
                    FontAttributes = FontAttributes.Bold,
                    CornerRadius = 12,
                    BorderColor = Color.FromArgb("#33FFFFFF"),
                    BorderWidth = 2,
                    Padding = new Thickness(0, 12)
                };

                button.Clicked += (s, e) =>
                {
                    _vm.AnswerCommand.Execute(option);
                };

                Grid.SetRow(button, row);
                Grid.SetColumn(button, col);
                OptionsGrid.Children.Add(button);
                _optionButtons.Add(button);

                col++;
                if (col > 1)
                {
                    col = 0;
                    row++;
                }
            }
        }

        private void OnRevealAnswer(object? sender, MathAnswerResult e)
        {
            // Highlight buttons
            foreach (var button in _optionButtons)
            {
                if (button.Text == e.CorrectAnswer.ToString())
                {
                    button.BackgroundColor = Color.FromArgb("#3306D6A0");
                    button.BorderColor = Color.FromArgb("#06D6A0");
                }
                else if (button.Text == e.Chosen.ToString() && !e.IsCorrect)
                {
                    button.BackgroundColor = Color.FromArgb("#33FF6B6B");
                    button.BorderColor = Color.FromArgb("#FF6B6B");
                    
                    try 
                    {
                        if (button.IsLoaded) 
                        {
                            button.TranslateToAsync(-5, 0, 50).ContinueWith(async t =>
                            {
                                await button.TranslateToAsync(5, 0, 50);
                                await button.TranslateToAsync(0, 0, 50);
                            });
                        }
                    } catch { }
                }
            }

            // Animate XP feedback
            try 
            {
                if (!FeedbackFlash.IsLoaded) return;

                FeedbackFlash.TranslationY = 0;
                
                if (e.IsCorrect)
                {
                    FeedbackFlash.Text = $"+{e.PointsGained} ⚡";
                    FeedbackFlash.TextColor = Color.FromArgb("#06D6A0");
                }
                else
                {
                    FeedbackFlash.Text = "✗";
                    FeedbackFlash.TextColor = Color.FromArgb("#FF6B6B");
                }

                _ = FeedbackFlash.FadeToAsync(1, 100);
                _ = FeedbackFlash.TranslateToAsync(0, -60, 600, Easing.CubicOut).ContinueWith(async _ => 
                {
                    await FeedbackFlash.FadeToAsync(0, 200);
                });
            } catch { }
        }
    }
}
