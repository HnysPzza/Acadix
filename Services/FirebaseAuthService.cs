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

    private static readonly HttpClient Http = new();
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

    public async Task<string> GetValidIdTokenAsync()
    {
        if (_currentSession == null)
            throw new InvalidOperationException("You need to log in first.");

        if (_currentSession.IsExpiredSoon)
            await RefreshIdTokenAsync();

        return _currentSession.IdToken;
    }

    public async Task LogoutAsync()
    {
        _currentSession = null;
        SecureStorage.Default.Remove(UidKey);
        SecureStorage.Default.Remove(EmailKey);
        SecureStorage.Default.Remove(IdTokenKey);
        SecureStorage.Default.Remove(RefreshTokenKey);
        SecureStorage.Default.Remove(ExpiresAtKey);
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

        if (_currentSession == null || string.IsNullOrWhiteSpace(_currentSession.RefreshToken))
            throw new InvalidOperationException("Your session has expired. Please log in again.");

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
            await LogoutAsync();
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

    private async Task SaveSessionAsync(FirebaseAuthSession session)
    {
        _currentSession = session;
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
                "WEAK_PASSWORD : Password should be at least 6 characters" => "Password must be at least 6 characters.",
                "USER_DISABLED" => "This account has been disabled.",
                "OPERATION_NOT_ALLOWED" => "Email/password sign-in is not enabled in Firebase.",
                "INVALID_EMAIL" => "Enter a valid email address.",
                _ => message ?? "Firebase authentication failed."
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
