using AcadsJulie.Configuration;
using AcadsJulie.Services;

namespace AcadsJulie.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
        ConfigureGoogleButton();
    }

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Enter your email and password.");
            return;
        }

        await RunBusyAsync(async () =>
        {
            await App.AuthService.LoginAsync(email, password);
            await App.RankingService.EnsureInitialSyncAsync();
            App.NavigateToPostAuthStart();
        });
    }

    private void OnRegisterClicked(object? sender, EventArgs e)
    {
        Application.Current!.Windows[0].Page = new RegisterPage();
    }

    private void OnTogglePasswordClicked(object? sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        TogglePasswordButton.Text = PasswordEntry.IsPassword ? "👁" : "👁‍🗨️";
        TogglePasswordButton.Opacity = PasswordEntry.IsPassword ? 0.6 : 1.0;
    }

    private async void OnGoogleClicked(object? sender, EventArgs e)
    {
        if (!FirebaseSettings.IsGoogleConfigured)
        {
            ShowError(FirebaseSettings.GoogleMissingConfigurationMessage);
            return;
        }

        await RunBusyAsync(async () =>
        {
            var googleIdToken = await App.GoogleOAuthService.GetIdTokenAsync();
            await App.AuthService.LoginWithGoogleAsync(googleIdToken);
            await App.RankingService.EnsureInitialSyncAsync();
            App.NavigateToPostAuthStart();
        }, "Opening Google...");
    }

    private async Task RunBusyAsync(Func<Task> action, string busyText = "Logging in...")
    {
        ErrorBanner.IsVisible = false;
        ErrorLabel.IsVisible = false;
        LoginButton.IsEnabled = false;
        GoogleButton.IsEnabled = false;
        LoginButton.Text = busyText;

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            ShowError(AuthUiMessageMapper.ToUserMessage(ex));
        }
        finally
        {
            LoginButton.IsEnabled = true;
            GoogleButton.IsEnabled = true;
            LoginButton.Text = "Log in";
        }
    }

    private void ShowError(string message)
    {
        ErrorLabel.Text = message;
        ErrorBanner.IsVisible = true;
        ErrorLabel.IsVisible = true;
    }

    private void ConfigureGoogleButton()
    {
        GoogleButton.IsEnabled = FirebaseSettings.IsGoogleConfigured;
        GoogleButton.Opacity = FirebaseSettings.IsGoogleConfigured ? 1 : 0.62;
        GoogleButton.Text = FirebaseSettings.IsGoogleConfigured
            ? "Continue with Google"
            : "Google sign-in unavailable";
    }
}
