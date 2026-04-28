using AcadsJulie.Models;
using AcadsJulie.Services;
using AcadsJulie.ViewModels;
using Microsoft.Maui.Media;
using SkiaSharp.Extended.UI.Controls;
using System.ComponentModel;
using System.Collections.Specialized;

namespace AcadsJulie.Views.Games;

public partial class TriviaGamePage : ContentPage
{
    private readonly TriviaViewModel _vm;
    private readonly List<Button> _optionButtons = [];
    private bool _isShowingGameOverOverlay;
    private CancellationTokenSource? _speechCts;

    public TriviaGamePage()
    {
        InitializeComponent();
        _vm = new TriviaViewModel();
        BindingContext = _vm;
        _vm.ShuffledOptions.CollectionChanged += OnOptionsChanged;
        _vm.OnRevealAnswer += OnRevealAnswer;
        _vm.PropertyChanged += OnViewModelPropertyChanged;
        _vm.GameSessionInitialized += OnGameSessionInitialized;
    }

    private void OnGameSessionInitialized()
    {
        // InitGame runs on UI thread; set Lottie source before entry animation starts.
        SetParrotMood("idle");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Defer so Shell can finish applying query parameters to TriviaViewModel (Field, SubField, Difficulty).
        Dispatcher.Dispatch(StartGameSession);
    }

    private void StartGameSession()
    {
        ApplyTheme();
        _vm.InitGame();
        // Parrot: OnGameSessionInitialized -> SetParrotMood after InitGame (also covers Play Again).
        _ = RunParrotHostEntryAnimationAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopQuestionSpeech();
        _vm.Cleanup();
    }

    private void ApplyTheme()
    {
        var theme = TriviaThemeService.GetTheme(_vm.Field, _vm.SubField);
        var brush = (LinearGradientBrush)BackgroundGradient.Background;
        var stops = brush.GradientStops;
        if (stops.Count >= 2)
        {
            stops[0].Color = Color.FromArgb(theme.PrimaryColor);
            stops[1].Color = Color.FromArgb(theme.SecondaryColor);
        }
        ProgressBarControl.ProgressColor = Color.FromArgb(theme.AccentColor);
        TitleLabel.Text = $"📚 {theme.DisplayName ?? _vm.Field}";
    }

    protected override bool OnBackButtonPressed()
    {
        if (!_vm.IsGameOver)
        {
            Dispatcher.Dispatch(async () =>
            {
                var quit = await DisplayAlert("Quit game?", "Progress will be lost", "Quit", "Cancel");
                if (quit)
                {
                    _vm.Cleanup();
                    await Shell.Current.GoToAsync("..");
                }
            });
            return true;
        }
        return base.OnBackButtonPressed();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        if (!_vm.IsGameOver)
        {
            var quit = await DisplayAlert("Quit game?", "Progress will be lost", "Quit", "Cancel");
            if (quit)
            {
                _vm.Cleanup();
                await Shell.Current.GoToAsync("..");
            }
        }
        else
        {
            await Shell.Current.GoToAsync("..");
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
        int row = 0, col = 0;
        var theme = TriviaThemeService.GetTheme(_vm.Field, _vm.SubField);

        foreach (var option in _vm.ShuffledOptions)
        {
            var button = new Button
            {
                Text = option,
                BackgroundColor = Color.FromArgb("#66FFFFFF"),
                TextColor = Colors.White,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 12,
                BorderColor = Color.FromArgb("#AAFFFFFF"),
                BorderWidth = 2,
                Padding = new Thickness(0, 16)
            };
            button.Clicked += (s, e) =>
            {
                if (!string.IsNullOrEmpty(_vm.SelectedOption)) return;
                ApplySelectedOptionStyle(button);
                SetOptionsEnabled(false);
                _vm.AnswerCommand.Execute(option);
            };
            Grid.SetRow(button, row);
            Grid.SetColumn(button, col);
            OptionsGrid.Children.Add(button);
            _optionButtons.Add(button);
            button.Opacity = 0;
            _ = button.FadeToAsync(1, 180 + (uint)(col * 70), Easing.CubicOut);
            col++;
            if (col > 1) { col = 0; row++; }
        }
    }

    private async void OnRevealAnswer(object? sender, TriviaAnswerResult e)
    {
        StopQuestionSpeech();

        var theme = TriviaThemeService.GetTheme(_vm.Field, _vm.SubField);
        var correctColor = Color.FromArgb(theme.AccentColor);

        foreach (var button in _optionButtons)
        {
            button.IsEnabled = false;
            if (button.Text == e.CorrectOption)
            {
                button.BackgroundColor = e.IsCorrect ? Color.FromArgb("#2E16A34A") : Color.FromArgb("#3306D6A0");
                button.BorderColor = correctColor;
            }
            else if (button.Text == e.SelectedOption && !e.IsCorrect)
            {
                button.BackgroundColor = Color.FromArgb("#33FF6B6B");
                button.BorderColor = Color.FromArgb("#FF6B6B");
                _ = button.TranslateToAsync(-5, 0, 50);
                await button.TranslateToAsync(5, 0, 50);
                await button.TranslateToAsync(0, 0, 50);
            }
        }

        if (e.IsCorrect)
        {
            SetParrotMood("happy");
            await PulseSpeechBubbleStrokeAsync("#78C800");
            await JumpParrotAsync();
        }
        else
        {
            SetParrotMood("sad");
            await ShakeSpeechBubbleAsync();
        }

        FeedbackFlash.IsVisible = true;
        FeedbackFlash.BackgroundColor = e.IsCorrect ? Color.FromArgb("#3306D6A0") : Color.FromArgb("#33FF6B6B");
        await FeedbackFlash.FadeToAsync(0.8, 100);
        await FeedbackFlash.FadeToAsync(0, 200);
        FeedbackFlash.IsVisible = false;

        if (!_vm.IsGameOver)
        {
            // Return to idle between rounds after feedback completes.
            SetParrotMood("idle");
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TriviaViewModel.IsGameOver) && _vm.IsGameOver)
        {
            Dispatcher.Dispatch(async () => await ShowGameOverOverlayAsync());
            return;
        }

        if (e.PropertyName == nameof(TriviaViewModel.CurrentQuestion))
        {
            _isShowingGameOverOverlay = false;
            GameOverOverlay.Opacity = 0;
            Dispatcher.Dispatch(async () => await SpeakCurrentQuestionAsync());
        }
    }

    private async Task SpeakCurrentQuestionAsync()
    {
        if (_vm.HasNoQuestions || _vm.IsGameOver || string.IsNullOrWhiteSpace(_vm.CurrentQuestionText))
        {
            return;
        }

        StopQuestionSpeech();
        var speechCts = new CancellationTokenSource();
        _speechCts = speechCts;

        try
        {
            await TextToSpeech.Default.SpeakAsync(
                _vm.CurrentQuestionText,
                new SpeechOptions { Pitch = 1.0f, Volume = 1.0f },
                speechCts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            // TTS availability can vary by platform/device; the quiz should continue without speech.
        }
        finally
        {
            if (ReferenceEquals(_speechCts, speechCts))
            {
                _speechCts = null;
            }

            speechCts.Dispose();
        }
    }

    private void StopQuestionSpeech()
    {
        var speechCts = _speechCts;
        _speechCts = null;

        if (speechCts != null)
        {
            speechCts.Cancel();
        }

    }

    private async Task RunParrotHostEntryAnimationAsync()
    {
        var activeParrot = GetActiveParrot();
        activeParrot.Opacity = 0;
        SpeechBubbleBorder.Opacity = 0;
        activeParrot.TranslationY = 18;
        SpeechBubbleBorder.TranslationY = 18;

        var fadeParrot = activeParrot.FadeToAsync(1, 400, Easing.CubicOut);
        var fadeBubble = SpeechBubbleBorder.FadeToAsync(1, 400, Easing.CubicOut);
        var moveParrot = activeParrot.TranslateToAsync(0, 0, 400, Easing.CubicOut);
        var moveBubble = SpeechBubbleBorder.TranslateToAsync(0, 0, 400, Easing.CubicOut);
        await Task.WhenAll(fadeParrot, fadeBubble, moveParrot, moveBubble);
    }

    private async Task PulseSpeechBubbleStrokeAsync(string colorHex)
    {
        var originalStroke = SpeechBubbleBorder.Stroke;
        SpeechBubbleBorder.Stroke = Color.FromArgb(colorHex);
        await SpeechBubbleBorder.ScaleToAsync(1.03, 110, Easing.CubicOut);
        await SpeechBubbleBorder.ScaleToAsync(1.0, 110, Easing.CubicIn);
        SpeechBubbleBorder.Stroke = originalStroke;
    }

    private async Task JumpParrotAsync()
    {
        var activeParrot = GetActiveParrot();
        await activeParrot.TranslateToAsync(0, -10, 120, Easing.CubicOut);
        await activeParrot.TranslateToAsync(0, 0, 120, Easing.CubicIn);
    }

    private async Task ShakeSpeechBubbleAsync()
    {
        var red = Color.FromArgb("#FF4B4B");
        var originalStroke = SpeechBubbleBorder.Stroke;
        SpeechBubbleBorder.Stroke = red;
        SpeechBubbleTail.Stroke = red;
        await SpeechBubbleBorder.TranslateToAsync(-10, 0, 70, Easing.CubicInOut);
        await SpeechBubbleBorder.TranslateToAsync(10, 0, 70, Easing.CubicInOut);
        await SpeechBubbleBorder.TranslateToAsync(-10, 0, 70, Easing.CubicInOut);
        await SpeechBubbleBorder.TranslateToAsync(0, 0, 70, Easing.CubicOut);
        SpeechBubbleBorder.Stroke = originalStroke;
        SpeechBubbleTail.Stroke = Color.FromArgb("#E5E5E5");
    }

    private void SetParrotMood(string mood)
    {
        var resolved = mood == "idle"
            ? "parrot_idle.json"
            : ParrotOutfitResolver.ResolveWithFallback(_vm.Field, _vm.SubField, mood);

        _vm.ParrotAssetSource = resolved;
        // SKLottieView.Source is SKLottieImageSource; string binding can fail on some targets — set explicitly.
        ParrotHostLottie.Source = new SKFileLottieImageSource { File = resolved };
        ParrotHostLottie.IsAnimationEnabled = true;
        ParrotHostLottie.IsVisible = true;
        if (ParrotHostLottie.Opacity < 1)
            ParrotHostLottie.Opacity = 1;
    }

    private void ApplySelectedOptionStyle(Button button)
    {
        button.BackgroundColor = Color.FromArgb("#88FFFFFF");
        button.BorderColor = Color.FromArgb("#FFFFFFFF");
    }

    private void SetOptionsEnabled(bool isEnabled)
    {
        foreach (var button in _optionButtons)
        {
            button.IsEnabled = isEnabled;
        }
    }

    private async Task ShowGameOverOverlayAsync()
    {
        if (_isShowingGameOverOverlay) return;
        _isShowingGameOverOverlay = true;

        GameOverOverlay.IsVisible = true;
        GameOverOverlay.Opacity = 0;
        GameOverCard.Scale = 0.8;
        await GameOverOverlay.FadeToAsync(1, 150);
        await GameOverCard.ScaleToAsync(1.05, 250, Easing.SpringOut);
        await GameOverCard.ScaleToAsync(1.0, 150, Easing.CubicOut);
    }

    private View GetActiveParrot()
    {
        return ParrotHostLottie;
    }
}
