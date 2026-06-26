using System.IO;
using System.Text.Json;
using Window_Alert_App.Models;

namespace Window_Alert_App.Settings;

public class LocalEventStore
{
    public static readonly LocalEventStore Instance = new();

    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WindowAlertApp", "local_events.json");

    private readonly List<CalendarEvent> _events;

    private LocalEventStore() => _events = Load();

    private static List<CalendarEvent> Load()
    {
        if (!File.Exists(FilePath)) return new();
        try { return JsonSerializer.Deserialize<List<CalendarEvent>>(File.ReadAllText(FilePath)) ?? new(); }
        catch { return new(); }
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(_events));
    }

    public CalendarEvent Add(string title, DateTime start, DateTime end,
        string? location, string? description, int notifyMinutes)
    {
        var ev = new CalendarEvent
        {
            Id = "local:" + Guid.NewGuid().ToString("N"),
            Title = title,
            StartTime = start,
            EndTime = end,
            Location = location,
            Description = description,
            NotifyMinutesBefore = notifyMinutes
        };
        lock (_events) { _events.Add(ev); Save(); }
        return ev;
    }

    public IReadOnlyList<CalendarEvent> GetUpcoming(int hours)
    {
        var from = DateTime.Today;
        var to = from.AddHours(hours);
        lock (_events)
            return _events.Where(e => e.StartTime >= from && e.StartTime <= to).ToList();
    }
}
