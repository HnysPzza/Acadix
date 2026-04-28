using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace AcadsJulie.ViewModels;

public partial class TriviaChoice : ObservableObject
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    [ObservableProperty] private bool _isSelected;
}

public partial class TriviaSetupViewModel : ObservableObject
{
    [ObservableProperty] private string _selectedField = "General";
    [ObservableProperty] private string _selectedSubField = "GeneralKnowledge";
    [ObservableProperty] private string _selectedDifficulty = "Easy";

    public ObservableCollection<TriviaChoice> Fields { get; } =
    [
        new() { Key = "History", Title = "History", Icon = "🏛️" },
        new() { Key = "Math", Title = "Math", Icon = "🔢" },
        new() { Key = "Science", Title = "Science", Icon = "🔬" },
        new() { Key = "Space", Title = "Space", Icon = "🚀" },
        new() { Key = "Biology", Title = "Biology", Icon = "🧬" },
        new() { Key = "Animals", Title = "Animals", Icon = "🦁" },
        new() { Key = "General", Title = "General", Icon = "📚", IsSelected = true }
    ];

    public ObservableCollection<TriviaChoice> Difficulties { get; } =
    [
        new() { Key = "Easy", Title = "Easy", Icon = "🟢", IsSelected = true },
        new() { Key = "Medium", Title = "Medium", Icon = "🟡" },
        new() { Key = "Hard", Title = "Hard", Icon = "🔴" }
    ];

    public TriviaSetupViewModel()
    {
        UpdateSelection(Fields, SelectedField);
        UpdateSelection(Difficulties, SelectedDifficulty);
    }

    public bool ShowHistoryScope => SelectedField == "History";
    public bool ShowAnimalScope => SelectedField == "Animals";
    public string SelectedSummary => $"Selected: {SelectedField} • {SelectedSubField} • {SelectedDifficulty}";

    partial void OnSelectedFieldChanged(string value)
    {
        OnPropertyChanged(nameof(ShowHistoryScope));
        OnPropertyChanged(nameof(ShowAnimalScope));
        SelectedSubField = value switch
        {
            "History" => "WorldHistory",
            "Animals" => "Land",
            "Math" => "Arithmetic",
            "Science" => "General",
            "Space" => "Astronomy",
            "Biology" => "HumanBody",
            "General" => "GeneralKnowledge",
            _ => "Default"
        };
        OnPropertyChanged(nameof(SelectedSummary));
    }

    partial void OnSelectedSubFieldChanged(string value)
    {
        OnPropertyChanged(nameof(SelectedSummary));
    }

    partial void OnSelectedDifficultyChanged(string value)
    {
        OnPropertyChanged(nameof(SelectedSummary));
    }

    [RelayCommand]
    void SelectField(string key)
    {
        SelectedField = key;
        UpdateSelection(Fields, key);
    }

    [RelayCommand]
    void SelectDifficulty(string key)
    {
        SelectedDifficulty = key;
        UpdateSelection(Difficulties, key);
    }

    [RelayCommand]
    void SelectSubField(string key) => SelectedSubField = key;

    [RelayCommand]
    async Task StartQuiz()
    {
        var subField = SelectedField switch
        {
            "History" => SelectedSubField,
            "Animals" => SelectedSubField,
            "Math" => "Arithmetic",
            "Science" => "General",
            "Space" => "Astronomy",
            "Biology" => "HumanBody",
            "General" => "GeneralKnowledge",
            _ => "Default"
        };

        await Shell.Current.GoToAsync(
            $"Games/TriviaGame?field={SelectedField}&subField={subField}&difficulty={SelectedDifficulty}&mode=Normal");
    }

    [RelayCommand]
    async Task GoBack()
    {
        if (Application.Current?.MainPage != null)
            await Application.Current.MainPage.Navigation.PopAsync();
    }

    private static void UpdateSelection(IEnumerable<TriviaChoice> items, string selectedKey)
    {
        foreach (var item in items)
        {
            item.IsSelected = item.Key == selectedKey;
        }
    }
}
