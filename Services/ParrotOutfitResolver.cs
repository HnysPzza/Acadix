namespace AcadsJulie.Services;

public static class ParrotOutfitResolver
{
    /*
     * Asset naming contract:
     * parrot_{outfit}_{mood}.json
     * - outfit: history, philippinehistory, math, science, space, biology, animals_land, animals_sea, animals_air, general
     * - mood: idle, happy, sad
     *
     * Fallback chain:
     * 1) scope-aware outfit mood (eg parrot_animals_sea_happy.json)
     * 2) field-level outfit mood (eg parrot_animals_happy.json)
     * 3) generic mood (parrot_happy.json / parrot_sad.json / parrot_idle.json)
     */

    public static string Resolve(string? field, string? subField, string mood)
    {
        var safeMood = (mood ?? "idle").Trim().ToLowerInvariant();
        if (safeMood is not ("idle" or "happy" or "sad"))
        {
            safeMood = "idle";
        }

        var fieldKey = (field ?? "general").Trim().ToLowerInvariant();
        var subFieldKey = (subField ?? string.Empty).Trim().ToLowerInvariant();

        var scopeOutfit = ResolveScopeOutfit(fieldKey, subFieldKey);
        if (!string.IsNullOrEmpty(scopeOutfit))
        {
            return $"parrot_{scopeOutfit}_{safeMood}.json";
        }

        var fieldOutfit = fieldKey switch
        {
            "history" => "history",
            "math" => "math",
            "science" => "science",
            "space" => "space",
            "biology" => "biology",
            "animals" => "animals",
            _ => "general"
        };

        return $"parrot_{fieldOutfit}_{safeMood}.json";
    }

    public static string ResolveWithFallback(string? field, string? subField, string mood)
    {
        var candidate = Resolve(field, subField, mood);
        return KnownAssets.Contains(candidate) ? candidate : $"parrot_{NormalizeMood(mood)}.json";
    }

    private static string NormalizeMood(string mood)
    {
        var key = (mood ?? "idle").Trim().ToLowerInvariant();
        return key is "happy" or "sad" ? key : "idle";
    }

    private static string ResolveScopeOutfit(string fieldKey, string subFieldKey)
    {
        if (fieldKey == "history" && subFieldKey == "philippinehistory")
        {
            return "philippinehistory";
        }

        if (fieldKey == "animals")
        {
            return subFieldKey switch
            {
                "land" => "animals_land",
                "sea" => "animals_sea",
                "air" => "animals_air",
                _ => "animals"
            };
        }

        return string.Empty;
    }

    private static readonly HashSet<string> KnownAssets = new(StringComparer.OrdinalIgnoreCase)
    {
        "parrot_history_idle.json", "parrot_history_happy.json", "parrot_history_sad.json",
        "parrot_philippinehistory_idle.json", "parrot_philippinehistory_happy.json", "parrot_philippinehistory_sad.json",
        "parrot_math_idle.json", "parrot_math_happy.json", "parrot_math_sad.json",
        "parrot_science_idle.json", "parrot_science_happy.json", "parrot_science_sad.json",
        "parrot_space_idle.json", "parrot_space_happy.json", "parrot_space_sad.json",
        "parrot_biology_idle.json", "parrot_biology_happy.json", "parrot_biology_sad.json",
        "parrot_animals_idle.json", "parrot_animals_happy.json", "parrot_animals_sad.json",
        "parrot_animals_land_idle.json", "parrot_animals_land_happy.json", "parrot_animals_land_sad.json",
        "parrot_animals_sea_idle.json", "parrot_animals_sea_happy.json", "parrot_animals_sea_sad.json",
        "parrot_animals_air_idle.json", "parrot_animals_air_happy.json", "parrot_animals_air_sad.json",
        "parrot_general_idle.json", "parrot_general_happy.json", "parrot_general_sad.json",
        "parrot_idle.json", "parrot_happy.json", "parrot_sad.json"
    };
}
