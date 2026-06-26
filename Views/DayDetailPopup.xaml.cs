using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Window_Alert_App.Core;
using Window_Alert_App.Models;
using Window_Alert_App.Settings;
using Window_Alert_App.Widget;

namespace Window_Alert_App.Views;

public partial class DayDetailPopup : Window
{
    private readonly DateTime _date;
    private readonly WidgetViewModel _vm;
    private readonly GoogleCalendarService _calendarService;

    public DayDetailPopup(DateTime date, WidgetViewModel vm, GoogleCalendarService calendarService)
    {
        InitializeComponent();
        _date = date;
        _vm = vm;
        _calendarService = calendarService;

        var days = new[] { "일", "월", "화", "수", "목", "금", "토" };
        DateHeader.Text = $"{date.Year}년 {date.Month}월 {date.Day}일 ({days[(int)date.DayOfWeek]})";

        Deactivated += OnDeactivatedClose;
        Loaded += (_, _) => BuildEventList();
    }

    private void BuildEventList()
    {
        EventListPanel.Children.Clear();
        var events = _vm.GetEventsForDate(_date);

        if (events.Count == 0)
        {
            var placeholder = new TextBlock
            {
                Text = "일정이 없습니다",
                Margin = new Thickness(0, 8, 0, 8),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };
            placeholder.SetResourceReference(TextBlock.FontSizeProperty, "FontSizeBase");
            placeholder.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");
            EventListPanel.Children.Add(placeholder);
            return;
        }

        foreach (var ev in events)
        {
            EventListPanel.Children.Add(CreateEventRow(ev));
        }
    }

    private UIElement CreateEventRow(CalendarEventViewModel ev)
    {
        var grid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(18) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var cb = new CheckBox
        {
            Style = (Style)FindResource("EventCheckBoxStyle"),
            IsChecked = ev.IsCompleted,
            VerticalAlignment = VerticalAlignment.Center
        };
        cb.Checked += (_, _) => { ev.IsCompleted = true; RefreshRowStyle(grid, true); };
        cb.Unchecked += (_, _) => { ev.IsCompleted = false; RefreshRowStyle(grid, false); };
        Grid.SetColumn(cb, 0);
        grid.Children.Add(cb);

        var timeText = new TextBlock
        {
            Text = ev.StartTime.ToString("HH:mm"),
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        timeText.SetResourceReference(TextBlock.FontSizeProperty, "FontSizeSmall");
        timeText.SetResourceReference(TextBlock.ForegroundProperty, "AccentLightBrush");
        Grid.SetColumn(timeText, 1);
        grid.Children.Add(timeText);

        var titleText = new TextBlock
        {
            Text = ev.Title,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        titleText.SetResourceReference(TextBlock.FontSizeProperty, "FontSizeBase");
        titleText.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");
        if (ev.IsCompleted)
        {
            titleText.TextDecorations = TextDecorations.Strikethrough;
            titleText.Opacity = 0.4;
        }
        Grid.SetColumn(titleText, 2);
        grid.Children.Add(titleText);

        var editBtn = new Button
        {
            Content = "✏",
            Style = (Style)FindResource("IconButtonStyle"),
            FontSize = 12,
            ToolTip = "수정",
            Tag = ev
        };
        editBtn.Click += EditEvent_Click;
        Grid.SetColumn(editBtn, 3);
        grid.Children.Add(editBtn);

        grid.MouseRightButtonDown += (_, e) =>
        {
            e.Handled = true;
            var menu = new ContextMenu();
            var editItem = new MenuItem { Header = "수정" };
            editItem.Click += (_, _) => OpenEditWindow(ev);
            var deleteItem = new MenuItem { Header = "삭제" };
            deleteItem.Click += async (_, _) => await DeleteEventAsync(ev);
            menu.Items.Add(editItem);
            menu.Items.Add(deleteItem);
            menu.IsOpen = true;
        };

        return grid;
    }

    private void RefreshRowStyle(Grid grid, bool completed)
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

    private void EditEvent_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is CalendarEventViewModel ev)
            OpenEditWindow(ev);
    }

    private void OpenEditWindow(CalendarEventViewModel ev)
    {
        Deactivated -= OnDeactivatedClose;
        var win = new EventEditWindow(_date, ev) { Owner = this };
        if (win.ShowDialog() == true && win.Result != null)
        {
            var r = win.Result;
            Task.Run(async () =>
            {
                await _calendarService.UpdateEventAsync(
                    ev.Id, r.Title, r.Start, r.End, r.Location, r.Description, r.NotifyMinutes);
                Dispatcher.Invoke(() =>
                {
                    _vm.RequestRefreshCommand.Execute(null);
                    BuildEventList();
                });
            });
        }
        Deactivated += OnDeactivatedClose;
        Activate();
    }

    private async Task DeleteEventAsync(CalendarEventViewModel ev)
    {
        var result = MessageBox.Show($"'{ev.Title}' 일정을 삭제할까요?",
            "삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        await _calendarService.DeleteEventAsync(ev.Id);
        _vm.RequestRefreshCommand.Execute(null);
        BuildEventList();
    }

    private void AddEvent_Click(object sender, RoutedEventArgs e)
    {
        Deactivated -= OnDeactivatedClose;
        var win = new EventEditWindow(_date) { Owner = this };
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
                    {
                        _vm.RequestRefreshCommand.Execute(null);
                        BuildEventList();
                    }
                });
            });
        }
        Deactivated += OnDeactivatedClose;
        Activate();
    }

    private void OnDeactivatedClose(object? sender, EventArgs e) => Close();

    private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();

    private void Header_DragMove(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            DragMove();
    }

    public void PositionNearWidget(Window widgetWindow)
    {
        var screen = SystemParameters.WorkArea;
        double preferredLeft = widgetWindow.Left + widgetWindow.Width + 8;
        double preferredTop = widgetWindow.Top;

        Left = preferredLeft + Width > screen.Right
            ? widgetWindow.Left - Width - 8
            : preferredLeft;

        Top = Math.Min(preferredTop, screen.Bottom - Height - 8);
        Top = Math.Max(Top, screen.Top);
    }
}
