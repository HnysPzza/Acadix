namespace AcadsJulie.Services;

/// <summary>
/// Tracks which Firebase account owns the data currently sitting in local storage.
///
/// Every gameplay/academic value the app saves goes through <see cref="ScopedPreferences"/>,
/// which prefixes the storage key with the signed-in user's UID. Without this, logging out
/// and logging in as somebody else on the same device would hand the second person the
/// first person's XP, streak, brain scores and — worse — their academic task notes.
///
/// Auth tokens are NOT scoped: they live in SecureStorage and are cleared on logout.
/// </summary>
public static class UserScope
{
    private const string KeyPrefix = "u_";
    private const string LegacyMigrationFlagKey = "legacy_data_migrated";

    /// <summary>Keys holding string (usually JSON) values.</summary>
    private static readonly string[] ScopedStringKeys =
    [
        "user_profile",
        "game_sessions",
        "academic_tasks",
        "AcadsJulie_DailyQuests",
        "user_goals",
        "user_habits",
        "course_progress",
        "enrolled_courses",
        "content_bookmarks",
        "daily_challenge",
        "challenge_history",
        "quick_note_keys"
    ];

    /// <summary>Keys holding integer values.</summary>
    private static readonly string[] ScopedIntKeys =
    [
        "LastSeenLevel"
    ];

    private static string _uid = string.Empty;

    /// <summary>
    /// Raised when the signed-in account changes. <see cref="App"/> uses this to rebuild the
    /// service singletons so no in-memory cache survives an account switch.
    /// </summary>
    public static event Action? Changed;

    public static string CurrentUid => _uid;

    public static bool HasUser => !string.IsNullOrWhiteSpace(_uid);

    /// <summary>
    /// Points local storage at <paramref name="uid"/>. Safe to call repeatedly with the same
    /// UID — <see cref="Changed"/> only fires when the account actually changes.
    /// </summary>
    public static void SetUser(string? uid)
    {
        var next = uid?.Trim() ?? string.Empty;
        if (next == _uid)
            return;

        _uid = next;

        if (HasUser)
            MigrateLegacyDataIfNeeded();

        Changed?.Invoke();
    }

    /// <summary>
    /// Detaches local storage from any account (used on logout). The data itself is kept so a
    /// returning user still finds their progress; it is simply no longer reachable.
    /// </summary>
    public static void ClearUser()
    {
        if (_uid.Length == 0)
            return;

        _uid = string.Empty;
        Changed?.Invoke();
    }

    /// <summary>Namespaces a base key against the current account.</summary>
    public static string Key(string baseKey) =>
        HasUser ? $"{KeyPrefix}{_uid}_{baseKey}" : baseKey;

    /// <summary>
    /// Deletes every scoped value belonging to the current account. Used by "Reset Progress",
    /// which previously called <c>Preferences.Clear()</c> and wiped other accounts' data
    /// (and the auth bookkeeping) along with it.
    /// </summary>
    public static void ClearCurrentUserData()
    {
        // Quick notes use generated key names, so read the index before deleting it.
        foreach (var noteKey in GetQuickNoteKeys())
            ScopedPreferences.Remove(noteKey);

        foreach (var key in ScopedStringKeys)
            ScopedPreferences.Remove(key);

        foreach (var key in ScopedIntKeys)
            ScopedPreferences.Remove(key);

        // Trivia "already seen" history uses one generated key per category+difficulty. The
        // provider owns that key format, so ask it rather than duplicating the naming here.
        TriviaQuestionProvider.ClearAllHistory();
    }

    private static IEnumerable<string> GetQuickNoteKeys()
    {
        var index = ScopedPreferences.Get("quick_note_keys", string.Empty) ?? string.Empty;
        return index.Length == 0
            ? []
            : index.Split(',', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Moves pre-scoping data into the first account that signs in after the update, so
    /// existing users keep their progress. Runs once per install; every later account starts
    /// with a clean slate.
    /// </summary>
    private static void MigrateLegacyDataIfNeeded()
    {
        if (Preferences.Default.Get(LegacyMigrationFlagKey, false))
            return;

        // Set the flag first: a crash mid-migration must not replay this for the next account.
        Preferences.Default.Set(LegacyMigrationFlagKey, true);

        var legacyNoteIndex = Preferences.Default.Get("quick_note_keys", string.Empty);
        var legacyNoteKeys = string.IsNullOrEmpty(legacyNoteIndex)
            ? []
            : legacyNoteIndex.Split(',', StringSplitOptions.RemoveEmptyEntries);

        foreach (var key in ScopedStringKeys.Concat(legacyNoteKeys))
            MoveLegacyString(key);

        foreach (var key in ScopedIntKeys)
            MoveLegacyInt(key);
    }

    private static void MoveLegacyString(string baseKey)
    {
        var value = Preferences.Default.Get(baseKey, (string?)null);
        if (value is null)
            return;

        Preferences.Default.Set(Key(baseKey), value);
        Preferences.Default.Remove(baseKey);
    }

    private static void MoveLegacyInt(string baseKey)
    {
        if (!Preferences.Default.ContainsKey(baseKey))
            return;

        Preferences.Default.Set(Key(baseKey), Preferences.Default.Get(baseKey, 0));
        Preferences.Default.Remove(baseKey);
    }
}

/// <summary>
/// Drop-in replacement for <c>Preferences</c> that keys everything against the signed-in
/// account. Use this for anything user-owned; use <c>Preferences</c> directly only for
/// genuinely device-wide settings.
/// </summary>
public static class ScopedPreferences
{
    /// <summary>
    /// Reads a string value. The result is non-null whenever <paramref name="defaultValue"/> is,
    /// so callers passing "" can use the result directly.
    /// </summary>
    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(defaultValue))]
    public static string? Get(string key, string? defaultValue) =>
        Preferences.Default.Get(UserScope.Key(key), defaultValue);

    public static int Get(string key, int defaultValue) =>
        Preferences.Default.Get(UserScope.Key(key), defaultValue);

    public static bool Get(string key, bool defaultValue) =>
        Preferences.Default.Get(UserScope.Key(key), defaultValue);

    public static void Set(string key, string value) =>
        Preferences.Default.Set(UserScope.Key(key), value);

    public static void Set(string key, int value) =>
        Preferences.Default.Set(UserScope.Key(key), value);

    public static void Set(string key, bool value) =>
        Preferences.Default.Set(UserScope.Key(key), value);

    public static void Remove(string key) =>
        Preferences.Default.Remove(UserScope.Key(key));

    public static bool ContainsKey(string key) =>
        Preferences.Default.ContainsKey(UserScope.Key(key));
}
