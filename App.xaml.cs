using AcadsJulie.Services;
using AcadsJulie.Views;
using AcadsJulie.Views.Onboarding;
using Plugin.LocalNotification;

namespace AcadsJulie
{
    public partial class App : Application
    {
        // Global services (simple singleton pattern for MAUI)
        public static ProfileService ProfileService { get; private set; } = new ProfileService();
        public static ProgressService ProgressService { get; private set; } = new ProgressService();
        public static FirebaseAuthService AuthService { get; private set; } = new FirebaseAuthService();
        public static GoogleOAuthService GoogleOAuthService { get; private set; } = new GoogleOAuthService();
        public static RankingService RankingService { get; private set; } = null!;
        public static DailyChallengeService ChallengeService { get; private set; } = new DailyChallengeService();
        public static DatabaseService DatabaseService { get; private set; } = new DatabaseService();
        public static TaskService TaskService { get; private set; } = new TaskService();
        public static CareerRecommendationService CareerRecommendationService { get; private set; } = new CareerRecommendationService(ProgressService);
        public static IStudyAssistantService StudyAssistantService { get; private set; } = new StudyAssistantService(CareerRecommendationService);
        public static QuestService QuestService { get; private set; } = null!;
        public static CourseService CourseService { get; private set; } = new CourseService();
        public static GoalService GoalService { get; private set; } = new GoalService();
        public static ContentLibraryService ContentLibraryService { get; private set; } = new ContentLibraryService();

        public static void ShowNotificationWhenAllowed(NotificationRequest notification)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _ = ShowNotificationWhenAllowedAsync(notification);
            });
        }

        private static async Task ShowNotificationWhenAllowedAsync(NotificationRequest notification)
        {
            if (await NotificationPermissionService.EnsureNotificationsAllowedAsync(ProfileService))
                await LocalNotificationCenter.Current.Show(notification);
        }

        public App()
        {
            InitializeComponent();
            RankingService = new RankingService(AuthService, ProfileService, ProgressService);
            QuestService = new QuestService(ProfileService);
            TaskService.ScheduleAllPendingReminders();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AuthGatePage());
        }

        protected override void OnStart()
        {
            base.OnStart();
            TaskService.ScheduleAllPendingReminders();
        }

        protected override void OnSleep()
        {
            base.OnSleep();
            
            var profile = ProfileService.GetProfile();

            if (profile.NotificationsEnabled)
            {
                var notification = new NotificationRequest
                {
                    NotificationId = 100,
                    Title = "Keep your streak alive! ",
                    Description = "It's time for your daily brain training session. Don't let your streak break!",
                    ReturningData = "Dummy Data",
                    Schedule = new NotificationRequestSchedule
                    {
                        NotifyTime = DateTime.Now.AddHours(24)
                    }
                };
                
                ShowNotificationWhenAllowed(notification);
            }

            TaskService.ScheduleAllPendingReminders();
        }

        protected override void OnResume()
        {
            base.OnResume();
            LocalNotificationCenter.Current.Cancel(100);
            TaskService.ScheduleAllPendingReminders();
        }

        public static void ResetServices()
        {
            ProfileService = new ProfileService();
            ProgressService = new ProgressService();
            GoogleOAuthService = new GoogleOAuthService();
            RankingService = new RankingService(AuthService, ProfileService, ProgressService);
            ChallengeService = new DailyChallengeService();
            TaskService = new TaskService();
            CareerRecommendationService = new CareerRecommendationService(ProgressService);
            StudyAssistantService = new StudyAssistantService(CareerRecommendationService);
            QuestService = new QuestService(ProfileService);
            CourseService = new CourseService();
            GoalService = new GoalService();
            ContentLibraryService = new ContentLibraryService();
        }

        public static Page GetPostAuthStartPage()
        {
            var profile = ProfileService.GetProfile();
            return profile.OnboardingCompleted ? new LoadingPage() : new OnboardingPage();
        }

        public static void NavigateToPostAuthStart()
        {
            Current!.Windows[0].Page = GetPostAuthStartPage();
        }

        public static void NavigateToLogin()
        {
            Current!.Windows[0].Page = new LoginPage();
        }

        public static void QueueLeaderboardSync()
        {
            if (!AuthService.IsSignedIn)
                return;

            _ = Task.Run(async () =>
            {
                try
                {
                    await RankingService.SyncCurrentUserAsync();
                }
                catch
                {
                    // Leaderboard sync should never block local gameplay progress.
                }
            });
        }
    }
}
