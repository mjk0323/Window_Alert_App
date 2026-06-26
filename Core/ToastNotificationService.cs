using Microsoft.Toolkit.Uwp.Notifications;
using Window_Alert_App.Models;

namespace Window_Alert_App.Core;

public class ToastNotificationService
{
    public event Action<string>? SnoozeRequested;

    public void RegisterActivationHandler()
    {
        ToastNotificationManagerCompat.OnActivated += args =>
        {
            var arg = args.Argument;
            if (arg.StartsWith("snooze:"))
            {
                SnoozeRequested?.Invoke(arg);
            }
        };
    }

    public void ShowNotification(CalendarEvent ev, int minutesBefore)
    {
        try
        {
            var builder = new ToastContentBuilder()
                .AddText(ev.Title)
                .AddText($"{minutesBefore}분 후 시작 · {ev.StartTime:HH:mm}")
                .AddButton(new ToastButton("확인", "dismiss"))
                .AddButton(new ToastButton("5분 후 다시 알림", $"snooze:5:{ev.Id}"));

            if (!string.IsNullOrWhiteSpace(ev.Location))
                builder.AddText(ev.Location);

            builder.Show();
        }
        catch { }
    }

    public void ShowSnoozeNotification(CalendarEvent ev, int snoozeMinutes)
    {
        try
        {
            new ToastContentBuilder()
                .AddText(ev.Title)
                .AddText($"다시 알림 · {snoozeMinutes}분 후 시작")
                .AddButton(new ToastButton("확인", "dismiss"))
                .Show();
        }
        catch { }
    }
}
