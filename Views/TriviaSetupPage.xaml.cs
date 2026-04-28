using AcadsJulie.ViewModels;

namespace AcadsJulie.Views;

public partial class TriviaSetupPage : ContentPage
{
    public TriviaSetupPage()
    {
        InitializeComponent();
        BindingContext = new TriviaSetupViewModel();
    }

    private async void OnStartQuizTapped(object? sender, EventArgs e)
    {
        await StartQuizCta.ScaleToAsync(0.97, 80, Easing.CubicOut);
        await StartQuizCta.ScaleToAsync(1, 80, Easing.CubicIn);
        if (BindingContext is TriviaSetupViewModel vm)
        {
            await vm.StartQuizCommand.ExecuteAsync(null);
        }
    }
}
