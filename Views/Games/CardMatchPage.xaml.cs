using AcadsJulie.Models;
using AcadsJulie.ViewModels;
using Microsoft.Maui.Controls.Shapes;
using System.Collections.Specialized;
using System.ComponentModel;

namespace AcadsJulie.Views.Games
{
    public partial class CardMatchPage : ContentPage
    {
        private readonly CardMatchViewModel _vm;
        private readonly Dictionary<CardItem, Border> _cardViews = new();

        public CardMatchPage()
        {
            InitializeComponent();
            _vm = new CardMatchViewModel();
            BindingContext = _vm;

            _vm.Cards.CollectionChanged += OnCardsChanged;
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
            // Reset logic triggered by command
        }

        private void OnCardsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset || e.Action == NotifyCollectionChangedAction.Add)
            {
                Dispatcher.Dispatch(BuildGrid);
            }
        }

        private void BuildGrid()
        {
            CardGrid.Children.Clear();
            _cardViews.Clear();

            int columns = 4;
            int count = _vm.Cards.Count;
            int row = 0;
            int col = 0;

            foreach (var card in _vm.Cards)
            {
                var border = new Border
                {
                    Style = (Style)Resources["GlassCardStyle"],
                    WidthRequest = 70,
                    HeightRequest = 90,
                    BackgroundColor = Color.FromArgb("#22FFFFFF")
                };

                var label = new Label
                {
                    Text = "?",
                    FontSize = 32,
                    TextColor = Color.FromArgb("#88FFFFFF"),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };

                border.Content = label;

                card.PropertyChanged += (s, e) => OnCardPropertyChanged(card, border, label, e.PropertyName!);

                var tap = new TapGestureRecognizer();
                tap.Tapped += (s, e) => _vm.FlipCardCommand.Execute(card);
                border.GestureRecognizers.Add(tap);

                Grid.SetRow(border, row);
                Grid.SetColumn(border, col);
                CardGrid.Children.Add(border);
                _cardViews[card] = border;

                col++;
                if (col >= columns)
                {
                    col = 0;
                    row++;
                }
            }
        }

        private async void OnCardPropertyChanged(CardItem card, Border border, Label label, string propertyName)
        {
            if (propertyName == nameof(card.IsFlipped))
            {
                if (card.IsFlipped)
                {
                    await border.RotateYToAsync(90, 150);
                    border.BackgroundColor = Color.FromArgb("#114361EE");
                    label.Text = card.Emoji;
                    label.TextColor = Colors.White;
                    await border.RotateYToAsync(0, 150);
                }
                else
                {
                    // If it was wrong pair, shake first then flip back
                    if (!card.IsMatched)
                    {
                        await Task.Delay(200); // Give player time to see it's wrong
                        await border.TranslateToAsync(-8, 0, 50);
                        await border.TranslateToAsync(8, 0, 50);
                        await border.TranslateToAsync(0, 0, 50);
                    }

                    await border.RotateYToAsync(90, 150);
                    border.BackgroundColor = Color.FromArgb("#22FFFFFF");
                    label.Text = "?";
                    label.TextColor = Color.FromArgb("#88FFFFFF");
                    await border.RotateYToAsync(0, 150);
                }
            }
            else if (propertyName == nameof(card.IsMatched))
            {
                if (card.IsMatched)
                {
                    // Green tint
                    border.BackgroundColor = Color.FromArgb("#3306D6A0");
                    border.Stroke = Color.FromArgb("#06D6A0");
                }
            }
        }
    }
}
