using AcadsJulie.Services;

namespace AcadsJulie.Views;

[QueryProperty(nameof(ContentId), "contentId")]
public partial class ContentViewerPage : ContentPage
{
    private readonly ContentLibraryService _libraryService;
    private string _contentId = string.Empty;

    public string ContentId
    {
        get => _contentId;
        set
        {
            _contentId = value;
            LoadContent();
        }
    }

    public ContentViewerPage()
    {
        InitializeComponent();
        _libraryService = App.ContentLibraryService;
    }

    private void LoadContent()
    {
        var content = _libraryService.GetContentById(ContentId);
        if (content == null) return;

        TitleLabel.Text = content.Title;
        CategoryLabel.Text = content.Category;
        ContentLabel.Text = content.Content;
        BookmarkButton.Text = content.IsBookmarked ? "🔖" : "📑";
    }

    private void OnBookmarkClicked(object? sender, EventArgs e)
    {
        _libraryService.ToggleBookmark(ContentId);
        LoadContent();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//LearningHubPage/ContentLibraryPage");
    }
}
