using System.IO;
using System.Reflection;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Window_Alert_App.Models;
using Window_Alert_App.Settings;

namespace Window_Alert_App.Core;

public class GoogleCalendarService : IDisposable
{
    private static readonly string[] Scopes = { CalendarService.Scope.Calendar };
    private const string AppName = "WindowAlertApp";

    private CalendarService? _service;
    private UserCredential? _credential;
    private bool _isAuthenticated;

    public bool IsAuthenticated => _isAuthenticated;

    public event EventHandler<bool>? AuthenticationChanged;

    private string TokenStorePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "WindowAlertApp", "token_store");

    private string SecretsPath => Path.Combine(
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
        "Secrets", "client_secrets.json");

    public async Task<bool> AuthenticateAsync(CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(SecretsPath))
                return false;

            await using var stream = new FileStream(SecretsPath, FileMode.Open, FileAccess.Read);
            _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                Scopes,
                "user",
                ct,
                new FileDataStore(TokenStorePath, fullPath: true));

            _service = new CalendarService(new BaseClientService.Initializer
            {
                HttpClientInitializer = _credential,
                ApplicationName = AppName
            });

            _isAuthenticated = true;

            var settings = SettingsManager.Instance.Load();
            settings.IsGoogleAuthenticated = true;
            SettingsManager.Instance.Save(settings);

            AuthenticationChanged?.Invoke(this, true);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch
        {
            _isAuthenticated = false;
            AuthenticationChanged?.Invoke(this, false);
            return false;
        }
    }

    public async Task RevokeAuthAsync()
    {
        if (_credential != null)
        {
            await _credential.RevokeTokenAsync(CancellationToken.None);
            _credential = null;
        }

        // 토큰 파일 삭제
        if (Directory.Exists(TokenStorePath))
        {
            foreach (var f in Directory.GetFiles(TokenStorePath))
                File.Delete(f);
        }

        _service?.Dispose();
        _service = null;
        _isAuthenticated = false;

        var settings = SettingsManager.Instance.Load();
        settings.IsGoogleAuthenticated = false;
        SettingsManager.Instance.Save(settings);

        AuthenticationChanged?.Invoke(this, false);
    }

    public string? GetAuthenticatedEmail()
    {
        return _credential?.UserId;
    }

    public async Task<IReadOnlyList<CalendarEvent>> GetUpcomingEventsAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        if (_service == null || !_isAuthenticated)
            return Array.Empty<CalendarEvent>();

        try
        {
            var request = _service.Events.List("primary");
            request.TimeMinDateTimeOffset = from;
            request.TimeMaxDateTimeOffset = to;
            request.SingleEvents = true;
            request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
            request.MaxResults = 250;

            var response = await request.ExecuteAsync(ct);
            var results = new List<CalendarEvent>();

            foreach (var item in response.Items ?? Enumerable.Empty<Event>())
            {
                if (item.Status == "cancelled") continue;

                bool isAllDay = item.Start?.Date != null;
                DateTime start, end;

                if (isAllDay)
                {
                    start = DateTime.Parse(item.Start!.Date!);
                    end = DateTime.Parse(item.End?.Date ?? item.Start.Date!);
                }
                else
                {
                    start = (item.Start?.DateTimeDateTimeOffset ?? DateTimeOffset.Now).LocalDateTime;
                    end = (item.End?.DateTimeDateTimeOffset ?? DateTimeOffset.Now).LocalDateTime;
                }

                int notifyMinutes = AppDefaults.DefaultNotifyMinutes;
                if (item.Reminders?.UseDefault == false &&
                    item.Reminders.Overrides?.Count > 0)
                {
                    notifyMinutes = (int)(item.Reminders.Overrides[0].Minutes ?? AppDefaults.DefaultNotifyMinutes);
                }

                results.Add(new CalendarEvent
                {
                    Id = item.Id ?? string.Empty,
                    Title = item.Summary ?? "(제목 없음)",
                    Description = item.Description,
                    Location = item.Location,
                    StartTime = start,
                    EndTime = end,
                    IsAllDay = isAllDay,
                    RecurringEventId = item.RecurringEventId,
                    NotifyMinutesBefore = notifyMinutes
                });
            }

            return results;
        }
        catch (OperationCanceledException)
        {
            return Array.Empty<CalendarEvent>();
        }
        catch
        {
            return Array.Empty<CalendarEvent>();
        }
    }

    public async Task<CalendarEvent?> CreateEventAsync(
        string title, DateTime start, DateTime end,
        string? location, string? description,
        int notifyMinutes, CancellationToken ct = default)
    {
        if (_service == null) return null;

        var ev = new Event
        {
            Summary = title,
            Location = location,
            Description = description,
            Start = new EventDateTime
            {
                DateTimeDateTimeOffset = new DateTimeOffset(start, TimeZoneInfo.Local.GetUtcOffset(start))
            },
            End = new EventDateTime
            {
                DateTimeDateTimeOffset = new DateTimeOffset(end, TimeZoneInfo.Local.GetUtcOffset(end))
            },
            Reminders = new Event.RemindersData
            {
                UseDefault = false,
                Overrides = new List<EventReminder>
                {
                    new EventReminder { Method = "popup", Minutes = notifyMinutes }
                }
            }
        };

        var created = await _service.Events.Insert(ev, "primary").ExecuteAsync(ct);

        return created == null ? null : new CalendarEvent
        {
            Id = created.Id ?? string.Empty,
            Title = created.Summary ?? title,
            Description = created.Description,
            Location = created.Location,
            StartTime = start,
            EndTime = end,
            NotifyMinutesBefore = notifyMinutes
        };
    }

    public async Task<bool> UpdateEventAsync(
        string eventId, string title, DateTime start, DateTime end,
        string? location, string? description,
        int notifyMinutes, CancellationToken ct = default)
    {
        if (_service == null) return false;

        var ev = new Event
        {
            Summary = title,
            Location = location,
            Description = description,
            Start = new EventDateTime
            {
                DateTimeDateTimeOffset = new DateTimeOffset(start, TimeZoneInfo.Local.GetUtcOffset(start))
            },
            End = new EventDateTime
            {
                DateTimeDateTimeOffset = new DateTimeOffset(end, TimeZoneInfo.Local.GetUtcOffset(end))
            },
            Reminders = new Event.RemindersData
            {
                UseDefault = false,
                Overrides = new List<EventReminder>
                {
                    new EventReminder { Method = "popup", Minutes = notifyMinutes }
                }
            }
        };

        await _service.Events.Update(ev, "primary", eventId).ExecuteAsync(ct);
        return true;
    }

    public async Task<bool> DeleteEventAsync(string eventId, CancellationToken ct = default)
    {
        if (_service == null) return false;
        await _service.Events.Delete("primary", eventId).ExecuteAsync(ct);
        return true;
    }

    public void Dispose()
    {
        _service?.Dispose();
    }
}
