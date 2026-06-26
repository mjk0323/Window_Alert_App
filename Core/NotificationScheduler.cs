using Microsoft.Win32;
using Window_Alert_App.Models;
using Window_Alert_App.Settings;

namespace Window_Alert_App.Core;

public class NotificationScheduler : IAsyncDisposable
{
    private readonly GoogleCalendarService _calendarService;
    private readonly ToastNotificationService _toastService;
    private readonly AppSettings _settings;

    private readonly Dictionary<string, CancellationTokenSource> _scheduledTasks = new();
    private readonly Dictionary<string, DateTime> _scheduledTimes = new();

    private CancellationTokenSource _cts = new();
    private Task? _pollingTask;

    public event Action<IReadOnlyList<CalendarEvent>>? EventsRefreshed;

    public NotificationScheduler(
        GoogleCalendarService calendarService,
        ToastNotificationService toastService,
        AppSettings settings)
    {
        _calendarService = calendarService;
        _toastService = toastService;
        _settings = settings;

        _toastService.SnoozeRequested += OnSnoozeRequested;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _pollingTask = RunPollingLoopAsync(_cts.Token);
        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _cts.Cancel();
        if (_pollingTask != null)
            await _pollingTask.ConfigureAwait(false);
    }

    public async Task RefreshNowAsync()
    {
        await PollAsync(_cts.Token);
    }

    private async Task RunPollingLoopAsync(CancellationToken ct)
    {
        try { await PollAsync(ct); }
        catch (OperationCanceledException) { return; }
        catch { }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_settings.PollingIntervalMinutes));
        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                try { await PollAsync(ct); }
                catch (OperationCanceledException) { return; }
                catch { }
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task PollAsync(CancellationToken ct)
    {
        var from = DateTime.Now;
        var to = from.AddHours(_settings.LookAheadHours);

        IReadOnlyList<CalendarEvent> events;
        if (_calendarService.IsAuthenticated)
            events = await _calendarService.GetUpcomingEventsAsync(from, to, ct);
        else
            events = Settings.LocalEventStore.Instance.GetUpcoming(_settings.LookAheadHours);

        EventsRefreshed?.Invoke(events);

        foreach (var ev in events)
        {
            if (ev.IsAllDay) continue;

            var notifyAt = ev.StartTime.AddMinutes(-ev.NotifyMinutesBefore);
            if (notifyAt <= DateTime.Now) continue;

            // 이미 동일 시각으로 스케줄된 경우 스킵
            if (_scheduledTasks.ContainsKey(ev.Id) &&
                _scheduledTimes.TryGetValue(ev.Id, out var prevTime) &&
                prevTime == notifyAt)
                continue;

            // 기존 스케줄 취소 후 재등록
            CancelScheduledTask(ev.Id);

            var taskCts = new CancellationTokenSource();
            _scheduledTasks[ev.Id] = taskCts;
            _scheduledTimes[ev.Id] = notifyAt;

            _ = ScheduleNotificationAsync(ev, notifyAt, taskCts.Token);
        }
    }

    private async Task ScheduleNotificationAsync(
        CalendarEvent ev, DateTime notifyAt, CancellationToken ct)
    {
        var delay = notifyAt - DateTime.Now;
        if (delay < TimeSpan.Zero) return;

        try
        {
            await Task.Delay(delay, ct);
            _toastService.ShowNotification(ev, ev.NotifyMinutesBefore);
            CleanupScheduledTask(ev.Id);
        }
        catch (OperationCanceledException) { }
    }

    private void OnSnoozeRequested(string arg)
    {
        // arg: "snooze:5:eventId"
        var parts = arg.Split(':');
        if (parts.Length < 3) return;
        if (!int.TryParse(parts[1], out var minutes)) return;
        var eventId = parts[2];

        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(minutes));

            // 해당 이벤트를 캐시에서 찾아 알림 재발송
            if (_scheduledTimes.TryGetValue(eventId, out _))
            {
                // 단순히 re-toast (이벤트 정보를 보관하지 않으므로 generic 알림)
                _toastService.ShowSnoozeNotification(
                    new CalendarEvent { Id = eventId, Title = "다시 알림", StartTime = DateTime.Now.AddMinutes(minutes) },
                    minutes);
            }
        });
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            Task.Run(() => PollAsync(_cts.Token));
        }
    }

    private void CancelScheduledTask(string eventId)
    {
        if (_scheduledTasks.TryGetValue(eventId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
            _scheduledTasks.Remove(eventId);
            _scheduledTimes.Remove(eventId);
        }
    }

    private void CleanupScheduledTask(string eventId)
    {
        if (_scheduledTasks.TryGetValue(eventId, out var cts))
        {
            cts.Dispose();
            _scheduledTasks.Remove(eventId);
            _scheduledTimes.Remove(eventId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _toastService.SnoozeRequested -= OnSnoozeRequested;

        _cts.Cancel();

        foreach (var cts in _scheduledTasks.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }
        _scheduledTasks.Clear();
        _scheduledTimes.Clear();

        if (_pollingTask != null)
        {
            try { await _pollingTask.WaitAsync(TimeSpan.FromSeconds(3)); }
            catch { }
        }
    }
}
