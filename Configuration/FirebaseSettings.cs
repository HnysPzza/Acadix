namespace AcadsJulie.Configuration;

public static partial class FirebaseSettings
{
    public const string FirebaseProjectIdEnvironmentKey = "ACADIX_FIREBASE_PROJECT_ID";
    public const string FirebaseWebApiKeyEnvironmentKey = "ACADIX_FIREBASE_WEB_API_KEY";
    public const string FirestoreDatabaseIdEnvironmentKey = "ACADIX_FIRESTORE_DATABASE_ID";
    public const string GoogleOAuthClientIdEnvironmentKey = "ACADIX_GOOGLE_OAUTH_CLIENT_ID";

    public static string FirebaseProjectId => GetSetting(FirebaseProjectIdEnvironmentKey);
    public static string FirebaseWebApiKey => GetSetting(FirebaseWebApiKeyEnvironmentKey);
    public static string FirestoreDatabaseId => GetSetting(FirestoreDatabaseIdEnvironmentKey, "(default)");
    public static string GoogleOAuthClientId => GetSetting(GoogleOAuthClientIdEnvironmentKey);

    public static bool IsConfigured => IsFirebaseConfigured;

    public static bool IsFirebaseConfigured =>
        !string.IsNullOrWhiteSpace(FirebaseProjectId) &&
        !string.IsNullOrWhiteSpace(FirebaseWebApiKey);

    public static bool IsGoogleConfigured =>
        !string.IsNullOrWhiteSpace(GoogleOAuthClientId);

    public static string FirebaseMissingConfigurationMessage =>
        "Sign-in is not ready on this build. Check Firebase setup and rebuild.";

    public static string GoogleMissingConfigurationMessage =>
        "Google sign-in is not available on this build.";

    public static string MissingConfigurationDiagnostics
    {
        get
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(FirebaseProjectId))
                missing.Add(FirebaseProjectIdEnvironmentKey);
            if (string.IsNullOrWhiteSpace(FirebaseWebApiKey))
                missing.Add(FirebaseWebApiKeyEnvironmentKey);
            if (string.IsNullOrWhiteSpace(GoogleOAuthClientId))
                missing.Add(GoogleOAuthClientIdEnvironmentKey);

            return missing.Count == 0
                ? "Firebase configuration is complete."
                : "Missing Firebase configuration: " + string.Join(", ", missing);
        }
    }

    static partial void ConfigureLocal(IDictionary<string, string> values);

    private static string GetSetting(string key, string fallback = "")
    {
        var localValue = GetLocalSetting(key);
        if (!string.IsNullOrWhiteSpace(localValue))
            return Clean(localValue);

        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        return Clean(value);
    }

    // Built once. IsFirebaseConfigured is checked on every auth/Firestore call and each check
    // reads several properties, so rebuilding this dictionary per read was pure waste.
    private static readonly Lazy<IReadOnlyDictionary<string, string>> LocalValues =
        new(() =>
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ConfigureLocal(values);
            return values;
        });

    private static string GetLocalSetting(string key) =>
        LocalValues.Value.TryGetValue(key, out var value) ? value : string.Empty;

    private static string Clean(string value) => value.Trim().Trim('"');
}
