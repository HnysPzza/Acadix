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

        if (exception is HttpRequestException or TaskCanceledException or TimeoutException)
            return "Can't reach the server. Check your connection and try again.";

        // FirebaseAuthService/RankingService already translate backend errors into friendly
        // text, so anything reaching here with a message is safe to show. Unknown exception
        // types (null refs, parse errors) fall back to a generic message rather than exposing
        // a stack-trace-flavoured string.
        return string.IsNullOrWhiteSpace(message) || exception is not InvalidOperationException
            ? "Something went wrong. Please try again."
            : message;
    }
}
