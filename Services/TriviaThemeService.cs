using AcadsJulie.Models;

namespace AcadsJulie.Services;

public static class TriviaThemeService
{
    public static TriviaTheme GetTheme(string field, string? subField = null)
    {
        var key = $"{field}_{subField ?? "Default"}";
        return key switch
        {
            "History_WorldHistory" => new TriviaTheme
            {
                PrimaryColor = "#8B4513",
                SecondaryColor = "#D2691E",
                AccentColor = "#CD853F",
                DisplayName = "World History"
            },
            "History_PhilippineHistory" => new TriviaTheme
            {
                PrimaryColor = "#003366",
                SecondaryColor = "#FFD700",
                AccentColor = "#FFD700",
                DisplayName = "Philippine History"
            },
            "Math_Default" or "Math_Arithmetic" or "Math_Algebra" or "Math_Geometry" => new TriviaTheme
            {
                PrimaryColor = "#2E86AB",
                SecondaryColor = "#A23B72",
                AccentColor = "#F18F01",
                DisplayName = "Math"
            },
            "Science_Default" or "Science_Physics" or "Science_Chemistry" or "Science_General" => new TriviaTheme
            {
                PrimaryColor = "#0F3460",
                SecondaryColor = "#16213E",
                AccentColor = "#00D9FF",
                DisplayName = "Science"
            },
            "Space_Default" or "Space_Astronomy" or "Space_SpaceExploration" => new TriviaTheme
            {
                PrimaryColor = "#0B0B1F",
                SecondaryColor = "#1A1A3E",
                AccentColor = "#9D4EDD",
                DisplayName = "Space"
            },
            "Biology_Default" or "Biology_Cells" or "Biology_Ecology" or "Biology_HumanBody" => new TriviaTheme
            {
                PrimaryColor = "#1B4332",
                SecondaryColor = "#40916C",
                AccentColor = "#52B788",
                DisplayName = "Biology"
            },
            "Animals_Land" => new TriviaTheme
            {
                PrimaryColor = "#5D4E37",
                SecondaryColor = "#8B7355",
                AccentColor = "#C9A227",
                DisplayName = "Animals (Land)"
            },
            "Animals_Sea" => new TriviaTheme
            {
                PrimaryColor = "#0077B6",
                SecondaryColor = "#00B4D8",
                AccentColor = "#90E0EF",
                DisplayName = "Animals (Sea)"
            },
            "Animals_Air" => new TriviaTheme
            {
                PrimaryColor = "#4A7C59",
                SecondaryColor = "#87CEEB",
                AccentColor = "#B0E0E6",
                DisplayName = "Animals (Air)"
            },
            "Geography_Default" or "Geography_WorldGeography" => new TriviaTheme
            {
                PrimaryColor = "#14532D",
                SecondaryColor = "#2D6A4F",
                AccentColor = "#74C69D",
                DisplayName = "Geography"
            },
            "Computers_Default" or "Computers_Computing" => new TriviaTheme
            {
                PrimaryColor = "#111827",
                SecondaryColor = "#1F2937",
                AccentColor = "#38BDF8",
                DisplayName = "Computers"
            },
            "Mythology_Default" or "Mythology_Myths" => new TriviaTheme
            {
                PrimaryColor = "#4C1D95",
                SecondaryColor = "#6D28D9",
                AccentColor = "#C4B5FD",
                DisplayName = "Mythology"
            },
            _ => new TriviaTheme
            {
                PrimaryColor = "#134E4A",
                SecondaryColor = "#4856D6",
                AccentColor = "#F9735B",
                DisplayName = field == "General" ? "General Knowledge" : field
            }
        };
    }
}
