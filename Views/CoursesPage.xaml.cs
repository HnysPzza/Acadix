using AcadsJulie.Services;

namespace AcadsJulie.Views;

public partial class CoursesPage : ContentPage
{
    private readonly CourseService _courseService;

    public CoursesPage()
    {
        InitializeComponent();
        _courseService = App.CourseService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadCourses();
    }

    private void LoadCourses()
    {
        EnrolledCoursesCollection.ItemsSource = _courseService.GetEnrolledCourses();
        FeaturedCoursesCollection.ItemsSource = _courseService.GetFeaturedCourses();
        AllCoursesCollection.ItemsSource = _courseService.GetAllCourses();
    }

    private async void OnCourseCardTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is string courseId)
        {
            await Shell.Current.GoToAsync($"CourseDetailPage?courseId={courseId}");
        }
    }
}
