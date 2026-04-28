namespace AcadsJulie
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register routes for non-tab pages
            Routing.RegisterRoute("SkillCategoryPage", typeof(Views.SkillCategoryPage));
            Routing.RegisterRoute("OnboardingPage", typeof(Views.Onboarding.OnboardingPage));
            Routing.RegisterRoute("EditProfilePage", typeof(Views.EditProfilePage));
            Routing.RegisterRoute("AchievementsPage", typeof(Views.AchievementsPage));
            Routing.RegisterRoute("PlannerPage", typeof(Views.PlannerPage));
            Routing.RegisterRoute("DailyChallengePage", typeof(Views.DailyChallengePage));
            Routing.RegisterRoute("RankingPage", typeof(Views.RankingPage));
            Routing.RegisterRoute("RecommendationsPage", typeof(Views.RecommendationsPage));
            Routing.RegisterRoute("StudyAssistantPage", typeof(Views.StudyAssistantPage));
            Routing.RegisterRoute("LearningHubPage", typeof(Views.LearningHubPage));
            Routing.RegisterRoute("CoursesPage", typeof(Views.CoursesPage));
            Routing.RegisterRoute("CourseDetailPage", typeof(Views.CourseDetailPage));
            Routing.RegisterRoute("LessonViewerPage", typeof(Views.LessonViewerPage));
            Routing.RegisterRoute("ContentLibraryPage", typeof(Views.ContentLibraryPage));
            Routing.RegisterRoute("ContentViewerPage", typeof(Views.ContentViewerPage));

            Routing.RegisterRoute("Games/CardMatch", typeof(Views.Games.CardMatchPage));
            Routing.RegisterRoute("Games/OddOneOut", typeof(Views.Games.OddOneOutPage));
            Routing.RegisterRoute("Games/Sequence", typeof(Views.Games.SequenceGamePage));
            Routing.RegisterRoute("Games/QuickMath", typeof(Views.Games.QuickMathPage));

            // Variant Games
            Routing.RegisterRoute("Games/SequenceRecall", typeof(Views.Games.SequenceRecallPage));
            Routing.RegisterRoute("Games/ColorTap", typeof(Views.Games.ColorTapPage));
            Routing.RegisterRoute("Games/NumberGrid", typeof(Views.Games.NumberGridPage));
            Routing.RegisterRoute("Games/ReactionTap", typeof(Views.Games.ReactionTapPage));

            Routing.RegisterRoute("TriviaSetup", typeof(Views.TriviaSetupPage));
            Routing.RegisterRoute("Games/TriviaGame", typeof(Views.Games.TriviaGamePage));
        }
    }
}
