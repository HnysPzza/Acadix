using AcadsJulie.ViewModels;

namespace AcadsJulie.Views.Games;

public partial class ColorTapPage : ContentPage
{
    private readonly ColorTapViewModel _vm;
    public ColorTapPage()
    {
        InitializeComponent();
        _vm = new ColorTapViewModel();
        BindingContext = _vm;
        Loaded += (s, e) => _vm.InitGame();
    }

    protected override void OnDisappearing() 
    { 
        base.OnDisappearing(); 
        _vm.Cleanup(); 
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        if (!_vm.IsGameOver)
        {
            var quit = await DisplayAlertAsync("Quit game?", "Progress will be saved.", "Quit", "Cancel");
            if (!quit) return;
            await _vm.SaveProgressEarlyAsync();
            _vm.Cleanup();
        }
        await Shell.Current.GoToAsync("..");
    }

    private void OnPlayAgainClicked(object sender, EventArgs e)
    {
        // Command binding handles restart; nothing extra needed
    }

    protected override bool OnBackButtonPressed()
    {
        if (!_vm.IsGameOver)
        {
            Dispatcher.Dispatch(async () =>
            {
                var quit = await DisplayAlertAsync("Quit game?", "Progress will be saved.", "Quit", "Cancel");
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
}

