namespace AcadsJulie.Models;

public class FirebaseAuthSession
{
    public string Uid { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string IdToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }

    public bool IsExpiredSoon => DateTimeOffset.UtcNow >= ExpiresAt.AddMinutes(-5);
}
