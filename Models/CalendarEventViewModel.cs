using CommunityToolkit.Mvvm.ComponentModel;
using Window_Alert_App.Settings;

namespace Window_Alert_App.Models;

public partial class CalendarEventViewModel : ObservableObject
{
    public CalendarEvent Event { get; }

    public string Id => Event.Id;
    public string Title => Event.Title;
    public string? Description => Event.Description;
    public string? Location => Event.Location;
    public DateTime StartTime => Event.StartTime;
    public DateTime EndTime => Event.EndTime;
    public bool IsAllDay => Event.IsAllDay;
    public int NotifyMinutesBefore => Event.NotifyMinutesBefore;

    [ObservableProperty]
    private bool _isCompleted;

    // 시작까지 10분 이내면 긴급 표시
    public bool IsUrgent =>
        !IsCompleted &&
        StartTime > DateTime.Now &&
        (StartTime - DateTime.Now).TotalMinutes <= 10;

    public CalendarEventViewModel(CalendarEvent ev)
    {
        Event = ev;
        _isCompleted = CompletedEventsStore.Instance.IsCompleted(ev.Id);
    }

    partial void OnIsCompletedChanged(bool value)
    {
        CompletedEventsStore.Instance.SetCompleted(Event.Id, value, Event.EndTime);
        OnPropertyChanged(nameof(IsUrgent));
    }
}
