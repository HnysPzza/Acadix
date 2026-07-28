namespace AcadsJulie.Services;

/// <summary>
/// Maps Acadix's Field/SubField pairs onto Open Trivia Database category ids.
///
/// The app's categories were designed around the local question bank and do not line up
/// one-to-one with OpenTDB's list, so a few deliberate decisions are encoded here:
///
/// <list type="bullet">
/// <item>Philippine History has no OpenTDB equivalent. It stays local-only — OpenTDB's
/// "History" category is overwhelmingly Western and would quietly replace curriculum-relevant
/// content with unrelated questions.</item>
/// <item>Math maps to OpenTDB 19 ("Science: Mathematics"), which is small; expect frequent
/// top-ups from the local bank.</item>
/// <item>Animals (Land/Sea/Air) all map to OpenTDB 27 ("Animals") because the API has no
/// sub-scoping. The land/sea/air split therefore only applies to local questions.</item>
/// </list>
/// </summary>
public static class TriviaCategoryMap
{
    // OpenTDB category ids (https://opentdb.com/api_category.php)
    private const int GeneralKnowledge = 9;
    private const int ScienceNature = 17;
    private const int ScienceComputers = 18;
    private const int ScienceMathematics = 19;
    private const int Mythology = 20;
    private const int Geography = 22;
    private const int History = 23;
    private const int Animals = 27;

    /// <summary>
    /// Returns the OpenTDB category id for a Field/SubField pair, or <c>null</c> when the
    /// category should be served from the local bank only.
    /// </summary>
    public static int? GetCategoryId(string? field, string? subField)
    {
        var f = (field ?? string.Empty).Trim();
        var s = (subField ?? string.Empty).Trim();

        // Philippine History is intentionally local-only — see class remarks.
        if (s.Equals("PhilippineHistory", StringComparison.OrdinalIgnoreCase))
            return null;

        return f.ToLowerInvariant() switch
        {
            "history" => History,
            "math" => ScienceMathematics,
            "biology" => ScienceNature,
            "animals" => Animals,
            "space" => ScienceNature,      // OpenTDB has no dedicated astronomy category
            "science" => ScienceNature,
            "general" => GeneralKnowledge,
            "geography" => Geography,
            "computers" => ScienceComputers,
            "mythology" => Mythology,
            _ => null
        };
    }

    /// <summary>True when this category can be topped up from OpenTDB.</summary>
    public static bool IsRemoteSupported(string? field, string? subField) =>
        GetCategoryId(field, subField) is not null;

    /// <summary>
    /// Human-readable note for categories that never use the API, shown in the setup screen so
    /// the behaviour is not mysterious.
    /// </summary>
    public static string? GetLocalOnlyReason(string? field, string? subField)
    {
        var s = (subField ?? string.Empty).Trim();

        if (s.Equals("PhilippineHistory", StringComparison.OrdinalIgnoreCase))
            return "Philippine History uses Acadix's own curated questions.";

        return IsRemoteSupported(field, subField)
            ? null
            : "This category uses Acadix's own curated questions.";
    }
}
