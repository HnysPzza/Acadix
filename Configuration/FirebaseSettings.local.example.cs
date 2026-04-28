using AcadsJulie.Configuration;

// Copy this file to FirebaseSettings.local.cs and fill in real values.
// FirebaseSettings.local.cs is ignored by git and is used for Android builds
// where Windows environment variables are not available at app runtime.
namespace AcadsJulie.Configuration;

public static partial class FirebaseSettings
{
    static partial void ConfigureLocal(IDictionary<string, string> values)
    {
        values[FirebaseProjectIdEnvironmentKey] = "your-firebase-project-id";
        values[FirebaseWebApiKeyEnvironmentKey] = "your-firebase-web-api-key";
        values[FirestoreDatabaseIdEnvironmentKey] = "(default)";
        values[GoogleOAuthClientIdEnvironmentKey] = "your-google-oauth-client-id.apps.googleusercontent.com";
    }
}
