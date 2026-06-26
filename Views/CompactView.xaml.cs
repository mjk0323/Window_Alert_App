using System.Windows.Controls;
using System.Windows.Input;
using Window_Alert_App.Widget;

namespace Window_Alert_App.Views;

public partial class CompactView : UserControl
{
    public event Action<DateTime>? DateSelectedForPopup;

    public CompactView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => UpdateDateText();
    }

    private void UpdateDateText()
    {
        if (DataContext is WidgetViewModel vm)
        {
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(vm.SelectedDate))
                    RefreshDateText(vm.SelectedDate);
            };
            RefreshDateText(vm.SelectedDate);
        }
    }

    private void RefreshDateText(DateTime date)
    {
        var dayNames = new[] { "일", "월", "화", "수", "목", "금", "토" };
        DateText.Text = $"{date.Month}월 {date.Day}일 ({dayNames[(int)date.DayOfWeek]})";
        DateText.Foreground = date.Date == DateTime.Today
            ? (System.Windows.Media.Brush)FindResource("TodayHighlightBrush")
            : (System.Windows.Media.Brush)FindResource("TextPrimaryBrush");
    }

    private void Header_DragMove(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            System.Windows.Window.GetWindow(this)?.DragMove();
    }

    private void AddEvent_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is WidgetViewModel vm)
            DateSelectedForPopup?.Invoke(vm.SelectedDate);
    }

    private void EventRow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is WidgetViewModel vm)
            DateSelectedForPopup?.Invoke(vm.SelectedDate);
    }
}
