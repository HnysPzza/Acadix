using AcadsJulie.Models;
using AcadsJulie.Services;
using System.Collections.ObjectModel;

namespace AcadsJulie.Views
{
    public partial class DailyQuestsPage : ContentPage
    {
        private readonly QuestService _questService;
        public ObservableCollection<DailyQuest> Quests { get; set; } = new();

        public DailyQuestsPage()
        {
            InitializeComponent();
            _questService = App.QuestService;
            QuestsList.ItemsSource = Quests;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadQuests();
        }

        private void LoadQuests()
        {
            Quests.Clear();
            var quests = _questService.GetTodayQuests();
            foreach (var q in quests)
            {
                Quests.Add(q);
            }
        }

        private async void OnClaimRewardClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is DailyQuest quest)
            {
                if (App.ProfileService.GetProfile().HapticsEnabled)
                    HapticFeedback.Default.Perform(HapticFeedbackType.Click);

                bool success = _questService.ClaimReward(quest.Id);
                
                if (success)
                {
                    // Visual feedback
                    button.Text = "Claimed!";
                    button.BackgroundColor = Colors.LightGray;
                    button.IsEnabled = false;

                    await Navigation.PushModalAsync(new QuestClaimModalPage(quest));
                    
                    // Reload
                    LoadQuests();
                }
            }
        }
    }
}
