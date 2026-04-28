using System.Windows.Input;

namespace AcadsJulie.Views;

public partial class GameModeSelectionPage : ContentPage
{
    private readonly string _route;

    public GameModeSelectionPage(string route)
    {
        InitializeComponent();
        _route = route;

        BindingContext = this;
    }

    public ICommand SelectModeCommand => new Command<string>(async (mode) =>
    {
        await Navigation.PopModalAsync();

        var diff = App.ProfileService.GetProfile().DifficultyPreference;
        await Shell.Current.GoToAsync($"{_route}?difficulty={diff}&mode={mode}");
    });

    private async void OnCloseTapped(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
