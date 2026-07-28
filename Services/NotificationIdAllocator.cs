namespace AcadsJulie.Services;

/// <summary>
/// Hands out non-overlapping notification ID blocks for scheduled reminders.
///
/// Each task reserves four consecutive IDs (base, +1, +2, +3) for its "due soon", "due now",
/// "overdue" and "custom reminder" notifications, so IDs must be allocated in strides of four.
/// The previous approach picked a random number in 10000..99999, which meant two tasks could
/// land close enough to overwrite each other's reminders — and could collide with the
/// hard-coded IDs below.
/// </summary>
public static class NotificationIdAllocator
{
    /// <summary>Daily streak reminder (scheduled from App).</summary>
    public const int DailyStreakId = 1;

    /// <summary>Daily "review your tasks" check.</summary>
    public const int DailyOverdueCheckId = 2;

    /// <summary>Task blocks start well above the reserved fixed IDs.</summary>
    private const int FirstTaskBlock = 1000;

    private const int BlockSize = 4;
    private const int MaxId = 1_000_000;
    private const string CounterKey = "notification_id_counter";

    private static readonly object Gate = new();

    /// <summary>
    /// Reserves the next free block of four IDs and returns its base. The counter is device-wide
    /// (not per account) so reminders belonging to different users can never share an ID.
    /// </summary>
    public static int Next()
    {
        lock (Gate)
        {
            var next = Preferences.Default.Get(CounterKey, FirstTaskBlock);

            // Wrap long before int overflow; by then the early blocks are long gone.
            if (next >= MaxId)
                next = FirstTaskBlock;

            Preferences.Default.Set(CounterKey, next + BlockSize);
            return next;
        }
    }
}
