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
        public static OpenTriviaService OpenTriviaService { get; private set; } = new OpenTriviaService();
        public static TriviaQuestionProvider TriviaQuestionProvider { get; private set; } = new TriviaQuestionProvider(OpenTriviaService);

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

            // Signing in as a different account must drop every cached profile/session list,
            // otherwise the previous user's data stays in memory under the new user's name.
            // Unsubscribe first so a re-created App cannot double-subscribe.
            UserScope.Changed -= ResetServices;
            UserScope.Changed += ResetServices;

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

            // Push any debounced progress before the process can be killed.
            _ = FlushLeaderboardSyncAsync();

            var profile = ProfileService.GetProfile();

            if (profile.NotificationsEnabled)
            {
                var notification = new NotificationRequest
                {
                    NotificationId = NotificationIdAllocator.DailyStreakId,
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
            LocalNotificationCenter.Current.Cancel(NotificationIdAllocator.DailyStreakId);
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

            // Recreated so the trivia no-repeat history is re-read under the new account scope.
            OpenTriviaService = new OpenTriviaService();
            TriviaQuestionProvider = new TriviaQuestionProvider(OpenTriviaService);
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

        // Finishing one game touches the profile many times (XP, category score, challenge,
        // quests, badges), and every touch used to fire its own Firestore write — up to nine
        // per game. These fields coalesce that burst into a single delayed write.
        private static readonly SemaphoreSlim SyncGate = new(1, 1);
        private static readonly TimeSpan SyncDebounce = TimeSpan.FromSeconds(5);
        private static int _syncQueued;

        /// <summary>
        /// Requests a leaderboard sync. Calls made while one is pending are absorbed into it,
        /// so a burst of profile saves results in one upload rather than one per save.
        /// </summary>
        public static void QueueLeaderboardSync()
        {
            if (!AuthService.IsSignedIn)
                return;

            // Already a flush pending: it will pick up whatever we just saved.
            if (Interlocked.Exchange(ref _syncQueued, 1) == 1)
                return;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(SyncDebounce);
                    Interlocked.Exchange(ref _syncQueued, 0);

                    await SyncGate.WaitAsync();
                    try
                    {
                        if (AuthService.IsSignedIn)
                            await RankingService.SyncCurrentUserAsync();
                    }
                    finally
                    {
                        SyncGate.Release();
                    }
                }
                catch
                {
                    // Leaderboard sync should never block local gameplay progress.
                    Interlocked.Exchange(ref _syncQueued, 0);
                }
            });
        }

        /// <summary>
        /// Uploads any pending progress immediately. Called when the app goes to the background
        /// so a debounced write is not lost if the process is killed.
        /// </summary>
        public static async Task FlushLeaderboardSyncAsync()
        {
            if (!AuthService.IsSignedIn)
                return;

            Interlocked.Exchange(ref _syncQueued, 0);

            try
            {
                await SyncGate.WaitAsync();
                try
                {
                    await RankingService.SyncCurrentUserAsync();
                }
                finally
                {
                    SyncGate.Release();
                }
            }
            catch
            {
                // Best effort — never block app suspension.
            }
        }
    }
}
