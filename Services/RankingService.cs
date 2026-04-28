using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AcadsJulie.Configuration;
using AcadsJulie.Models;

namespace AcadsJulie.Services;

public class RankingService
{
    private const string InitialSyncPrefix = "leaderboard_initial_sync_";
    private static readonly HttpClient Http = new();

    private readonly FirebaseAuthService _authService;
    private readonly ProfileService _profileService;
    private readonly ProgressService _progressService;

    public RankingService(FirebaseAuthService authService, ProfileService profileService, ProgressService progressService)
    {
        _authService = authService;
        _profileService = profileService;
        _progressService = progressService;
    }

    public async Task EnsureInitialSyncAsync()
    {
        var uid = _authService.CurrentSession?.Uid;
        if (string.IsNullOrWhiteSpace(uid))
            return;

        var key = InitialSyncPrefix + uid;
        if (Preferences.Get(key, false))
            return;

        await SyncCurrentUserAsync();
        Preferences.Set(key, true);
    }

    public async Task SyncCurrentUserAsync()
    {
        EnsureConfigured();

        if (!_authService.IsSignedIn || _authService.CurrentSession == null)
            return;

        var profile = _profileService.GetProfile();
        var sessions = _progressService.GetSessions();
        var session = _authService.CurrentSession;

        var fields = new Dictionary<string, object>
        {
            ["uid"] = StringField(session.Uid),
            ["displayName"] = StringField(profile.Name),
            ["email"] = StringField(session.Email),
            ["brainScore"] = IntegerField(profile.BrainScore),
            ["level"] = IntegerField(profile.Level),
            ["xp"] = IntegerField(profile.XP),
            ["streakDays"] = IntegerField(profile.StreakDays),
            ["gamesPlayed"] = IntegerField(sessions.Count),
            ["triviaBestScore"] = IntegerField(profile.TriviaBestScore),
            ["updatedAt"] = TimestampField(DateTimeOffset.UtcNow)
        };

        var url = $"{DocumentsBaseUrl()}/leaderboards/global/users/{Uri.EscapeDataString(session.Uid)}";
        using var request = new HttpRequestMessage(HttpMethod.Patch, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _authService.GetValidIdTokenAsync());
        request.Content = JsonContent(new { fields });

        using var response = await Http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await ReadFirestoreErrorAsync(response));
    }

    public async Task<List<RankingEntry>> GetTopGlobalAsync(int limit = 50)
    {
        EnsureConfigured();

        var url = $"{DocumentsBaseUrl()}/leaderboards/global/users?pageSize={limit}&orderBy={Uri.EscapeDataString("brainScore desc")}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _authService.GetValidIdTokenAsync());

        using var response = await Http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await ReadFirestoreErrorAsync(response));

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("documents", out var documents))
            return [];

        var currentUid = _authService.CurrentSession?.Uid ?? string.Empty;
        var entries = new List<RankingEntry>();
        var rank = 1;

        foreach (var document in documents.EnumerateArray())
        {
            if (!document.TryGetProperty("fields", out var fields))
                continue;

            var uid = GetString(fields, "uid");
            entries.Add(new RankingEntry
            {
                Rank = rank++,
                Uid = uid,
                DisplayName = GetString(fields, "displayName", "Student"),
                Email = GetString(fields, "email"),
                BrainScore = GetInt(fields, "brainScore"),
                Level = GetInt(fields, "level"),
                XP = GetInt(fields, "xp"),
                StreakDays = GetInt(fields, "streakDays"),
                GamesPlayed = GetInt(fields, "gamesPlayed"),
                TriviaBestScore = GetInt(fields, "triviaBestScore"),
                UpdatedAt = GetTimestamp(fields, "updatedAt"),
                IsCurrentUser = uid == currentUid
            });
        }

        return entries;
    }

    private static void EnsureConfigured()
    {
        if (!FirebaseSettings.IsFirebaseConfigured)
            throw new InvalidOperationException("Firebase is not configured.");
    }

    private static object StringField(string value) => new { stringValue = value };
    private static object IntegerField(int value) => new { integerValue = value.ToString() };
    private static object TimestampField(DateTimeOffset value) => new { timestampValue = value.UtcDateTime.ToString("O") };

    private static StringContent JsonContent(object value) =>
        new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

    private static string DocumentsBaseUrl() =>
        $"https://firestore.googleapis.com/v1/projects/{Uri.EscapeDataString(FirebaseSettings.FirebaseProjectId)}/databases/{Uri.EscapeDataString(FirebaseSettings.FirestoreDatabaseId)}/documents";

    private static string GetString(JsonElement fields, string name, string fallback = "")
    {
        if (fields.TryGetProperty(name, out var field) &&
            field.TryGetProperty("stringValue", out var value))
            return value.GetString() ?? fallback;

        return fallback;
    }

    private static int GetInt(JsonElement fields, string name)
    {
        if (!fields.TryGetProperty(name, out var field))
            return 0;

        if (field.TryGetProperty("integerValue", out var intValue) &&
            int.TryParse(intValue.GetString(), out var parsedInt))
            return parsedInt;

        if (field.TryGetProperty("doubleValue", out var doubleValue))
            return (int)Math.Round(doubleValue.GetDouble());

        return 0;
    }

    private static DateTimeOffset GetTimestamp(JsonElement fields, string name)
    {
        if (fields.TryGetProperty(name, out var field) &&
            field.TryGetProperty("timestampValue", out var value) &&
            DateTimeOffset.TryParse(value.GetString(), out var timestamp))
            return timestamp;

        return DateTimeOffset.MinValue;
    }

    private static async Task<string> ReadFirestoreErrorAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        try
        {
            using var doc = JsonDocument.Parse(json);
            var message = doc.RootElement.GetProperty("error").GetProperty("message").GetString();
            return string.IsNullOrWhiteSpace(message) ? "Firebase ranking request failed." : message;
        }
        catch
        {
            return "Firebase ranking request failed.";
        }
    }
}
