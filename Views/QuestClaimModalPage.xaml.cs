using AcadsJulie.Models;

namespace AcadsJulie.Views;

public partial class QuestClaimModalPage : ContentPage
{
    public QuestClaimModalPage(DailyQuest quest)
    {
        InitializeComponent();
        
        QuestTitleLabel.Text = quest.Title;
        RewardLabel.Text = $"+{quest.RewardXP} XP";

        if (!string.IsNullOrEmpty(quest.RewardBadge))
        {
            BadgeFrame.IsVisible = true;
            BadgeLabel.Text = $"Unlocked: {quest.RewardBadge}";
        }
    }

    private async void OnCloseTapped(object sender, EventArgs e)
    {
        await ModalFrame.ScaleTo(0.8, 150, Easing.CubicIn);
        await ModalFrame.FadeTo(0, 150);
        await Navigation.PopModalAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Bounce-in animation for a rewarding feel
        ModalFrame.FadeTo(1, 200);
        await ModalFrame.ScaleTo(1.05, 250, Easing.SpringOut);
        await ModalFrame.ScaleTo(1.0, 150, Easing.CubicOut);
        
        // Minor haptic reward too
        if (App.ProfileService.GetProfile().HapticsEnabled)
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
    }
}
