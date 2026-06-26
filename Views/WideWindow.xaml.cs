using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Window_Alert_App.Core;
using Window_Alert_App.Models;
using Window_Alert_App.Settings;
using Window_Alert_App.Widget;

namespace Window_Alert_App.Views;

public partial class WideWindow : Window
{
    private readonly WidgetViewModel _vm;
    private readonly GoogleCalendarService _calendarService;
    private DateTime _selectedDate = DateTime.Today;

    public event Action<WidgetMode>? ModeChangeRequested;

    public WideWindow(WidgetViewModel vm, GoogleCalendarService calendarService)
    {
        InitializeComponent();
        _vm = vm;
        _calendarService = calendarService;

        WideCalGrid.HasEventsCallback = vm.HasEventsOnDate;
        WideCalGrid.SelectedDate = _selectedDate;

        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(vm.Events))
            {
                WideCalGrid.HasEventsCallback = vm.HasEventsOnDate;
                RefreshEventPanel();
            }
        };

        Loaded += (_, _) =>
        {
            UpdatePanelForDate(_selectedDate);
            if (Application.Current.Resources["FontSizeBase"] is double fs)
                CalendarColumn.Width = new System.Windows.GridLength(Math.Max(260, 320 + (fs - 12) * 30));
        };
    }

    private void WideCalGrid_DateSelected(DateTime date)
    {
        _selectedDate = date;
        UpdatePanelForDate(date);
    }

    private void UpdatePanelForDate(DateTime date)
    {
        var days = new[] { "일", "월", "화", "수", "목", "금", "토" };
        PanelDateHeader.Text = $"{date:yyyy년 M월 d일} ({days[(int)date.DayOfWeek]})";
        RefreshEventPanel();
    }

    private void RefreshEventPanel()
    {
        WideEventPanel.Children.Clear();
        var events = _vm.GetEventsForDate(_selectedDate);

        if (events.Count == 0)
        {
            var placeholder = new TextBlock
            {
                Text = "일정이 없습니다",
                FontSize = 13, Margin = new Thickness(0, 16, 0, 0),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };
            placeholder.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
            WideEventPanel.Children.Add(placeholder);
            return;
        }

        foreach (var ev in events)
            WideEventPanel.Children.Add(CreateEventRow(ev));
    }

    private UIElement CreateEventRow(CalendarEventViewModel ev)
    {
        var grid = new Grid { Margin = new Thickness(0, 6, 0, 6) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var cb = new CheckBox
        {
            Style = (Style)FindResource("EventCheckBoxStyle"),
            IsChecked = ev.IsCompleted,
            VerticalAlignment = VerticalAlignment.Center
        };
        cb.Checked += (_, _) => { ev.IsCompleted = true; UpdateTitleStyle(grid, true); };
        cb.Unchecked += (_, _) => { ev.IsCompleted = false; UpdateTitleStyle(grid, false); };
        Grid.SetColumn(cb, 0);
        grid.Children.Add(cb);

        var time = new TextBlock
        {
            Text = ev.StartTime.ToString("HH:mm"),
            FontSize = 12, FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        time.SetResourceReference(TextBlock.ForegroundProperty, "AccentLightBrush");
        Grid.SetColumn(time, 1);
        grid.Children.Add(time);

        var title = new TextBlock
        {
            Text = ev.Title,
            FontSize = 13,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            TextDecorations = ev.IsCompleted ? TextDecorations.Strikethrough : null,
            Opacity = ev.IsCompleted ? 0.4 : 1.0
        };
        title.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        Grid.SetColumn(title, 2);
        grid.Children.Add(title);

        var editBtn = new Button
        {
            Content = "✏",
            Style = (Style)FindResource("IconButtonStyle"),
            FontSize = 13, Tag = ev
        };
        editBtn.Click += (_, _) => OpenEditWindow(ev);
        Grid.SetColumn(editBtn, 3);
        grid.Children.Add(editBtn);

        grid.MouseRightButtonDown += (_, e) =>
        {
            e.Handled = true;
            var menu = new ContextMenu();
            var editItem = new MenuItem { Header = "수정" };
            editItem.Click += (_, _) => OpenEditWindow(ev);
            var delItem = new MenuItem { Header = "삭제" };
            delItem.Click += async (_, _) => await DeleteEventAsync(ev);
            menu.Items.Add(editItem);
            menu.Items.Add(delItem);
            menu.IsOpen = true;
        };

        return grid;
    }

    private void UpdateTitleStyle(Grid grid, bool completed)
    {
        foreach (var child in grid.Children.OfType<TextBlock>())
        {
            if (Grid.GetColumn(child) == 2)
            {
                child.TextDecorations = completed ? TextDecorations.Strikethrough : null;
                child.Opacity = completed ? 0.4 : 1.0;
            }
        }
    }

    private void OpenEditWindow(CalendarEventViewModel ev)
    {
        var win = new EventEditWindow(_selectedDate, ev) { Owner = this };
        if (win.ShowDialog() == true && win.Result != null)
        {
            var r = win.Result;
            Task.Run(async () =>
            {
                await _calendarService.UpdateEventAsync(
                    ev.Id, r.Title, r.Start, r.End, r.Location, r.Description, r.NotifyMinutes);
                Dispatcher.Invoke(() => { _vm.RequestRefreshCommand.Execute(null); });
            });
        }
    }

    private async Task DeleteEventAsync(CalendarEventViewModel ev)
    {
        var result = MessageBox.Show($"'{ev.Title}' 일정을 삭제할까요?",
            "삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;
        await _calendarService.DeleteEventAsync(ev.Id);
        _vm.RequestRefreshCommand.Execute(null);
    }

    private void AddEvent_Click(object sender, RoutedEventArgs e)
    {
        var win = new EventEditWindow(_selectedDate) { Owner = this };
        if (win.ShowDialog() == true && win.Result != null)
        {
            var r = win.Result;
            Task.Run(async () =>
            {
                CalendarEvent? created;
                if (_calendarService.IsAuthenticated)
                    created = await _calendarService.CreateEventAsync(
                        r.Title, r.Start, r.End, r.Location, r.Description, r.NotifyMinutes);
                else
                    created = LocalEventStore.Instance.Add(
                        r.Title, r.Start, r.End, r.Location, r.Description, r.NotifyMinutes);
                Dispatcher.Invoke(() =>
                {
                    if (created == null)
                        MessageBox.Show("일정 추가 실패", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    else
                        _vm.RequestRefreshCommand.Execute(null);
                });
            });
        }
    }

    private void SwitchToCompact_Click(object sender, RoutedEventArgs e)
        => ModeChangeRequested?.Invoke(WidgetMode.Compact);

    private void SwitchToNormal_Click(object sender, RoutedEventArgs e)
        => ModeChangeRequested?.Invoke(WidgetMode.Normal);

    private void Refresh_Click(object sender, RoutedEventArgs e)
        => _vm.RequestRefreshCommand.Execute(null);

    private void Settings_Click(object sender, RoutedEventArgs e)
        => (Application.Current as App)?.OpenSettings();
}
