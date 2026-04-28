namespace AcadsJulie.Models;

public class AptitudeScore
{
    public string Domain { get; set; } = string.Empty;
    public double Score { get; set; }
    public string DisplayScore => $"{Math.Round(Score)}%";
}

public class RecommendationItem
{
    public string Title { get; set; } = string.Empty;
    public double MatchPercent { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Strengths { get; set; } = [];
    public string MatchLabel => $"{Math.Round(MatchPercent)}% match";
    public string StrengthsLabel => Strengths.Count == 0 ? "Keep exploring more subjects." : string.Join(" • ", Strengths);
}

public class RecommendationReport
{
    public string Stage { get; set; } = "JuniorHigh";
    public bool HasEnoughData { get; set; }
    public int SessionsAnalyzed { get; set; }
    public int MinimumSessionsRequired { get; set; } = 5;
    public List<AptitudeScore> Aptitudes { get; set; } = [];
    public List<RecommendationItem> Recommendations { get; set; } = [];
    public string Summary { get; set; } = string.Empty;
    public string NextStep { get; set; } = string.Empty;
}
