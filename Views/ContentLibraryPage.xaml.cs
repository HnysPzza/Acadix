using AcadsJulie.Services;

namespace AcadsJulie.Views;

public partial class ContentLibraryPage : ContentPage
{
    private readonly ContentLibraryService _libraryService;

    public ContentLibraryPage()
    {
        InitializeComponent();
        _libraryService = App.ContentLibraryService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadContent();
    }

    private void LoadContent()
    {
        ContentCollection.ItemsSource = _libraryService.GetAllContent();
    }

    private async void OnContentTapped(object? sender, TappedEventArgs e)
    {
        if (e.Parameter is string contentId)
        {
            await Shell.Current.GoToAsync($"ContentViewerPage?contentId={contentId}");
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//LearningHubPage");
    }
}
