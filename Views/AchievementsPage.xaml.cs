using AcadsJulie.ViewModels;

namespace AcadsJulie.Views;

public partial class AchievementsPage : ContentPage
{
    private readonly AchievementsViewModel _viewModel;

    public AchievementsPage(AchievementsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadAchievements();
    }
}
