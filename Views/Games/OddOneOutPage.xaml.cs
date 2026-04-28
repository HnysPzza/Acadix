using AcadsJulie.ViewModels;
using System.Collections.Specialized;

namespace AcadsJulie.Views.Games
{
    public partial class OddOneOutPage : ContentPage
    {
        private readonly OddOneOutViewModel _vm;
        private readonly List<Button> _optionButtons = new();

        public OddOneOutPage()
        {
            InitializeComponent();
            _vm = new OddOneOutViewModel();
            BindingContext = _vm;

            _vm.ShuffledOptions.CollectionChanged += OnOptionsChanged;
            _vm.OnRevealAnswer += OnRevealAnswer;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _vm.InitGame();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _vm.Cleanup();
        }

        protected override bool OnBackButtonPressed()
        {
            if (!_vm.IsGameOver)
            {
                Dispatcher.Dispatch(async () =>
                {
                    var quit = await DisplayAlertAsync("Quit game?", "Progress will be lost", "Quit", "Cancel");
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

        private async void OnBackClicked(object sender, EventArgs e)
        {
            if (!_vm.IsGameOver)
            {
                var quit = await DisplayAlertAsync("Quit game?", "Progress will be lost", "Quit", "Cancel");
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

            foreach (var option in _vm.ShuffledOptions)
            {
                var button = new Button
                {
                    Text = option,
                    BackgroundColor = Color.FromArgb("#22FFFFFF"),
                    TextColor = Colors.White,
                    FontSize = 16,
                    FontAttributes = FontAttributes.Bold,
                    CornerRadius = 12,
                    BorderColor = Color.FromArgb("#33FFFFFF"),
                    BorderWidth = 2,
                    Padding = new Thickness(0, 16)
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

        private void OnRevealAnswer(object? sender, FocusAnswerResult e)
        {
            foreach (var button in _optionButtons)
            {
                if (button.Text == e.CorrectOption)
                {
                    button.BackgroundColor = Color.FromArgb("#3306D6A0");
                    button.BorderColor = Color.FromArgb("#06D6A0");
                }
                else if (button.Text == e.SelectedOption && !e.IsCorrect)
                {
                    button.BackgroundColor = Color.FromArgb("#33FF6B6B");
                    button.BorderColor = Color.FromArgb("#FF6B6B");
                    
                    // Quick shake animation
                    button.TranslateToAsync(-5, 0, 50).ContinueWith(async t =>
                    {
                        await button.TranslateToAsync(5, 0, 50);
                        await button.TranslateToAsync(0, 0, 50);
                    });
                }
            }
        }
    }
}
