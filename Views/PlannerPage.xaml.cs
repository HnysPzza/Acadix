using AcadsJulie.Models;
using AcadsJulie.Services;

namespace AcadsJulie.Views;

public partial class PlannerPage : ContentPage
{
    private readonly TaskService _taskService;

    public PlannerPage()
    {
        InitializeComponent();
        _taskService = App.TaskService;
        PriorityPicker.SelectedIndex = 1;
        DueDatePicker.Date = DateTime.Today;
        DueTimePicker.Time = DateTime.Now.AddHours(2).TimeOfDay;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        LoadTasks();
    }

    private void LoadTasks()
    {
        var summary = _taskService.GetSummary();
        PendingTasksLabel.Text = summary.PendingTasks.ToString();
        DueTodayLabel.Text = summary.DueTodayTasks.ToString();
        OverdueLabel.Text = summary.OverdueTasks.ToString();
        TasksCollection.ItemsSource = _taskService.GetSortedTasks();
    }

    private async void OnAddTaskClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleEntry.Text))
        {
            await DisplayAlert("Missing title", "Please enter a task title.", "OK");
            return;
        }

        var selectedDate = DueDatePicker.Date ?? DateTime.Today;
        var selectedTime = DueTimePicker.Time ?? TimeSpan.FromHours(17);
        var dueDate = new DateTime(selectedDate.Year, selectedDate.Month, selectedDate.Day).Add(selectedTime);
        DateTime? reminder = ReminderSwitch.IsToggled ? dueDate.AddHours(-2) : null;
        if (reminder != null && reminder <= DateTime.Now)
            reminder = DateTime.Now.AddMinutes(1);

        _taskService.AddTask(new AcademicTask
        {
            Title = TitleEntry.Text.Trim(),
            Subject = SubjectEntry.Text?.Trim() ?? string.Empty,
            Notes = NotesEditor.Text?.Trim() ?? string.Empty,
            DueDate = dueDate,
            ReminderTime = reminder,
            Priority = PriorityPicker.SelectedItem?.ToString() ?? "Medium"
        });

        TitleEntry.Text = string.Empty;
        SubjectEntry.Text = string.Empty;
        NotesEditor.Text = string.Empty;
        PriorityPicker.SelectedIndex = 1;
        ReminderSwitch.IsToggled = true;

        LoadTasks();
    }

    private void OnToggleTaskClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string id)
        {
            _taskService.ToggleCompleted(id);
            LoadTasks();
        }
    }

    private async void OnDeleteTaskClicked(object? sender, EventArgs e)
    {
        if (sender is not Button button || button.CommandParameter is not string id)
            return;

        var confirmed = await DisplayAlert("Delete task", "Remove this academic task?", "Delete", "Cancel");
        if (!confirmed)
            return;

        _taskService.DeleteTask(id);
        LoadTasks();
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void OnRefreshClicked(object? sender, EventArgs e)
    {
        LoadTasks();
    }

    private void OnReminderToggled(object? sender, ToggledEventArgs e)
    {
        NotificationInfoBorder.IsVisible = e.Value;
    }
}
