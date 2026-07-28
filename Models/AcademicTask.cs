namespace AcadsJulie.Models;

public class AcademicTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Base ID for this task's four reminders (base, +1, +2, +3).
    /// </summary>
    /// <remarks>
    /// Previously <c>Random.Shared.Next(10000, 99999)</c>, which collided in two ways: two tasks
    /// could draw adjacent numbers and cancel each other's reminders, and the range included the
    /// hard-coded 99999 daily-check ID. Allocated from a persisted counter in stride-4 blocks
    /// instead — see <see cref="Services.NotificationIdAllocator"/>.
    /// </remarks>
    public int NotificationId { get; set; } = Services.NotificationIdAllocator.Next();
    public string Title { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime DueDate { get; set; } = DateTime.Now.AddDays(1);
    public DateTime? ReminderTime { get; set; }
    public string Priority { get; set; } = "Medium";
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? CompletedAt { get; set; }

    public bool IsOverdue => !IsCompleted && DueDate < DateTime.Now;
    public string StatusLabel => IsCompleted ? "Completed" : IsOverdue ? "Overdue" : "Pending";
    public string DueDateLabel => DueDate.ToString("MMM dd, yyyy hh:mm tt");
    public string ReminderLabel => ReminderTime?.ToString("MMM dd, yyyy hh:mm tt") ?? "No reminder";
    public string SubjectLabel => string.IsNullOrWhiteSpace(Subject) ? "General" : Subject;
}

public class AcademicTaskSummary
{
    public int TotalTasks { get; set; }
    public int PendingTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int DueTodayTasks { get; set; }
    public int OverdueTasks { get; set; }
}
