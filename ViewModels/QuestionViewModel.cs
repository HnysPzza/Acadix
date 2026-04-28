using System.Collections.ObjectModel;
using AcadsJulie.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AcadsJulie.ViewModels;

public partial class AnswerOptionViewModel : ObservableObject
{
    [ObservableProperty] private string _text = string.Empty;
    [ObservableProperty] private bool _isCorrectAnswer;
    [ObservableProperty] private bool _isCorrectState;
    [ObservableProperty] private bool _isIncorrectState;
}

public partial class QuestionViewModel : ObservableObject
{
    private readonly IHapticService _hapticService;

    [ObservableProperty] private string _questionText = "Which planet is known as the Red Planet?";
    [ObservableProperty] private string _parrotAnimationSource = "parrot_idle.json";
    [ObservableProperty] private double _progress = 0.45;

    public ObservableCollection<AnswerOptionViewModel> Answers { get; } =
    [
        new() { Text = "Mars", IsCorrectAnswer = true },
        new() { Text = "Venus", IsCorrectAnswer = false },
        new() { Text = "Mercury", IsCorrectAnswer = false },
        new() { Text = "Jupiter", IsCorrectAnswer = false }
    ];

    public QuestionViewModel(IHapticService hapticService)
    {
        _hapticService = hapticService;
    }

    [RelayCommand]
    private void SelectAnswer(AnswerOptionViewModel? option)
    {
        if (option is null)
            return;

        _hapticService.LightTap();

        foreach (var item in Answers)
        {
            item.IsCorrectState = false;
            item.IsIncorrectState = false;
        }

        if (option.IsCorrectAnswer)
        {
            option.IsCorrectState = true;
            ParrotAnimationSource = "parrot_happy.json";
            return;
        }

        option.IsIncorrectState = true;
        ParrotAnimationSource = "parrot_sad.json";
    }
}
