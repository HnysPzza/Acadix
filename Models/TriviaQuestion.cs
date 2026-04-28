namespace AcadsJulie.Models;

public class TriviaQuestion
{
    public string QuestionText { get; set; } = string.Empty;
    public List<string> Options { get; set; } = [];
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public string Field { get; set; } = string.Empty;
    public string SubField { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "Easy";
    public string Depth { get; set; } = "General";
}
