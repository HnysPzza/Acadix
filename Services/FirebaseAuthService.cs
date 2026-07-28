using System.Net.Http.Json;
using System.Text.Json;
using AcadsJulie.Configuration;
using AcadsJulie.Models;

namespace AcadsJulie.Services;

public class FirebaseAuthService
{
    private const string UidKey = "firebase_uid";
    private const string EmailKey = "firebase_email";
    private const string IdTokenKey = "firebase_id_token";
    private const string RefreshTokenKey = "firebase_refresh_token";
    private const string ExpiresAtKey = "firebase_expires_at";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

    // Serialises token refresh. Several background leaderboard syncs can ask for a valid token
    // at once; without this they would each fire their own refresh and race to overwrite
    // _currentSession and the SecureStorage entries.
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    private FirebaseAuthSession? _currentSession;

    public FirebaseAuthSession? CurrentSession => _currentSession;
    public bool IsSignedIn => _currentSession != null;

    public async Task<FirebaseAuthSession?> RestoreSessionAsync()
    {
        var uid = await SecureStorage.Default.GetAsync(UidKey);
        var email = await SecureStorage.Default.GetAsync(EmailKey);
        var idToken = await SecureStorage.Default.GetAsync(IdTokenKey);
        var refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
        var expiresAtRaw = await SecureStorage.Default.GetAsync(ExpiresAtKey);

        if (string.IsNullOrWhiteSpace(uid) ||
            string.IsNullOrWhiteSpace(idToken) ||
            string.IsNullOrWhiteSpace(refreshToken) ||
            string.IsNullOrWhiteSpace(expiresAtRaw) ||
            !DateTimeOffset.TryParse(expiresAtRaw, out var expiresAt))
        {
            return null;
        }

        _currentSession = new FirebaseAuthSession
        {
            Uid = uid,
            Email = email ?? string.Empty,
            IdToken = idToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        };

        // Point local storage at this account before anything reads a profile.
        UserScope.SetUser(uid);

        if (_currentSession.IsExpiredSoon)
            await RefreshIdTokenAsync();

        return _currentSession;
    }

    public Task<FirebaseAuthSession> RegisterAsync(string email, string password) =>
        PasswordAuthAsync("accounts:signUp", email, password);

    public Task<FirebaseAuthSession> LoginAsync(string email, string password) =>
        PasswordAuthAsync("accounts:signInWithPassword", email, password);

    public async Task<FirebaseAuthSession> LoginWithGoogleAsync(string googleIdToken)
    {
        EnsureConfigured();

        var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key={FirebaseSettings.FirebaseWebApiKey}";
        var postBody = $"id_token={Uri.EscapeDataString(googleIdToken)}&providerId=google.com";
        var response = await Http.PostAsJsonAsync(url, new
        {
            postBody,
            requestUri = $"{GoogleOAuthService.CallbackScheme}://{GoogleOAuthService.CallbackHost}",
            returnIdpCredential = true,
            returnSecureToken = true
        });

        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(MapFirebaseError(json));

        var payload = JsonSerializer.Deserialize<AuthResponse>(json, JsonOptions()) ??
                      throw new InvalidOperationException("Firebase returned an empty auth response.");

        var session = new FirebaseAuthSession
        {
            Uid = payload.LocalId,
            Email = payload.Email ?? string.Empty,
            IdToken = payload.IdToken,
            RefreshToken = payload.RefreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(ParseExpiresIn(payload.ExpiresIn))
        };

        await SaveSessionAsync(session);
        return session;
    }

    /// <summary>
    /// Sends a Firebase password-reset email. Without this a student who forgets their
    /// password has no way back into their account.
    /// </summary>
    public async Task SendPasswordResetEmailAsync(string email)
    {
        EnsureConfigured();

        var url = $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={FirebaseSettings.FirebaseWebApiKey}";
        var response = await Http.PostAsJsonAsync(url, new
        {
            requestType = "PASSWORD_RESET",
            email
        });

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(MapFirebaseError(await response.Content.ReadAsStringAsync()));
    }

    /// <summary>
    /// Permanently deletes the signed-in Firebase account and clears the local session.
    /// Google Play requires an in-app deletion path for any app that offers account creation.
    /// </summary>
    /// <remarks>
    /// The caller is responsible for removing this user's Firestore leaderboard entry and
    /// local data first — once the account is gone the ID token can no longer authorise it.
    /// </remarks>
    public async Task DeleteAccountAsync()
    {
        EnsureConfigured();

        var idToken = await GetValidIdTokenAsync();
        var url = $"https://identitytoolkit.googleapis.com/v1/accounts:delete?key={FirebaseSettings.FirebaseWebApiKey}";
        var response = await Http.PostAsJsonAsync(url, new { idToken });

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(MapFirebaseError(await response.Content.ReadAsStringAsync()));

        await LogoutCoreAsync();
    }

    public async Task<string> GetValidIdTokenAsync()
    {
        if (_currentSession == null)
            throw new InvalidOperationException("You need to log in first.");

        if (_currentSession.IsExpiredSoon)
            await RefreshIdTokenAsync();

        return _currentSession.IdToken;
    }

    public Task LogoutAsync() => LogoutCoreAsync();

    /// <summary>
    /// Clears the session. Kept separate from <see cref="LogoutAsync"/> so the refresh path can
    /// call it while already holding <see cref="_refreshLock"/> without risking re-entry.
    /// </summary>
    private async Task LogoutCoreAsync()
    {
        _currentSession = null;
        SecureStorage.Default.Remove(UidKey);
        SecureStorage.Default.Remove(EmailKey);
        SecureStorage.Default.Remove(IdTokenKey);
        SecureStorage.Default.Remove(RefreshTokenKey);
        SecureStorage.Default.Remove(ExpiresAtKey);

        // Detach local storage so the next person to sign in cannot read this account's
        // progress or academic notes. The data stays on disk for when this user returns.
        UserScope.ClearUser();
        await Task.CompletedTask;
    }

    private static void EnsureConfigured()
    {
        if (!FirebaseSettings.IsFirebaseConfigured)
            throw new InvalidOperationException("Firebase is not configured.");
    }

    private async Task<FirebaseAuthSession> PasswordAuthAsync(string endpoint, string email, string password)
    {
        EnsureConfigured();

        var url = $"https://identitytoolkit.googleapis.com/v1/{endpoint}?key={FirebaseSettings.FirebaseWebApiKey}";
        var response = await Http.PostAsJsonAsync(url, new
        {
            email,
            password,
            returnSecureToken = true
        });

        var json = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(MapFirebaseError(json));

        var payload = JsonSerializer.Deserialize<AuthResponse>(json, JsonOptions()) ??
                      throw new InvalidOperationException("Firebase returned an empty auth response.");

        var session = new FirebaseAuthSession
        {
            Uid = payload.LocalId,
            Email = payload.Email ?? email,
            IdToken = payload.IdToken,
            RefreshToken = payload.RefreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(ParseExpiresIn(payload.ExpiresIn))
        };

        await SaveSessionAsync(session);
        return session;
    }

    private async Task RefreshIdTokenAsync()
    {
        EnsureConfigured();

        await _refreshLock.WaitAsync();
        try
        {
            if (_currentSession == null || string.IsNullOrWhiteSpace(_currentSession.RefreshToken))
                throw new InvalidOperationException("Your session has expired. Please log in again.");

            // Another caller may have refreshed while we waited for the lock.
            if (!_currentSession.IsExpiredSoon)
                return;

            var url = $"https://securetoken.googleapis.com/v1/token?key={FirebaseSettings.FirebaseWebApiKey}";
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = _currentSession.RefreshToken
            });

            var response = await Http.PostAsync(url, content);
            var json = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                // Only a rejection from Firebase means the refresh token is dead. A network
                // failure never reaches here (it throws), so we do not sign out on a blip.
                await LogoutCoreAsync();
                throw new InvalidOperationException("Your session expired. Please log in again.");
            }

            var payload = JsonSerializer.Deserialize<RefreshResponse>(json, JsonOptions()) ??
                          throw new InvalidOperationException("Firebase returned an empty refresh response.");

            _currentSession.IdToken = payload.IdToken;
            _currentSession.RefreshToken = payload.RefreshToken;
            _currentSession.Uid = payload.UserId;
            _currentSession.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(ParseExpiresIn(payload.ExpiresIn));
            await SaveSessionAsync(_currentSession);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task SaveSessionAsync(FirebaseAuthSession session)
    {
        _currentSession = session;

        // Re-point local storage before any caller reads a profile. Signing in as a different
        // account swaps the whole scope, so nobody inherits the previous user's data.
        UserScope.SetUser(session.Uid);

        await SecureStorage.Default.SetAsync(UidKey, session.Uid);
        await SecureStorage.Default.SetAsync(EmailKey, session.Email);
        await SecureStorage.Default.SetAsync(IdTokenKey, session.IdToken);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, session.RefreshToken);
        await SecureStorage.Default.SetAsync(ExpiresAtKey, session.ExpiresAt.ToString("O"));
    }

    private static int ParseExpiresIn(string? expiresIn) =>
        int.TryParse(expiresIn, out var seconds) ? seconds : 3600;

    private static string MapFirebaseError(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var message = doc.RootElement.GetProperty("error").GetProperty("message").GetString();
            return message switch
            {
                "EMAIL_EXISTS" => "That email is already registered.",
                "EMAIL_NOT_FOUND" => "No account was found for that email.",
                "INVALID_PASSWORD" => "The password is incorrect.",
                "INVALID_LOGIN_CREDENTIALS" => "That email or password is incorrect.",
                "USER_DISABLED" => "This account has been disabled.",
                "OPERATION_NOT_ALLOWED" => "Email/password sign-in is not enabled in Firebase.",
                "INVALID_EMAIL" => "Enter a valid email address.",
                "MISSING_EMAIL" => "Enter your email address.",
                "RESET_PASSWORD_EXCEED_LIMIT" => "Too many reset attempts. Please try again later.",
                "CREDENTIAL_TOO_OLD_LOGIN_AGAIN" => "For security, please log in again before doing this.",
                "TOKEN_EXPIRED" => "Your session expired. Please log in again.",

                // Firebase appends detail to some codes, so match on the prefix.
                not null when message.StartsWith("WEAK_PASSWORD", StringComparison.Ordinal)
                    => "Password must be at least 6 characters.",
                not null when message.StartsWith("TOO_MANY_ATTEMPTS_TRY_LATER", StringComparison.Ordinal)
                    => "Too many attempts. Please wait a moment and try again.",

                // Never surface a raw Firebase code to the user — it leaks backend detail and
                // reads like a crash. Anything unmapped becomes a generic message.
                _ => "Something went wrong. Please try again."
            };
        }
        catch
        {
            return "Firebase authentication failed.";
        }
    }

    private static JsonSerializerOptions JsonOptions() => new() { PropertyNameCaseInsensitive = true };

    private sealed class AuthResponse
    {
        public string LocalId { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string IdToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string? ExpiresIn { get; set; }
    }

    private sealed class RefreshResponse
    {
        public string UserId { get; set; } = string.Empty;
        public string IdToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string? ExpiresIn { get; set; }
    }
}
