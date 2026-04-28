namespace AcadsJulie.Views;

public partial class AuthGatePage : ContentPage
{
    public AuthGatePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        try
        {
            var session = await App.AuthService.RestoreSessionAsync();
            if (session == null)
            {
                App.NavigateToLogin();
                return;
            }

            await App.RankingService.EnsureInitialSyncAsync();
            App.NavigateToPostAuthStart();
        }
        catch
        {
            await App.AuthService.LogoutAsync();
            App.NavigateToLogin();
        }
    }
}
