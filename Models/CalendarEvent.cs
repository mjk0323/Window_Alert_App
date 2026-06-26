namespace Window_Alert_App.Models;

public class CalendarEvent
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Location { get; init; }
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public bool IsAllDay { get; init; }
    public string? RecurringEventId { get; init; }
    public int NotifyMinutesBefore { get; init; } = AppDefaults.DefaultNotifyMinutes;
}

public static class AppDefaults
{
    public const int DefaultNotifyMinutes = 10;
}
