using AcadsJulie.Configuration;
using AcadsJulie.Services;

namespace AcadsJulie.Views;

public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
        InitializeComponent();
        ConfigureGoogleButton();
    }

    private async void OnRegisterClicked(object? sender, EventArgs e)
    {
        var name = NameEntry.Text?.Trim() ?? string.Empty;
        var email = EmailEntry.Text?.Trim() ?? string.Empty;
        var password = PasswordEntry.Text ?? string.Empty;
        var confirm = ConfirmPasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Enter your name, email, and password.");
            return;
        }

        if (password != confirm)
        {
            ShowError("Passwords do not match.");
            return;
        }

        await RunBusyAsync(async () =>
        {
            await App.AuthService.RegisterAsync(email, password);

            var profile = App.ProfileService.GetProfile();
            profile.Name = name;
            App.ProfileService.SaveProfile(profile);

            await App.RankingService.EnsureInitialSyncAsync();
            App.NavigateToPostAuthStart();
        });
    }

    private void OnLoginClicked(object? sender, EventArgs e)
    {
        App.NavigateToLogin();
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

            var profile = App.ProfileService.GetProfile();
            var name = NameEntry.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(name))
            {
                profile.Name = name;
                App.ProfileService.SaveProfile(profile);
            }

            await App.RankingService.EnsureInitialSyncAsync();
            App.NavigateToPostAuthStart();
        }, "Opening Google...");
    }

    private async Task RunBusyAsync(Func<Task> action, string busyText = "Creating...")
    {
        ErrorBanner.IsVisible = false;
        ErrorLabel.IsVisible = false;
        RegisterButton.IsEnabled = false;
        GoogleButton.IsEnabled = false;
        RegisterButton.Text = busyText;

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
            RegisterButton.IsEnabled = true;
            GoogleButton.IsEnabled = true;
            RegisterButton.Text = "Create account";
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
