using System.Text.Json;
using AcadsJulie.Models;

namespace AcadsJulie.Services;

public class ContentLibraryService
{
    private const string BookmarksKey = "content_bookmarks";
    private List<ContentLibraryItem>? _cachedContent;
    private List<string>? _cachedBookmarks;

    public List<ContentLibraryItem> GetAllContent()
    {
        if (_cachedContent != null)
            return _cachedContent;

        _cachedContent = GetSampleContent();
        LoadBookmarks();
        
        return _cachedContent;
    }

    public List<ContentLibraryItem> GetContentByCategory(string category)
    {
        return GetAllContent().Where(c => c.Category == category).ToList();
    }

    public List<ContentLibraryItem> GetBookmarkedContent()
    {
        return GetAllContent().Where(c => c.IsBookmarked).ToList();
    }

    public List<string> GetCategories()
    {
        return GetAllContent().Select(c => c.Category).Distinct().OrderBy(c => c).ToList();
    }

    public void ToggleBookmark(string contentId)
    {
        var content = GetAllContent().FirstOrDefault(c => c.Id == contentId);
        if (content == null) return;

        content.IsBookmarked = !content.IsBookmarked;
        SaveBookmarks();
    }

    public ContentLibraryItem? GetContentById(string id)
    {
        return GetAllContent().FirstOrDefault(c => c.Id == id);
    }

    public List<ContentLibraryItem> SearchContent(string query)
    {
        var lowerQuery = query.ToLower();
        return GetAllContent()
            .Where(c => c.Title.ToLower().Contains(lowerQuery) || 
                       c.Description.ToLower().Contains(lowerQuery) ||
                       c.Tags.Any(t => t.ToLower().Contains(lowerQuery)))
            .ToList();
    }

    private void LoadBookmarks()
    {
        var json = ScopedPreferences.Get(BookmarksKey, null);
        _cachedBookmarks = json == null
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

        foreach (var item in _cachedContent ?? new List<ContentLibraryItem>())
        {
            item.IsBookmarked = _cachedBookmarks.Contains(item.Id);
        }
    }

    private void SaveBookmarks()
    {
        _cachedBookmarks = GetAllContent().Where(c => c.IsBookmarked).Select(c => c.Id).ToList();
        ScopedPreferences.Set(BookmarksKey, JsonSerializer.Serialize(_cachedBookmarks));
    }

    private List<ContentLibraryItem> GetSampleContent()
    {
        return new List<ContentLibraryItem>
        {
            new ContentLibraryItem
            {
                Id = "math-formulas",
                Title = "Essential Math Formulas",
                Description = "Common algebraic and geometric formulas",
                Category = "Mathematics",
                Type = ContentType.Formula,
                IconEmoji = "🔢",
                Tags = new List<string> { "algebra", "geometry", "formulas" },
                Content = "**Quadratic Formula:**\nx = (-b ± √(b² - 4ac)) / 2a\n\n**Pythagorean Theorem:**\na² + b² = c²\n\n**Area of Circle:**\nA = πr²\n\n**Volume of Sphere:**\nV = (4/3)πr³\n\n**Distance Formula:**\nd = √((x₂-x₁)² + (y₂-y₁)²)"
            },
            new ContentLibraryItem
            {
                Id = "periodic-table",
                Title = "Periodic Table Reference",
                Description = "Key elements and their properties",
                Category = "Science",
                Type = ContentType.Reference,
                IconEmoji = "🧪",
                Tags = new List<string> { "chemistry", "elements", "periodic table" },
                Content = "**Common Elements:**\n\nH - Hydrogen (1)\nC - Carbon (6)\nN - Nitrogen (7)\nO - Oxygen (8)\nNa - Sodium (11)\nMg - Magnesium (12)\nAl - Aluminum (13)\nSi - Silicon (14)\nP - Phosphorus (15)\nS - Sulfur (16)\nCl - Chlorine (17)\nK - Potassium (19)\nCa - Calcium (20)\nFe - Iron (26)\nCu - Copper (29)\nZn - Zinc (30)\nAg - Silver (47)\nAu - Gold (79)"
            },
            new ContentLibraryItem
            {
                Id = "ph-history-timeline",
                Title = "Philippine History Timeline",
                Description = "Major events in Philippine history",
                Category = "History",
                Type = ContentType.Timeline,
                IconEmoji = "🇵🇭",
                Tags = new List<string> { "philippines", "history", "timeline" },
                Content = "**Key Dates:**\n\n1521 - Magellan arrives in the Philippines\n1565 - Spanish colonization begins\n1896 - Philippine Revolution starts\n1898 - Declaration of Independence (June 12)\n1898 - Treaty of Paris, US colonization\n1935 - Commonwealth established\n1942-1945 - Japanese occupation\n1946 - Independence from US (July 4)\n1965 - Marcos becomes president\n1986 - EDSA People Power Revolution\n1987 - New Constitution ratified"
            },
            new ContentLibraryItem
            {
                Id = "grammar-guide",
                Title = "English Grammar Quick Guide",
                Description = "Essential grammar rules and tips",
                Category = "Language",
                Type = ContentType.Guide,
                IconEmoji = "📝",
                Tags = new List<string> { "english", "grammar", "writing" },
                Content = "**Subject-Verb Agreement:**\nSingular subjects take singular verbs\nPlural subjects take plural verbs\n\n**Common Mistakes:**\n• Their/There/They're\n• Your/You're\n• Its/It's\n• Affect/Effect\n\n**Sentence Structure:**\n1. Simple: One independent clause\n2. Compound: Two independent clauses\n3. Complex: Independent + dependent clause\n4. Compound-Complex: Multiple of both"
            },
            new ContentLibraryItem
            {
                Id = "study-tips",
                Title = "Effective Study Techniques",
                Description = "Proven methods to improve learning",
                Category = "Study Skills",
                Type = ContentType.Guide,
                IconEmoji = "📚",
                Tags = new List<string> { "study", "learning", "tips" },
                Content = "**Pomodoro Technique:**\n25 min focus + 5 min break\n\n**Active Recall:**\nTest yourself instead of re-reading\n\n**Spaced Repetition:**\nReview material at increasing intervals\n\n**Feynman Technique:**\nExplain concepts in simple terms\n\n**Mind Mapping:**\nVisualize connections between ideas\n\n**SQ3R Method:**\nSurvey, Question, Read, Recite, Review"
            }
        };
    }
}
