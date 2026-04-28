using System.Collections.ObjectModel;
using AcadsJulie.Models;
using AcadsJulie.Services;

namespace AcadsJulie.Views;

public partial class StudyAssistantPage : ContentPage
{
    private readonly ObservableCollection<ChatMessage> _messages = [];
    private readonly IStudyAssistantService _studyAssistantService;

    public StudyAssistantPage()
    {
        InitializeComponent();
        _studyAssistantService = App.StudyAssistantService;
        MessagesCollection.ItemsSource = _messages;
        _messages.Add(new ChatMessage
        {
            Role = "Assistant",
            Text = "Hi! I can help you prioritize tasks, suggest a study routine, or explain your strand/course recommendation.",
            SentAt = DateTime.Now
        });
    }

    private async void OnSendClicked(object? sender, EventArgs e)
    {
        var text = MessageEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return;

        _messages.Add(new ChatMessage
        {
            Role = "User",
            Text = text,
            SentAt = DateTime.Now
        });

        MessageEntry.Text = string.Empty;
        var response = await _studyAssistantService.SendMessageAsync(text);
        _messages.Add(response);

        if (_messages.Count > 0)
            MessagesCollection.ScrollTo(_messages[^1], position: ScrollToPosition.End, animate: true);
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
