namespace AcadsJulie.Views;

public partial class AuthGatePage : ContentPage
{
    public AuthGatePage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// True when an exception represents "the network is unavailable" rather than
    /// "this account is no longer valid". Only the latter should force a sign-out.
    /// </summary>
    private static bool IsTransientNetworkFailure(Exception ex) =>
        ex is HttpRequestException
           or TaskCanceledException
           or TimeoutException
        || ex.InnerException is HttpRequestException
           or TaskCanceledException
           or TimeoutException;

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

            // Leaderboard sync is optional at startup — a failure here must not gate entry
            // into the app, so it is deliberately swallowed.
            try
            {
                await App.RankingService.EnsureInitialSyncAsync();
            }
            catch
            {
                // Offline or Firestore unreachable: sync will retry on the next save.
            }

            App.NavigateToPostAuthStart();
        }
        catch (Exception ex) when (IsTransientNetworkFailure(ex))
        {
            // A dropped connection is not a sign-out. Keep the stored session and let the user
            // continue offline; the token refreshes on the next successful call.
            App.NavigateToPostAuthStart();
        }
        catch
        {
            // Genuine auth failure (refresh token rejected/revoked): sign out for real.
            await App.AuthService.LogoutAsync();
            App.NavigateToLogin();
        }
    }
}
