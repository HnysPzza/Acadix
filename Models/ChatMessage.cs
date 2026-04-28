namespace AcadsJulie.Models;

public class ChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Role { get; set; } = "Assistant";
    public string Text { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.Now;
    public bool IsUser => Role.Equals("User", StringComparison.OrdinalIgnoreCase);
    public string TimestampLabel => SentAt.ToString("hh:mm tt");
}
