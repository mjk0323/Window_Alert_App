using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Window_Alert_App.Models;
using Window_Alert_App.Settings;

namespace Window_Alert_App.Widget;

public partial class WidgetViewModel : ObservableObject
{
    [ObservableProperty] private IReadOnlyList<CalendarEventViewModel> _events = Array.Empty<CalendarEventViewModel>();
    [ObservableProperty] private DateTime _selectedDate = DateTime.Today;
    [ObservableProperty] private WidgetMode _currentMode;
    [ObservableProperty] private bool _isAuthenticated;
    [ObservableProperty] private DateTime _calendarMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    public event Action<WidgetMode>? ModeChangeRequested;
    public event Action? RefreshRequested;

    public IReadOnlyList<CalendarEventViewModel> SelectedDayEvents =>
        Events.Where(e => e.StartTime.Date == SelectedDate.Date && !e.IsAllDay).ToList();

    public WidgetViewModel(AppSettings settings)
    {
        _currentMode = settings.Mode;
    }

    public void UpdateEvents(IReadOnlyList<CalendarEvent> events)
    {
        // 기존 완료 상태 보존: ID가 같으면 기존 VM 재사용
        var dict = Events.ToDictionary(vm => vm.Id);
        Events = events
            .Select(e => dict.TryGetValue(e.Id, out var existing) ? existing : new CalendarEventViewModel(e))
            .ToList();
        OnPropertyChanged(nameof(SelectedDayEvents));
    }

    partial void OnSelectedDateChanged(DateTime value)
    {
        OnPropertyChanged(nameof(SelectedDayEvents));
    }

    [RelayCommand]
    private void PreviousDay() => SelectedDate = SelectedDate.AddDays(-1);

    [RelayCommand]
    private void NextDay() => SelectedDate = SelectedDate.AddDays(1);

    [RelayCommand]
    private void PreviousMonth() => CalendarMonth = CalendarMonth.AddMonths(-1);

    [RelayCommand]
    private void NextMonth() => CalendarMonth = CalendarMonth.AddMonths(1);

    [RelayCommand]
    private void SwitchMode(string modeStr)
    {
        if (Enum.TryParse<WidgetMode>(modeStr, out var mode))
            ModeChangeRequested?.Invoke(mode);
    }

    [RelayCommand]
    private void RequestRefresh() => RefreshRequested?.Invoke();

    public bool HasEventsOnDate(DateTime date) =>
        Events.Any(e => e.StartTime.Date == date.Date && !e.IsAllDay);

    public IReadOnlyList<CalendarEventViewModel> GetEventsForDate(DateTime date) =>
        Events.Where(e => e.StartTime.Date == date.Date && !e.IsAllDay).ToList();
}
