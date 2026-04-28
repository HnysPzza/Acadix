using AcadsJulie.Models;
using AcadsJulie.Services;

namespace AcadsJulie.Views;

[QueryProperty(nameof(CourseId), "courseId")]
public partial class CourseDetailPage : ContentPage
{
    private readonly CourseService _courseService;
    private LearningPath? _course;
    private string _courseId = string.Empty;

    public string CourseId
    {
        get => _courseId;
        set
        {
            _courseId = value;
            LoadCourse();
        }
    }

    public CourseDetailPage()
    {
        InitializeComponent();
        _courseService = App.CourseService;
    }

    private void LoadCourse()
    {
        _course = _courseService.GetCourseById(CourseId);
        if (_course == null)
            return;

        CourseEmojiLabel.Text = _course.ThumbnailEmoji;
        CourseTitleLabel.Text = _course.Title;
        CourseDescriptionLabel.Text = _course.Description;
        CourseCategoryLabel.Text = _course.Category;
        CourseDifficultyLabel.Text = _course.DifficultyLevel;
        CourseDurationLabel.Text = _course.DurationLabel;
        LessonsCollection.ItemsSource = _course.Lessons;

        var progress = _courseService.GetProgress(CourseId);
        var isEnrolled = _courseService.GetEnrolledCourses().Any(c => c.Id == CourseId);

        if (isEnrolled)
        {
            EnrollButton.IsVisible = false;
            ProgressCard.IsVisible = true;
            var percentage = progress.ProgressPercentage(_course.TotalLessons);
            CourseProgressBar.Progress = percentage / 100.0;
            ProgressLabel.Text = progress.ProgressLabel(_course.TotalLessons);
        }
        else
        {
            EnrollButton.IsVisible = true;
            ProgressCard.IsVisible = false;
        }
    }

    private void OnEnrollClicked(object? sender, EventArgs e)
    {
        if (_course == null)
            return;

        _courseService.EnrollInCourse(CourseId);
        LoadCourse();
        DisplayAlert("Enrolled!", $"You're now enrolled in {_course.Title}", "OK");
    }

    private async void OnLessonTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is string lessonId && _course != null)
        {
            var isEnrolled = _courseService.GetEnrolledCourses().Any(c => c.Id == CourseId);
            if (!isEnrolled)
            {
                await DisplayAlert("Enroll First", "Please enroll in this course to access lessons", "OK");
                return;
            }

            await Shell.Current.GoToAsync($"LessonViewerPage?courseId={CourseId}&lessonId={lessonId}");
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//LearningHubPage/CoursesPage");
    }
}
