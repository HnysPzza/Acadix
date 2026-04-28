using AcadsJulie.Models;

namespace AcadsJulie.Services;

public class CareerRecommendationService
{
    private readonly ProgressService _progressService;

    public CareerRecommendationService(ProgressService progressService)
    {
        _progressService = progressService;
    }

    public RecommendationReport GenerateReport(string stage)
    {
        var sessions = _progressService.GetSessions();
        var report = new RecommendationReport
        {
            Stage = stage,
            SessionsAnalyzed = sessions.Count,
            HasEnoughData = sessions.Count >= 5
        };

        var aptitudes = BuildAptitudes(sessions);
        report.Aptitudes = aptitudes.OrderByDescending(a => a.Score).ToList();

        if (!report.HasEnoughData)
        {
            var remaining = report.MinimumSessionsRequired - report.SessionsAnalyzed;
            report.Summary = "Play more quizzes and games to unlock clearer recommendations.";
            report.NextStep = $"Complete {remaining} more learning session{(remaining == 1 ? "" : "s")} across trivia, logic, memory, and speed activities.";
            report.Recommendations = [];
            return report;
        }

        report.Recommendations = stage == "SeniorHigh"
            ? BuildCourseRecommendations(aptitudes)
            : BuildStrandRecommendations(aptitudes);

        if (report.Recommendations.Count > 0)
        {
            var top = report.Recommendations[0];
            report.Summary = stage == "SeniorHigh"
                ? $"Your current data points most strongly toward {top.Title}."
                : $"Your current performance suggests {top.Title} as your strongest strand fit.";
            report.NextStep = BuildNextStep(report);
        }
        else
        {
            report.Summary = "Play more quizzes and games to unlock clearer recommendations.";
            report.NextStep = "Try at least 5 mixed sessions across trivia, logic, memory, and speed activities.";
        }

        return report;
    }

    private static List<AptitudeScore> BuildAptitudes(List<GameSession> sessions)
    {
        double math = 0;
        double science = 0;
        double language = 0;
        double humanities = 0;
        double analytical = 0;
        double creativity = 0;

        var mathTrivia = ScoreTrivia(sessions, "Math");
        var scienceTrivia = AverageScores(sessions, new[] { "Science", "Biology", "Space" });
        var historyTrivia = AverageScores(sessions, new[] { "WorldHistory", "PhilippineHistory" });
        var languageTrivia = AverageScores(sessions, new[] { "Language", "English", "Reading" });

        var logic = AverageCategoryScore(sessions, "Logic");
        var speed = AverageCategoryScore(sessions, "Speed");
        var memory = AverageCategoryScore(sessions, "Memory");
        var focus = AverageCategoryScore(sessions, "Focus");
        var trivia = AverageCategoryScore(sessions, "Trivia");

        math = (mathTrivia * 0.45) + (logic * 0.35) + (speed * 0.20);
        science = (scienceTrivia * 0.50) + (logic * 0.25) + (memory * 0.25);
        language = (languageTrivia * 0.50) + (focus * 0.25) + (trivia * 0.25);
        humanities = (historyTrivia * 0.50) + (languageTrivia * 0.20) + (focus * 0.30);
        analytical = (logic * 0.45) + (mathTrivia * 0.30) + (speed * 0.25);
        creativity = (memory * 0.35) + (languageTrivia * 0.30) + (focus * 0.35);

        return
        [
            new AptitudeScore { Domain = "Math", Score = Clamp(math) },
            new AptitudeScore { Domain = "Science", Score = Clamp(science) },
            new AptitudeScore { Domain = "Language", Score = Clamp(language) },
            new AptitudeScore { Domain = "Humanities", Score = Clamp(humanities) },
            new AptitudeScore { Domain = "Analytical", Score = Clamp(analytical) },
            new AptitudeScore { Domain = "Creativity", Score = Clamp(creativity) }
        ];
    }

    private static List<RecommendationItem> BuildStrandRecommendations(List<AptitudeScore> aptitudes)
    {
        double math = Get(aptitudes, "Math");
        double science = Get(aptitudes, "Science");
        double language = Get(aptitudes, "Language");
        double humanities = Get(aptitudes, "Humanities");
        double analytical = Get(aptitudes, "Analytical");
        double creativity = Get(aptitudes, "Creativity");
        double applied = (analytical * 0.45) + (creativity * 0.30) + (science * 0.25);

        var recommendations = new List<RecommendationItem>
        {
            CreateRecommendation("STEM", (math * 0.35) + (science * 0.35) + (analytical * 0.30), "You show strong performance in quantitative, science, and logic-heavy activities.", "Best fit for students leaning toward engineering, science, technology, and medicine.", "Math", "Science", "Analytical"),
            CreateRecommendation("ABM", (math * 0.35) + (analytical * 0.35) + (language * 0.30), "Your scores suggest balanced analytical and communication strengths.", "Good for business, entrepreneurship, accounting, and management tracks.", "Math", "Analytical", "Language"),
            CreateRecommendation("HUMSS", (humanities * 0.40) + (language * 0.35) + (creativity * 0.25), "You are doing well in reading-heavy and human-centered subject areas.", "Good for law, communication, teaching, psychology, and social sciences.", "Humanities", "Language", "Creativity"),
            CreateRecommendation("GAS", (math * 0.20) + (science * 0.20) + (language * 0.20) + (humanities * 0.20) + (analytical * 0.20), "Your results are balanced across multiple domains.", "Good if you want flexibility before committing to a highly specialized path.", "Math", "Science", "Language"),
            CreateRecommendation("TVL", applied, "You respond well to practical and performance-based tasks.", "Good for applied, hands-on, and technical skill pathways.", "Analytical", "Creativity", "Science")
        };

        return recommendations
            .OrderByDescending(r => r.MatchPercent)
            .ToList();
    }

    private static List<RecommendationItem> BuildCourseRecommendations(List<AptitudeScore> aptitudes)
    {
        double math = Get(aptitudes, "Math");
        double science = Get(aptitudes, "Science");
        double language = Get(aptitudes, "Language");
        double humanities = Get(aptitudes, "Humanities");
        double analytical = Get(aptitudes, "Analytical");
        double creativity = Get(aptitudes, "Creativity");

        var recommendations = new List<RecommendationItem>
        {
            CreateRecommendation("Engineering / Architecture", (math * 0.35) + (science * 0.25) + (analytical * 0.40), "Your data shows strong quantitative and logical performance.", "A strong match for engineering, architecture, and other technical design programs.", "Math", "Science", "Analytical"),
            CreateRecommendation("Computer Science / IT", (math * 0.30) + (analytical * 0.50) + (creativity * 0.20), "You perform well in problem-solving, patterns, and speed-based reasoning.", "Good fit for software, systems, data, and digital technology tracks.", "Analytical", "Math", "Creativity"),
            CreateRecommendation("Medicine / Health Sciences", (science * 0.45) + (memoryFrom(aptitudes) * 0.30) + (language * 0.25), "You show good science retention with strong reading and recall potential.", "Relevant for nursing, medicine, pharmacy, and allied health programs.", "Science", "Language", "Memory"),
            CreateRecommendation("Business / Economics", (math * 0.30) + (analytical * 0.35) + (language * 0.35), "Your profile indicates a mix of analytical and communication strengths.", "Good for business administration, economics, marketing, and finance.", "Math", "Language", "Analytical"),
            CreateRecommendation("Education / Social Sciences", (humanities * 0.35) + (language * 0.35) + (creativity * 0.30), "You appear strong in people-centered and communication-oriented learning.", "Good fit for teaching, psychology, communication, and development studies.", "Humanities", "Language", "Creativity"),
            CreateRecommendation("Law / Political Science", (humanities * 0.40) + (language * 0.35) + (analytical * 0.25), "Your results suggest potential in argument-heavy and reading-intensive disciplines.", "Good for pre-law, public administration, and policy-related tracks.", "Humanities", "Language", "Analytical")
        };

        return recommendations
            .OrderByDescending(r => r.MatchPercent)
            .Take(5)
            .ToList();

        static double memoryFrom(List<AptitudeScore> list) => (Get(list, "Science") * 0.5) + (Get(list, "Creativity") * 0.5);
    }

    private static RecommendationItem CreateRecommendation(string title, double score, string reason, string description, params string[] strengths)
    {
        return new RecommendationItem
        {
            Title = title,
            MatchPercent = Clamp(score),
            Reason = reason,
            Description = description,
            Strengths = strengths.ToList()
        };
    }

    private static double ScoreTrivia(List<GameSession> sessions, string keyword)
    {
        var relevant = sessions
            .Where(s => s.GameId == "Trivia" && (s.SubCategory?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();

        if (relevant.Count == 0)
            return 0;

        return Clamp(relevant.Average(s => ((s.Score / 10.0) * 0.6) + (s.Accuracy * 0.4)));
    }

    private static double AverageScores(List<GameSession> sessions, IEnumerable<string> keywords)
    {
        var scores = keywords.Select(k => ScoreTrivia(sessions, k)).Where(s => s > 0).ToList();
        return scores.Count == 0 ? 0 : Clamp(scores.Average());
    }

    private static double AverageCategoryScore(List<GameSession> sessions, string category)
    {
        var relevant = sessions.Where(s => s.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        if (relevant.Count == 0)
            return 0;

        return Clamp(relevant.Average(s => ((s.Score / 10.0) * 0.7) + (s.Accuracy * 0.3)));
    }

    private static double Get(List<AptitudeScore> aptitudes, string domain)
    {
        return aptitudes.FirstOrDefault(a => a.Domain == domain)?.Score ?? 0;
    }

    private static double Clamp(double value) => Math.Max(0, Math.Min(100, value));

    private static string BuildNextStep(RecommendationReport report)
    {
        var weakest = report.Aptitudes.OrderBy(a => a.Score).FirstOrDefault();
        if (weakest == null)
            return "Keep taking quizzes to improve recommendation accuracy.";

        return $"Improve your {weakest.Domain.ToLowerInvariant()} performance to broaden your options and refine your recommendation.";
    }
}
