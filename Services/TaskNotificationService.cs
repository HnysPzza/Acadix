using AcadsJulie.Models;
using Plugin.LocalNotification;

namespace AcadsJulie.Services;

public class TaskNotificationService
{
    private readonly ProfileService _profileService;

    public TaskNotificationService(ProfileService profileService)
    {
        _profileService = profileService;
    }

    public void ScheduleTaskNotifications(AcademicTask task)
    {
        if (!_profileService.GetProfile().NotificationsEnabled)
            return;

        if (task.IsCompleted)
            return;

        CancelTaskNotifications(task);

        var now = DateTime.Now;
        var dueDate = task.DueDate;

        var nearTime = dueDate.AddHours(-2);
        var currentTime = dueDate;
        var pastTime = dueDate.AddMinutes(30);

        if (nearTime > now)
        {
            ScheduleNotification(
                task.NotificationId,
                "📌 Task Due Soon",
                $"{task.Title} is due in 2 hours",
                GetNotificationBody(task, "2 hours remaining"),
                nearTime,
                task.Id
            );
        }

        if (currentTime > now)
        {
            ScheduleNotification(
                task.NotificationId + 1,
                "⏰ Task Due Now",
                $"{task.Title} is due now!",
                GetNotificationBody(task, "Due now"),
                currentTime,
                task.Id
            );
        }

        if (pastTime > now)
        {
            ScheduleNotification(
                task.NotificationId + 2,
                "⚠️ Task Overdue",
                $"{task.Title} is overdue",
                GetNotificationBody(task, "30 minutes overdue"),
                pastTime,
                task.Id
            );
        }

        if (task.ReminderTime.HasValue && task.ReminderTime.Value > now)
        {
            ScheduleNotification(
                task.NotificationId + 3,
                "🔔 Custom Reminder",
                $"Reminder: {task.Title}",
                GetNotificationBody(task, $"Due {task.DueDate:MMM dd, hh:mm tt}"),
                task.ReminderTime.Value,
                task.Id
            );
        }
    }

    public void CancelTaskNotifications(AcademicTask task)
    {
        LocalNotificationCenter.Current.Cancel(task.NotificationId);
        LocalNotificationCenter.Current.Cancel(task.NotificationId + 1);
        LocalNotificationCenter.Current.Cancel(task.NotificationId + 2);
        LocalNotificationCenter.Current.Cancel(task.NotificationId + 3);
    }

    public void ScheduleDailyOverdueCheck()
    {
        if (!_profileService.GetProfile().NotificationsEnabled)
            return;

        var tomorrow = DateTime.Today.AddDays(1).AddHours(8);
        
        var request = new NotificationRequest
        {
            NotificationId = NotificationIdAllocator.DailyOverdueCheckId,
            Title = "📚 Daily Academic Check",
            Description = "Review your pending tasks and deadlines",
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = tomorrow,
                RepeatType = NotificationRepeat.Daily
            }
        };

        App.ShowNotificationWhenAllowed(request);
    }

    private void ScheduleNotification(
        int notificationId,
        string title,
        string subtitle,
        string description,
        DateTime notifyTime,
        string taskId)
    {
        var request = new NotificationRequest
        {
            NotificationId = notificationId,
            Title = title,
            Subtitle = subtitle,
            Description = description,
            ReturningData = taskId,
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = notifyTime
            },
            CategoryType = NotificationCategoryType.Reminder
        };

        App.ShowNotificationWhenAllowed(request);
    }

    private string GetNotificationBody(AcademicTask task, string timeInfo)
    {
        var parts = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(task.Subject))
            parts.Add($"Subject: {task.Subject}");
        
        parts.Add(timeInfo);
        
        if (task.Priority == "High")
            parts.Add("⭐ High Priority");

        return string.Join(" • ", parts);
    }
}
