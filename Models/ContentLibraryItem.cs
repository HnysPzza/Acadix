namespace AcadsJulie.Models;

public class ContentLibraryItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public ContentType Type { get; set; } = ContentType.Reference;
    public string Content { get; set; } = string.Empty;
    public string IconEmoji { get; set; } = "📄";
    public List<string> Tags { get; set; } = new();
    public bool IsBookmarked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public enum ContentType
{
    Reference,
    Formula,
    Guide,
    Cheatsheet,
    Timeline
}
