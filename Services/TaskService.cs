using System.Text.Json;
using AcadsJulie.Models;

namespace AcadsJulie.Services;

public class TaskService
{
    private const string TasksKey = "academic_tasks";
    private List<AcademicTask>? _cachedTasks;
    private readonly TaskNotificationService _notificationService;

    public List<AcademicTask> GetTasks()
    {
        if (_cachedTasks != null)
            return _cachedTasks;

        var json = Preferences.Get(TasksKey, null);
        _cachedTasks = json == null
            ? []
            : JsonSerializer.Deserialize<List<AcademicTask>>(json) ?? [];

        return _cachedTasks;
    }

    public List<AcademicTask> GetSortedTasks()
    {
        return GetTasks()
            .OrderBy(t => t.IsCompleted)
            .ThenBy(t => t.DueDate)
            .ThenByDescending(t => GetPriorityWeight(t.Priority))
            .ToList();
    }

    public List<AcademicTask> GetTasksDueToday()
    {
        var today = DateTime.Today;
        return GetSortedTasks()
            .Where(t => !t.IsCompleted && t.DueDate.Date == today)
            .ToList();
    }

    public AcademicTaskSummary GetSummary()
    {
        var tasks = GetTasks();
        var today = DateTime.Today;

        return new AcademicTaskSummary
        {
            TotalTasks = tasks.Count,
            PendingTasks = tasks.Count(t => !t.IsCompleted),
            CompletedTasks = tasks.Count(t => t.IsCompleted),
            DueTodayTasks = tasks.Count(t => !t.IsCompleted && t.DueDate.Date == today),
            OverdueTasks = tasks.Count(t => !t.IsCompleted && t.DueDate < DateTime.Now)
        };
    }

    public TaskService()
    {
        _notificationService = new TaskNotificationService(App.ProfileService);
    }

    public AcademicTask AddTask(AcademicTask task)
    {
        var tasks = GetTasks();
        tasks.Add(task);
        SaveTasks(tasks);
        _notificationService.ScheduleTaskNotifications(task);
        return task;
    }

    public void UpdateTask(AcademicTask task)
    {
        var tasks = GetTasks();
        var existing = tasks.FirstOrDefault(t => t.Id == task.Id);
        if (existing == null)
        {
            tasks.Add(task);
        }
        else
        {
            existing.Title = task.Title;
            existing.Subject = task.Subject;
            existing.Notes = task.Notes;
            existing.DueDate = task.DueDate;
            existing.ReminderTime = task.ReminderTime;
            existing.Priority = task.Priority;
            existing.IsCompleted = task.IsCompleted;
            existing.CompletedAt = task.CompletedAt;
        }

        SaveTasks(tasks);
        _notificationService.ScheduleTaskNotifications(task);
    }

    public void ToggleCompleted(string taskId)
    {
        var task = GetTasks().FirstOrDefault(t => t.Id == taskId);
        if (task == null)
            return;

        task.IsCompleted = !task.IsCompleted;
        task.CompletedAt = task.IsCompleted ? DateTime.Now : null;
        SaveTasks(GetTasks());

        if (task.IsCompleted)
            _notificationService.CancelTaskNotifications(task);
        else
            _notificationService.ScheduleTaskNotifications(task);
    }

    public void DeleteTask(string taskId)
    {
        var tasks = GetTasks();
        var task = tasks.FirstOrDefault(t => t.Id == taskId);
        if (task == null)
            return;

        tasks.Remove(task);
        SaveTasks(tasks);
        _notificationService.CancelTaskNotifications(task);
    }

    public void ScheduleAllPendingReminders()
    {
        foreach (var task in GetTasks().Where(t => !t.IsCompleted))
            _notificationService.ScheduleTaskNotifications(task);
        
        _notificationService.ScheduleDailyOverdueCheck();
    }

    private void SaveTasks(List<AcademicTask> tasks)
    {
        _cachedTasks = tasks;
        Preferences.Set(TasksKey, JsonSerializer.Serialize(tasks));
    }

    private static int GetPriorityWeight(string priority)
    {
        return priority switch
        {
            "High" => 3,
            "Medium" => 2,
            _ => 1
        };
    }
}
