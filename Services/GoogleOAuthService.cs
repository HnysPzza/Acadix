using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AcadsJulie.Configuration;

namespace AcadsJulie.Services;

public class GoogleOAuthService
{
    public const string CallbackScheme = "com.companyname.acadsjulie";
    public const string CallbackHost = "auth";

    private static readonly HttpClient Http = new();

    public async Task<string> GetIdTokenAsync()
    {
        if (!FirebaseSettings.IsGoogleConfigured)
            throw new InvalidOperationException("Google sign-in is not configured.");

        var codeVerifier = CreateCodeVerifier();
        var codeChallenge = CreateCodeChallenge(codeVerifier);
        var redirectUri = $"{CallbackScheme}://{CallbackHost}";

        var authUrl =
            "https://accounts.google.com/o/oauth2/v2/auth" +
            $"?client_id={Uri.EscapeDataString(FirebaseSettings.GoogleOAuthClientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            "&response_type=code" +
            $"&scope={Uri.EscapeDataString("openid email profile")}" +
            "&prompt=select_account" +
            $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
            "&code_challenge_method=S256";

        var result = await WebAuthenticator.Default.AuthenticateAsync(
            new Uri(authUrl),
            new Uri(redirectUri));

        if (!result.Properties.TryGetValue("code", out var code) || string.IsNullOrWhiteSpace(code))
            throw new InvalidOperationException("Google sign-in did not return an authorization code.");

        var tokenResponse = await Http.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = FirebaseSettings.GoogleOAuthClientId,
            ["code"] = code,
            ["code_verifier"] = codeVerifier,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri
        }));

        var json = await tokenResponse.Content.ReadAsStringAsync();
        if (!tokenResponse.IsSuccessStatusCode)
            throw new InvalidOperationException("Google sign-in failed while exchanging the authorization code.");

        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("id_token", out var idTokenElement))
            throw new InvalidOperationException("Google sign-in did not return an ID token.");

        return idTokenElement.GetString() ?? throw new InvalidOperationException("Google sign-in returned an empty ID token.");
    }

    private static string CreateCodeVerifier()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string CreateCodeChallenge(string verifier)
    {
        var bytes = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes) =>
        Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
