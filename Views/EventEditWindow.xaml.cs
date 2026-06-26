using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Window_Alert_App.Models;
using TextBox = System.Windows.Controls.TextBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace Window_Alert_App.Views;

public partial class EventEditWindow : Window
{
    public record EventResult(
        string Title, DateTime Start, DateTime End,
        string? Location, string? Description, int NotifyMinutes);

    public EventResult? Result { get; private set; }

    private readonly string? _existingEventId;

    public EventEditWindow(DateTime initialDate, CalendarEventViewModel? existing = null)
    {
        InitializeComponent();
        _existingEventId = existing?.Id;

        PopulateTimeComboBoxes();

        if (existing != null)
        {
            TitleBox.Text = existing.Title;
            DatePicker.SelectedDate = existing.StartTime.Date;
            SetTime(StartHourBox, StartMinuteBox, existing.StartTime.Hour, existing.StartTime.Minute);
            SetTime(EndHourBox, EndMinuteBox, existing.EndTime.Hour, existing.EndTime.Minute);
            LocationBox.Text = existing.Location ?? string.Empty;
            DescriptionBox.Text = existing.Description ?? string.Empty;
            SelectNotifyMinutes(existing.NotifyMinutesBefore);
            Title = "일정 수정";
        }
        else
        {
            DatePicker.SelectedDate = initialDate;
            SetTime(StartHourBox, StartMinuteBox, 9, 0);
            SetTime(EndHourBox, EndMinuteBox, 9, 30);
        }
    }

    private void PopulateTimeComboBoxes()
    {
        for (int h = 0; h <= 23; h++)
        {
            StartHourBox.Items.Add(new ComboBoxItem { Content = h.ToString("D2"), Tag = h });
            EndHourBox.Items.Add(new ComboBoxItem { Content = h.ToString("D2"), Tag = h });
        }

    }

    private static void SetTime(ComboBox hourBox, TextBox minuteBox, int hour, int minute)
    {
        foreach (ComboBoxItem item in hourBox.Items)
            if (item.Tag is int h && h == hour) { hourBox.SelectedItem = item; break; }
        minuteBox.Text = minute.ToString("D2");
    }

    private static (int hour, int minute) GetTime(ComboBox hourBox, TextBox minuteBox)
    {
        int h = hourBox.SelectedItem is ComboBoxItem hi && hi.Tag is int hv ? hv : 9;
        int m = int.TryParse(minuteBox.Text, out int mv) ? Math.Clamp(mv, 0, 59) : 0;
        return (h, m);
    }

    private void MinuteBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox tb) return;
        int val = int.TryParse(tb.Text, out int v) ? v : 0;
        if (e.Key == Key.Up)   { tb.Text = Math.Clamp(val + 1, 0, 59).ToString("D2"); e.Handled = true; }
        if (e.Key == Key.Down) { tb.Text = Math.Clamp(val - 1, 0, 59).ToString("D2"); e.Handled = true; }
    }

    private void SelectNotifyMinutes(int minutes)
    {
        foreach (ComboBoxItem item in NotifyComboBox.Items)
        {
            if (item.Tag is string tag && int.TryParse(tag, out int val) && val == minutes)
            {
                NotifyComboBox.SelectedItem = item;
                return;
            }
        }
    }

    private int GetSelectedNotifyMinutes()
    {
        if (NotifyComboBox.SelectedItem is ComboBoxItem item &&
            item.Tag is string tag &&
            int.TryParse(tag, out int val))
            return val;
        return 10;
    }

    private void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleBox.Text))
        {
            MessageBox.Show("제목을 입력하세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
            TitleBox.Focus();
            return;
        }

        if (DatePicker.SelectedDate is not DateTime date)
        {
            MessageBox.Show("날짜를 선택하세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var (startH, startM) = GetTime(StartHourBox, StartMinuteBox);
        var (endH, endM) = GetTime(EndHourBox, EndMinuteBox);

        var start = date + new TimeSpan(startH, startM, 0);
        var end = date + new TimeSpan(endH, endM, 0);
        if (end <= start) end = start.AddMinutes(30);

        Result = new EventResult(
            TitleBox.Text.Trim(),
            start, end,
            string.IsNullOrWhiteSpace(LocationBox.Text) ? null : LocationBox.Text.Trim(),
            string.IsNullOrWhiteSpace(DescriptionBox.Text) ? null : DescriptionBox.Text.Trim(),
            GetSelectedNotifyMinutes());

        DialogResult = true;
        Close();
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
