using System.Text;
using AcadsJulie.Models;

namespace AcadsJulie.Services;

public interface IStudyAssistantService
{
    Task<ChatMessage> SendMessageAsync(string userMessage);
}

public class StudyAssistantService : IStudyAssistantService
{
    private readonly CareerRecommendationService _careerRecommendationService;

    public StudyAssistantService(CareerRecommendationService careerRecommendationService)
    {
        _careerRecommendationService = careerRecommendationService;
    }

    public Task<ChatMessage> SendMessageAsync(string userMessage)
    {
        var profile = App.ProfileService.GetProfile();
        var stage = profile.EducationStage;
        var recommendation = _careerRecommendationService.GenerateReport(stage);
        var lower = userMessage.Trim().ToLowerInvariant();

        string response = lower switch
        {
            _ when lower.Contains("strand") || lower.Contains("course") || lower.Contains("career") => BuildCareerResponse(stage, recommendation),
            _ when lower.Contains("task") || lower.Contains("assignment") || lower.Contains("todo") || lower.Contains("deadline") => BuildTaskResponse(),
            _ when lower.Contains("study") || lower.Contains("review") || lower.Contains("exam") => BuildStudyResponse(recommendation),
            _ when lower.Contains("math") => "For Math, try alternating Quick Math sessions with Math trivia. Aim for accuracy first, then speed.",
            _ when lower.Contains("science") => "For Science improvement, mix Science or Biology trivia with Memory games so recall becomes stronger.",
            _ => BuildGeneralResponse(recommendation)
        };

        return Task.FromResult(new ChatMessage
        {
            Role = "Assistant",
            Text = response,
            SentAt = DateTime.Now
        });
    }

    private static string BuildTaskResponse()
    {
        var summary = App.TaskService.GetSummary();
        if (summary.PendingTasks == 0)
            return "You currently have no pending academic tasks. Add your assignments in the Planner tab so I can help you prioritize them.";

        return $"You have {summary.PendingTasks} pending task(s), including {summary.DueTodayTasks} due today and {summary.OverdueTasks} overdue. Start with high-priority tasks that are due soonest, then use short focus sessions to finish them one by one.";
    }

    private static string BuildStudyResponse(RecommendationReport report)
    {
        var strongest = report.Aptitudes.OrderByDescending(a => a.Score).FirstOrDefault()?.Domain ?? "general learning";
        var weakest = report.Aptitudes.OrderBy(a => a.Score).FirstOrDefault()?.Domain ?? "consistency";

        return $"Your strongest area right now is {strongest}. Use that confidence to build momentum, then spend extra review time on {weakest}. A good routine is 25 minutes of focused study, 5 minutes rest, then one quick quiz or game for reinforcement.";
    }

    private static string BuildCareerResponse(string stage, RecommendationReport report)
    {
        var top = report.Recommendations.FirstOrDefault();
        if (top == null)
            return "I need a bit more quiz and game data before I can give a strong strand or course suggestion. Try a few trivia and logic sessions first.";

        return stage == "SeniorHigh"
            ? $"Based on your current performance, my top course suggestion is {top.Title} with a {Math.Round(top.MatchPercent)}% match. {top.Reason}"
            : $"Based on your current performance, your strongest strand match is {top.Title} at {Math.Round(top.MatchPercent)}%. {top.Reason}";
    }

    private static string BuildGeneralResponse(RecommendationReport report)
    {
        var top = report.Recommendations.FirstOrDefault();
        var builder = new StringBuilder();
        builder.Append("I can help with study tips, task planning, and strand/course guidance. ");

        if (top != null)
            builder.Append($"Right now your strongest match is {top.Title} ({Math.Round(top.MatchPercent)}%). ");

        builder.Append("Ask me about what to study next, how to prioritize assignments, or which academic path fits your quiz performance.");
        return builder.ToString();
    }
}
