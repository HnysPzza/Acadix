using AcadsJulie.ViewModels;
using System.Collections.Specialized;

namespace AcadsJulie.Views.Games
{
    public partial class SequenceGamePage : ContentPage
    {
        private readonly SequenceGameViewModel _vm;
        private readonly List<Button> _optionButtons = new();
        private Border? _blankBox;
        private Label? _blankLabel;
        private bool _isPulsing;

        private bool _isFirstLoad = true;

        public SequenceGamePage()
        {
            InitializeComponent();
            _vm = new SequenceGameViewModel();
            BindingContext = _vm;

            _vm.ShuffledOptions.CollectionChanged += OnOptionsChanged;
            _vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SequenceGameViewModel.CurrentQuestion))
                {
                    // Dispatch-defer to avoid JavaProxyThrowable
                    Dispatcher.Dispatch(BuildSequenceRow);
                }
            };
            _vm.OnRevealAnswer += OnRevealAnswer;

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
            _isPulsing = false;
            _vm.Cleanup();
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
                        _isPulsing = false;
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
                    _isPulsing = false;
                    await Shell.Current.GoToAsync("..");
                }
            }
            else
            {
                _isPulsing = false;
                await Shell.Current.GoToAsync("..");
            }
        }

        private void OnPlayAgainClicked(object sender, EventArgs e)
        {
            // Handled by viewmodel command binding
        }

        private void BuildSequenceRow()
        {
            SequenceStack.Children.Clear();
            _blankBox = null;
            _blankLabel = null;
            _isPulsing = false;

            if (_vm.CurrentQuestion == null) return;

            var seq = _vm.CurrentQuestion.Sequence;
            
            for (int i = 0; i < seq.Count; i++)
            {
                var item = seq[i];

                if (item.HasValue)
                {
                    var border = new Border
                    {
                        BackgroundColor = Color.FromArgb("#224856D6"),
                        Stroke = GetColor("MobileIndigo", "#4856D6"),
                        StrokeThickness = 2,
                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(14) },
                        Padding = new Thickness(12, 10),
                        Content = new Label { Text = item.Value.ToString(), TextColor = Colors.White, FontSize = 18, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center }
                    };
                    SequenceStack.Children.Add(border);
                }
                else
                {
                    _blankLabel = new Label { Text = "?", TextColor = GetColor("MobileCoral", "#F9735B"), FontSize = 18, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center };
                    _blankBox = new Border
                    {
                        BackgroundColor = Color.FromArgb("#22F9735B"),
                        Stroke = GetColor("MobileCoral", "#F9735B"),
                        StrokeThickness = 3, // Thicker stroke instead of unsupported dash array
                        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(14) },
                        Padding = new Thickness(12, 10),
                        Content = _blankLabel
                    };
                    SequenceStack.Children.Add(_blankBox);
                    
                    if (_blankBox.IsLoaded) 
                        StartPulseAnimation();
                    else 
                        _blankBox.Loaded += (s, e) => StartPulseAnimation();
                }

                // Add arrow if not last
                if (i < seq.Count - 1)
                {
                    SequenceStack.Children.Add(new Label
                    {
                        Text = "→",
                        TextColor = Color.FromArgb("#88FFFFFF"),
                        FontSize = 18,
                        VerticalOptions = LayoutOptions.Center,
                        Margin = new Thickness(4, 0)
                    });
                }
            }
        }

        private async void StartPulseAnimation()
        {
            if (_isPulsing || _blankBox == null) return;
            _isPulsing = true;
            
            while (_isPulsing && _blankBox != null)
            {
                // Ensure the view is actually part of the window
                if (_blankBox.Window == null || !_blankBox.IsLoaded) break;

                try 
                {
                    await _blankBox.ScaleToAsync(1.08, 600, Easing.SinInOut);
                    await _blankBox.ScaleToAsync(1.0, 600, Easing.SinInOut);
                }
                catch 
                {
                    break;
                }
            }
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

            foreach (var option in _vm.ShuffledOptions)
            {
                var button = new Button
                {
                    Text = option.ToString(),
                    BackgroundColor = Color.FromArgb("#22FFFFFF"),
                    TextColor = Colors.White,
                    FontSize = 22,
                    FontAttributes = FontAttributes.Bold,
                    CornerRadius = 18,
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

        private async void OnRevealAnswer(object? sender, SequenceAnswerResult e)
        {
            // Stop pulsing blank box and show answer
            _isPulsing = false;
            if (_blankBox != null && _blankLabel != null)
            {
                _blankLabel.Text = e.CorrectOption.ToString();
                
                if (e.IsCorrect)
                {
                    _blankLabel.TextColor = Colors.White;
                    _blankBox.BackgroundColor = Color.FromArgb("#06D6A0");
                    _blankBox.Stroke = Color.FromArgb("#06D6A0");
                    // removed setting StrokeDashArray = null to avoid Android crash

                    // Animate XP Flash
                    try 
                    {
                        if (XpFlash.IsLoaded) 
                        {
                            XpFlash.TranslationY = 0;
                            _ = XpFlash.FadeToAsync(1, 100);
                            _ = XpFlash.TranslateToAsync(0, -50, 800, Easing.CubicOut).ContinueWith(async _ => 
                            {
                                await XpFlash.FadeToAsync(0, 200);
                            });
                        }
                    } catch { }
                }
                else
                {
                    _blankLabel.TextColor = Colors.White;
                    _blankBox.BackgroundColor = Color.FromArgb("#FF6B6B");
                    _blankBox.Stroke = Color.FromArgb("#FF6B6B");
                    // removed setting StrokeDashArray = null to avoid Android crash
                }
            }

            // Highlight options
            foreach (var button in _optionButtons)
            {
                if (button.Text == e.CorrectOption.ToString())
                {
                    button.BackgroundColor = Color.FromArgb("#3306D6A0");
                    button.BorderColor = Color.FromArgb("#06D6A0");
                }
                else if (button.Text == e.SelectedOption.ToString() && !e.IsCorrect)
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
        }

        private static Color GetColor(string key, string fallback)
        {
            return Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
                ? color
                : Color.FromArgb(fallback);
        }
    }
}
