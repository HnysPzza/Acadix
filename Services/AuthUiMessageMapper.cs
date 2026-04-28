using AcadsJulie.Configuration;

namespace AcadsJulie.Services;

public static class AuthUiMessageMapper
{
    public static string ToUserMessage(Exception exception)
    {
        var message = exception.Message;

        if (message.Contains("Firebase is not configured", StringComparison.OrdinalIgnoreCase) ||
            message.Contains(FirebaseSettings.FirebaseProjectIdEnvironmentKey, StringComparison.OrdinalIgnoreCase) ||
            message.Contains(FirebaseSettings.FirebaseWebApiKeyEnvironmentKey, StringComparison.OrdinalIgnoreCase))
        {
            return FirebaseSettings.FirebaseMissingConfigurationMessage;
        }

        if (message.Contains("Google sign-in is not configured", StringComparison.OrdinalIgnoreCase) ||
            message.Contains(FirebaseSettings.GoogleOAuthClientIdEnvironmentKey, StringComparison.OrdinalIgnoreCase))
        {
            return FirebaseSettings.GoogleMissingConfigurationMessage;
        }

        return string.IsNullOrWhiteSpace(message)
            ? "Something went wrong. Please try again."
            : message;
    }
}
