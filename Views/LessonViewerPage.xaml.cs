using AcadsJulie.Models;
using AcadsJulie.Services;

namespace AcadsJulie.Views;

[QueryProperty(nameof(CourseId), "courseId")]
[QueryProperty(nameof(LessonId), "lessonId")]
public partial class LessonViewerPage : ContentPage
{
    private readonly CourseService _courseService;
    private LearningPath? _course;
    private Lesson? _lesson;
    private string _courseId = string.Empty;
    private string _lessonId = string.Empty;

    public string CourseId
    {
        get => _courseId;
        set
        {
            _courseId = value;
            LoadLesson();
        }
    }

    public string LessonId
    {
        get => _lessonId;
        set
        {
            _lessonId = value;
            LoadLesson();
        }
    }

    public LessonViewerPage()
    {
        InitializeComponent();
        _courseService = App.CourseService;
    }

    private void LoadLesson()
    {
        if (string.IsNullOrEmpty(CourseId) || string.IsNullOrEmpty(LessonId))
            return;

        _course = _courseService.GetCourseById(CourseId);
        if (_course == null)
            return;

        _lesson = _course.Lessons.FirstOrDefault(l => l.Id == LessonId);
        if (_lesson == null)
            return;

        LessonTitleLabel.Text = _lesson.Title;
        LessonDurationLabel.Text = _lesson.DurationLabel;
        LessonContentLabel.Text = _lesson.Content;

        if (_lesson.KeyConcepts.Count > 0)
        {
            KeyConceptsCard.IsVisible = true;
            KeyConceptsStack.Children.Clear();
            foreach (var concept in _lesson.KeyConcepts)
            {
                var label = new Label
                {
                    Text = $"• {concept}",
                    FontSize = 13,
                    TextColor = GetColor("TextSecondary", "#59686A")
                };
                KeyConceptsStack.Children.Add(label);
            }
        }

        var progress = _courseService.GetProgress(CourseId);
        var isCompleted = progress.CompletedLessonIds.Contains(LessonId);

        if (isCompleted)
        {
            CompleteButton.Text = "✓ Completed";
            CompleteButton.BackgroundColor = GetColor("MobileSurfaceSunken", "#DDE9E5");
            CompleteButton.IsEnabled = false;
        }

        var currentIndex = _course.Lessons.FindIndex(l => l.Id == LessonId);
        if (currentIndex >= 0 && currentIndex < _course.Lessons.Count - 1)
        {
            NextLessonButton.IsVisible = true;
        }
    }

    private async void OnCompleteClicked(object? sender, EventArgs e)
    {
        if (_lesson == null || _course == null)
            return;

        _courseService.CompleteLesson(CourseId, LessonId);
        _courseService.AddStudyTime(CourseId, _lesson.EstimatedMinutes);

        CompleteButton.Text = "✓ Completed";
        CompleteButton.BackgroundColor = GetColor("MobileSurfaceSunken", "#DDE9E5");
        CompleteButton.IsEnabled = false;

        var progress = _courseService.GetProgress(CourseId);
        if (progress.IsCompleted)
        {
            await DisplayAlert("Course Completed! 🎉", 
                $"Congratulations! You've completed {_course.Title}. You earned 100 XP!", 
                "Awesome");
        }
        else
        {
            await DisplayAlert("Lesson Complete! ✓", 
                "Great job! You earned 20 XP. Keep learning!", 
                "Continue");
        }
    }

    private async void OnNextLessonClicked(object? sender, EventArgs e)
    {
        if (_course == null)
            return;

        var currentIndex = _course.Lessons.FindIndex(l => l.Id == LessonId);
        if (currentIndex >= 0 && currentIndex < _course.Lessons.Count - 1)
        {
            var nextLesson = _course.Lessons[currentIndex + 1];
            await Shell.Current.GoToAsync($"LessonViewerPage?courseId={CourseId}&lessonId={nextLesson.Id}");
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//LearningHubPage/CoursesPage/CourseDetailPage?courseId={CourseId}");
    }

    private static Color GetColor(string key, string fallback)
    {
        return Application.Current?.Resources.TryGetValue(key, out var value) == true && value is Color color
            ? color
            : Color.FromArgb(fallback);
    }
}
