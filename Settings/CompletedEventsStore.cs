using System.IO;
using System.Text.Json;

namespace Window_Alert_App.Settings;

public class CompletedEventsStore
{
    private static CompletedEventsStore? _instance;
    public static CompletedEventsStore Instance => _instance ??= new CompletedEventsStore();

    private readonly string _filePath;
    private Dictionary<string, DateTime> _completedEvents = new();

    private CompletedEventsStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WindowAlertApp");
        _filePath = Path.Combine(dir, "completed_events.json");
        Load();
    }

    public bool IsCompleted(string eventId) =>
        _completedEvents.ContainsKey(eventId);

    public void SetCompleted(string eventId, bool completed, DateTime eventEndTime)
    {
        if (completed)
            _completedEvents[eventId] = eventEndTime;
        else
            _completedEvents.Remove(eventId);

        PurgeExpired();
        Save();
    }

    private void PurgeExpired()
    {
        var cutoff = DateTime.Now.AddDays(-1);
        var expired = _completedEvents
            .Where(kv => kv.Value < cutoff)
            .Select(kv => kv.Key)
            .ToList();
        foreach (var key in expired)
            _completedEvents.Remove(key);
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return;
            var json = File.ReadAllText(_filePath);
            _completedEvents = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(json)
                               ?? new Dictionary<string, DateTime>();
        }
        catch { _completedEvents = new Dictionary<string, DateTime>(); }
    }

    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_filePath)!;
            Directory.CreateDirectory(dir);
            var tmp = _filePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(_completedEvents));
            File.Move(tmp, _filePath, overwrite: true);
        }
        catch { }
    }
}
