using AcadsJulie.Services;
using AcadsJulie.ViewModels;
using System.Timers;

namespace AcadsJulie.Views;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;
    private readonly ProgressService _progressService;
    private readonly ProfileService _profileService;
    private readonly ContentLibraryService _contentLibraryService;
    private readonly TaskService _taskService;

    // Timer fields
    private System.Timers.Timer? _studyTimer;
    private int _remainingSeconds = 1500; // 25 minutes
    private bool _isTimerRunning = false;
    private DateTime _timerStartTime;
    private int _xpToastVersion;

    // Daily Quotes
    private readonly (string Quote, string Author)[] _dailyQuotes = new[]
    {
        ("The beautiful thing about learning is that no one can take it away from you.", "B.B. King"),
        ("Education is the passport to the future.", "Malcolm X"),
        ("The more that you read, the more things you will know.", "Dr. Seuss"),
        ("Learning is not attained by chance, it must be sought for with ardor.", "Abigail Adams"),
        ("The mind is not a vessel to be filled but a fire to be kindled.", "Plutarch"),
        ("Success is the sum of small efforts, repeated day in and day out.", "Robert Collier"),
        ("Don't watch the clock; do what it does. Keep going.", "Sam Levenson"),
        ("The expert in anything was once a beginner.", "Helen Hayes"),
        ("There are no shortcuts to any place worth going.", "Beverly Sills"),
        ("Your future is created by what you do today, not tomorrow.", "Robert Kiyosaki"),
        ("Strive for progress, not perfection.", "Unknown"),
        ("The secret of getting ahead is getting started.", "Mark Twain"),
        ("Small steps every day lead to big results.", "Unknown"),
        ("Believe you can and you're halfway there.", "Theodore Roosevelt"),
        ("It always seems impossible until it's done.", "Nelson Mandela")
    };

    public HomePage()
    {
        InitializeComponent();
        _viewModel = new HomeViewModel();
        BindingContext = _viewModel;
        _progressService = App.ProgressService;
        _profileService = App.ProfileService;
        _contentLibraryService = App.ContentLibraryService;
        _taskService = App.TaskService;

        LoadDailyQuote();
        UpdateWeeklyProgress();
        LoadBookmarks();
        UpdateCheckInStatus();
        UpdateNotesCount();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Refresh();
        LoadRecentActivity();
        UpdateWeeklyProgress();
        UpdateNotesCount();
    }

    #region Daily Quote
    private void LoadDailyQuote()
    {
        // Use day of year to pick a consistent quote for the day
        var dayOfYear = DateTime.Now.DayOfYear;
        var quoteIndex = dayOfYear % _dailyQuotes.Length;
        var (quote, author) = _dailyQuotes[quoteIndex];

        QuoteTextLabel.Text = quote;
        QuoteAuthorLabel.Text = $"— {author}";
    }
    #endregion

    #region Study Timer
    private void OnStartTimerClicked(object? sender, EventArgs e)
    {
        if (!_isTimerRunning)
        {
            StartTimer();
        }
    }

    private void OnStopTimerClicked(object? sender, EventArgs e)
    {
        StopTimer();
    }

    private void StartTimer()
    {
        _isTimerRunning = true;
        _timerStartTime = DateTime.Now;

        // Update UI
        StartTimerButton.IsVisible = false;
        StopTimerButton.IsVisible = true;
        TimerStatusLabel.Text = "🔥 Focus mode ON - Stay focused!";
        TimerCard.BackgroundColor = GetColor("MobileSuccess", "#16A34A");

        // Start timer
        _studyTimer = new System.Timers.Timer(1000); // 1 second intervals
        _studyTimer.Elapsed += OnTimerTick;
        _studyTimer.AutoReset = true;
        _studyTimer.Start();
    }

    private void StopTimer()
    {
        _isTimerRunning = false;
        _studyTimer?.Stop();
        _studyTimer?.Dispose();
        _studyTimer = null;

        // Calculate completed time and award XP
        var elapsedSeconds = 1500 - _remainingSeconds;
        var completedMinutes = elapsedSeconds / 60;

        if (completedMinutes >= 5) // Award XP for at least 5 minutes
        {
            var xpEarned = completedMinutes * 2; // 2 XP per minute
            App.ProfileService.AddXP(xpEarned);
            TimerStatusLabel.Text = $"✅ Completed! Earned {xpEarned} XP";
            _ = ShowXpGainAsync(xpEarned);
        }
        else
        {
            TimerStatusLabel.Text = "Timer stopped early";
        }

        // Reset
        _remainingSeconds = 1500;
        UpdateTimerDisplay();

        StartTimerButton.IsVisible = true;
        StopTimerButton.IsVisible = false;
        TimerCard.BackgroundColor = GetColor("MobilePrimaryDark", "#134E4A");
    }

    private void OnTimerTick(object? sender, ElapsedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_remainingSeconds > 0)
            {
                _remainingSeconds--;
                UpdateTimerDisplay();
            }
            else
            {
                // Timer completed!
                StopTimer();
                TimerStatusLabel.Text = "🎉 25 minutes complete! +50 XP";
                App.ProfileService.AddXP(50); // Bonus for completing full session
                _ = ShowXpGainAsync(50);

                // Show completion alert
                DisplayAlert("Focus Complete! 🎉", "Great job! You stayed focused for 25 minutes and earned bonus XP!", "Awesome");
            }
        });
    }

    private void UpdateTimerDisplay()
    {
        var minutes = _remainingSeconds / 60;
        var seconds = _remainingSeconds % 60;
        TimerDisplayLabel.Text = $"{minutes:D2}:{seconds:D2}";
    }
    #endregion

    #region Weekly Progress
    private void UpdateWeeklyProgress()
    {
        var sessions = _progressService.GetSessions();
        var tasks = _taskService.GetTasks();

        // Calculate this week's stats
        var startOfWeek = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
        var weekSessions = sessions.Where(s => s.PlayedAt >= startOfWeek).ToList();
        var completedTasks = tasks
            .Where(t => t.IsCompleted && t.CompletedAt.HasValue && t.CompletedAt.Value >= startOfWeek)
            .ToList();

        var studyDays = weekSessions.Select(s => s.PlayedAt.Date).Distinct().Count();
        var tasksDone = completedTasks.Count;
        var xpEarned = weekSessions.Sum(s => s.Score / 10);

        WeekStudyDaysLabel.Text = studyDays.ToString();
        WeekTasksLabel.Text = tasksDone.ToString();
        WeekXPLabel.Text = xpEarned.ToString();

        // Calculate comparison with last week
        var lastWeekStart = startOfWeek.AddDays(-7);
        var lastWeekSessions = sessions.Where(s => s.PlayedAt >= lastWeekStart && s.PlayedAt < startOfWeek).ToList();
        var lastWeekCount = lastWeekSessions.Count;

        if (lastWeekCount > 0)
        {
            var percentChange = ((weekSessions.Count - lastWeekCount) * 100) / lastWeekCount;
            var sign = percentChange >= 0 ? "↑" : "↓";
            WeekComparisonLabel.Text = $"{sign} {Math.Abs(percentChange)}% vs last week";
            WeekComparisonLabel.TextColor = percentChange >= 0
                ? GetColor("MobileSuccess", "#16A34A")
                : GetColor("Danger", "#EF4444");
        }
        else
        {
            WeekComparisonLabel.Text = "New week started!";
            WeekComparisonLabel.TextColor = GetColor("MobilePrimary", "#0F766E");
        }

        UpdateRecentDailyProgress(weekSessions);
    }

    private void UpdateRecentDailyProgress(List<Models.GameSession> weekSessions)
    {
        var days = Enumerable.Range(0, 3)
            .Select(offset => DateTime.Today.AddDays(offset - 2))
            .ToList();

        var counts = days
            .Select(day => weekSessions.Count(s => s.PlayedAt.Date == day.Date))
            .ToList();

        var maxCount = Math.Max(1, counts.Max());
        UpdateDailyProgressRow(WeekDay1Label, WeekDay1Progress, WeekDay1PercentLabel, days[0], counts[0], maxCount);
        UpdateDailyProgressRow(WeekDay2Label, WeekDay2Progress, WeekDay2PercentLabel, days[1], counts[1], maxCount);
        UpdateDailyProgressRow(WeekDay3Label, WeekDay3Progress, WeekDay3PercentLabel, days[2], counts[2], maxCount);
    }

    private static void UpdateDailyProgressRow(Label dayLabel, ProgressBar progressBar, Label percentLabel, DateTime day, int count, int maxCount)
    {
        var progress = maxCount == 0 ? 0 : count / (double)maxCount;
        dayLabel.Text = day.ToString("ddd");
        progressBar.Progress = progress;
        percentLabel.Text = $"{Math.Round(progress * 100)}%";
    }
    #endregion

    private void LoadRecentActivity()
    {
        RecentActivityStack.Children.Clear();
        var sessions = _progressService.GetSessions().OrderByDescending(s => s.PlayedAt).Take(3).ToList();

        if (sessions.Count == 0)
        {
            RecentActivityStack.Children.Add(new Label
            {
                Text = "No games played yet. Start training! 🚀",
                TextColor = GetColor("TextSecondary", "#59686A"),
                FontSize = 14,
                HorizontalOptions = LayoutOptions.Center
            });
        }
        else
        {
            foreach (var session in sessions)
            {
                var icon = session.Category switch
                {
                    "Memory" => "🧠",
                    "Focus"  => "🎯",
                    "Logic"  => "🧩",
                    "Speed"  => "⚡",
                    "Trivia" => "📚",
                    _        => "🎮"
                };
                var grid = new Grid
                {
                    ColumnDefinitions =
                    [
                        new ColumnDefinition(GridLength.Auto),
                        new ColumnDefinition(GridLength.Star),
                        new ColumnDefinition(GridLength.Auto)
                    ],
                    ColumnSpacing = 12
                };
                grid.Add(new Label { Text = icon, FontSize = 20, VerticalOptions = LayoutOptions.Center }, 0);
                grid.Add(new Label
                {
                    Text = session.Category,
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = GetColor("TextPrimary", "#121A1C"),
                    VerticalOptions = LayoutOptions.Center
                }, 1);
                grid.Add(new Label
                {
                    Text = $"+{session.Score} pts",
                    FontSize = 14,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = GetColor("MobilePrimary", "#0F766E"),
                    VerticalOptions = LayoutOptions.Center
                }, 2);
                RecentActivityStack.Children.Add(grid);

                if (sessions.IndexOf(session) < sessions.Count - 1)
                    RecentActivityStack.Children.Add(new BoxView { BackgroundColor = GetColor("MobileDivider", "#D9E5E0"), HeightRequest = 1 });
            }
        }
    }

    #region Bookmarks Quick Access
    private void LoadBookmarks()
    {
        var bookmarks = _contentLibraryService.GetBookmarkedContent().Take(5).ToList();
        BookmarksCollection.ItemsSource = bookmarks;
    }

    private async void OnBookmarkTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is string contentId)
        {
            await Shell.Current.GoToAsync($"ContentViewerPage?contentId={contentId}");
        }
    }

    private async void OnViewAllBookmarksClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("ContentLibraryPage");
    }
    #endregion

    #region Daily Check-in
    private void UpdateCheckInStatus()
    {
        var profile = _profileService.GetProfile();
        var alreadyCheckedIn = _profileService.HasCheckedInToday();

        if (alreadyCheckedIn)
        {
            CheckInButton.IsEnabled = false;
            CheckInButton.Text = "✅ Done";
            CheckInButton.BackgroundColor = GetColor("MobileSurfaceSunken", "#DDE9E5");
            CheckInStatusLabel.Text = "Checked in today!";
        }
        else
        {
            CheckInButton.IsEnabled = true;
            CheckInButton.Text = "Check In";
            CheckInButton.BackgroundColor = GetColor("MobilePrimary", "#0F766E");
            CheckInStatusLabel.Text = "Daily Check-in";
        }

        CheckInStreakLabel.Text = $"🔥 {profile.StreakDays} day streak!";
    }

    private async void OnCheckInClicked(object? sender, EventArgs e)
    {
        var xpEarned = _profileService.CheckInToday();
        if (xpEarned == 0)
        {
            UpdateCheckInStatus();
            return;
        }

        // Update UI
        UpdateCheckInStatus();
        await ShowXpGainAsync(xpEarned);
        await DisplayAlert("Checked In! ✅", $"You earned {xpEarned} XP! Keep your streak going!", "Awesome");
    }
    #endregion

    #region Quick Notes
    private int _savedNotesCount = 0;

    private void UpdateNotesCount()
    {
        // Count saved notes from preferences
        _savedNotesCount = 0;
        var allKeys = ScopedPreferences.Get("quick_note_keys", "").Split(',').Where(k => !string.IsNullOrEmpty(k)).ToList();
        _savedNotesCount = allKeys.Count;
        SavedNotesCountLabel.Text = $"{_savedNotesCount} saved";
    }

    private async void OnSaveQuickNoteClicked(object? sender, EventArgs e)
    {
        var noteText = QuickNoteEditor.Text?.Trim();
        if (string.IsNullOrWhiteSpace(noteText))
        {
            await DisplayAlert("Empty Note", "Please write something before saving!", "OK");
            return;
        }

        // Save note with timestamp
        var noteKey = $"quick_note_{DateTime.Now:yyyyMMdd_HHmmss}";
        ScopedPreferences.Set(noteKey, noteText);

        // Track note keys
        var existingKeys = ScopedPreferences.Get("quick_note_keys", "");
        var updatedKeys = string.IsNullOrEmpty(existingKeys) ? noteKey : $"{existingKeys},{noteKey}";
        ScopedPreferences.Set("quick_note_keys", updatedKeys);

        // Clear editor
        QuickNoteEditor.Text = string.Empty;

        // Update count and timestamp
        UpdateNotesCount();
        LastNoteTimeLabel.Text = "just now";

        // Award small XP for note-taking
        App.ProfileService.AddXP(2);
        await ShowXpGainAsync(2);

        await DisplayAlert("Saved ✨", "Your note has been saved. +2 XP!", "Great");
    }

    private async Task ShowXpGainAsync(int amount)
    {
        if (amount <= 0)
            return;

        var version = ++_xpToastVersion;
        XpToastLabel.Text = $"+{amount} XP";
        XpToast.IsVisible = true;
        XpToast.Opacity = 0;
        XpToast.TranslationY = -18;
        XpToast.Scale = 0.96;

        await Task.WhenAll(
            XpToast.FadeTo(1, 120, Easing.CubicOut),
            XpToast.TranslateTo(0, 0, 180, Easing.CubicOut),
            XpToast.ScaleTo(1, 180, Easing.CubicOut));

        await Task.Delay(800);
        if (version != _xpToastVersion)
            return;

        await Task.WhenAll(
            XpToast.FadeTo(0, 220, Easing.CubicIn),
            XpToast.TranslateTo(0, -18, 220, Easing.CubicIn));

        if (version == _xpToastVersion)
            XpToast.IsVisible = false;
    }

    private static Color GetColor(string key, string fallback)
    {
        return Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : Color.FromArgb(fallback);
    }
    #endregion
}
