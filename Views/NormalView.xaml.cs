using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Window_Alert_App.Widget;

namespace Window_Alert_App.Views;

public partial class NormalView : UserControl
{
    private WidgetViewModel? _vm;
    public event Action<DateTime>? DateSelectedForPopup;

    public NormalView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        _vm = DataContext as WidgetViewModel;
        if (_vm == null) return;

        CalGrid.HasEventsCallback = _vm.HasEventsOnDate;

        _vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(_vm.Events))
            {
                CalGrid.HasEventsCallback = _vm.HasEventsOnDate;
                CalGrid.InvalidateVisual();
                UpdatePreview();
            }
            else if (args.PropertyName == nameof(_vm.CalendarMonth))
            {
                CalGrid.DisplayMonth = _vm.CalendarMonth;
            }
        };

        UpdatePreview();
    }

    private void Header_DragMove(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            Window.GetWindow(this)?.DragMove();
    }

    private void CalGrid_DateSelected(DateTime date)
    {
        if (_vm != null)
            _vm.SelectedDate = date;
        DateSelectedForPopup?.Invoke(date);
    }

    private void UpdatePreview()
    {
        if (_vm == null) return;
        var todayEvents = _vm.GetEventsForDate(DateTime.Today);
        if (todayEvents.Count == 0)
            PreviewText.Text = "오늘 일정 없음";
        else
            PreviewText.Text = $"오늘 · {todayEvents[0].StartTime:HH:mm} {todayEvents[0].Title}";
    }
}
