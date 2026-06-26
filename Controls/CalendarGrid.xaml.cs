using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Brush = System.Windows.Media.Brush;

namespace Window_Alert_App.Controls;

public partial class CalendarGrid : UserControl
{
    public static readonly DependencyProperty DisplayMonthProperty =
        DependencyProperty.Register(nameof(DisplayMonth), typeof(DateTime), typeof(CalendarGrid),
            new PropertyMetadata(DateTime.Today, OnDisplayMonthChanged));

    public static readonly DependencyProperty SelectedDateProperty =
        DependencyProperty.Register(nameof(SelectedDate), typeof(DateTime), typeof(CalendarGrid),
            new PropertyMetadata(DateTime.Today, OnSelectedDateChanged));

    public static readonly DependencyProperty HasEventsCallbackProperty =
        DependencyProperty.Register(nameof(HasEventsCallback), typeof(Func<DateTime, bool>), typeof(CalendarGrid),
            new PropertyMetadata(null, OnDisplayMonthChanged));

    public DateTime DisplayMonth
    {
        get => (DateTime)GetValue(DisplayMonthProperty);
        set => SetValue(DisplayMonthProperty, value);
    }

    public DateTime SelectedDate
    {
        get => (DateTime)GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public Func<DateTime, bool>? HasEventsCallback
    {
        get => (Func<DateTime, bool>?)GetValue(HasEventsCallbackProperty);
        set => SetValue(HasEventsCallbackProperty, value);
    }

    public event Action<DateTime>? DateSelected;

    public double NavRightMargin
    {
        get => NextMonthBtn.Margin.Right;
        set => NextMonthBtn.Margin = new Thickness(0, 0, value, 0);
    }

    public CalendarGrid()
    {
        InitializeComponent();
        Loaded += (_, _) => Rebuild();
    }

    private static void OnDisplayMonthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((CalendarGrid)d).Rebuild();

    private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((CalendarGrid)d).Rebuild();

    private void Rebuild()
    {
        if (!IsLoaded) return;

        MonthHeader.Text = $"{DisplayMonth.Year}년 {DisplayMonth.Month}월";
        DaysGrid.Children.Clear();

        var firstDay = new DateTime(DisplayMonth.Year, DisplayMonth.Month, 1);
        int offset = (int)firstDay.DayOfWeek;
        int daysInMonth = DateTime.DaysInMonth(DisplayMonth.Year, DisplayMonth.Month);

        for (int i = 0; i < offset; i++)
            DaysGrid.Children.Add(new Border());

        for (int d = 1; d <= daysInMonth; d++)
        {
            var date = new DateTime(DisplayMonth.Year, DisplayMonth.Month, d);
            DaysGrid.Children.Add(CreateDayCell(date));
        }

        int remaining = 42 - offset - daysInMonth;
        for (int i = 0; i < remaining; i++)
            DaysGrid.Children.Add(new Border());
    }

    private Border CreateDayCell(DateTime date)
    {
        bool isToday = date.Date == DateTime.Today;
        bool isSelected = date.Date == SelectedDate.Date;
        bool isSunday = date.DayOfWeek == DayOfWeek.Sunday;
        bool isSaturday = date.DayOfWeek == DayOfWeek.Saturday;
        bool hasEvents = HasEventsCallback?.Invoke(date) ?? false;

        var stack = new StackPanel { HorizontalAlignment = System.Windows.HorizontalAlignment.Center };

        var numText = new TextBlock
        {
            Text = date.Day.ToString(),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center
        };
        numText.SetResourceReference(TextBlock.FontSizeProperty, "FontSizeSmall");
        if (isSunday)
            numText.SetResourceReference(TextBlock.ForegroundProperty, "UrgentBrush");
        else if (isSaturday)
            numText.SetResourceReference(TextBlock.ForegroundProperty, "AccentLightBrush");
        else
            numText.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");

        if (isToday)
        {
            numText.FontWeight = FontWeights.Bold;
            var todayText = new TextBlock
            {
                Text = date.Day.ToString(),
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            todayText.SetResourceReference(TextBlock.FontSizeProperty, "FontSizeSmall");
            var todayBorder = new Border
            {
                Width = 22, Height = 22,
                CornerRadius = new CornerRadius(11),
                Child = todayText
            };
            todayBorder.SetResourceReference(Border.BackgroundProperty, "TodayHighlightBrush");
            stack.Children.Add(todayBorder);
        }
        else
        {
            stack.Children.Add(numText);
        }

        if (hasEvents)
        {
            var dot = new Ellipse
            {
                Width = 4, Height = 4,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(0, 1, 0, 0)
            };
            dot.SetResourceReference(Ellipse.FillProperty, "AccentBrush");
            stack.Children.Add(dot);
        }

        var cell = new Border
        {
            Child = stack,
            Padding = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            Tag = date
        };

        if (isSelected && !isToday)
            cell.SetResourceReference(Border.BackgroundProperty, "SelectedBgBrush");

        cell.MouseEnter += (_, _) =>
        {
            if (!isToday && !isSelected)
                cell.Background = Application.Current.Resources["HoverBgBrush"] as Brush;
        };
        cell.MouseLeave += (_, _) =>
        {
            if (!isToday && !isSelected)
                cell.Background = Brushes.Transparent;
            else if (isSelected && !isToday)
                cell.SetResourceReference(Border.BackgroundProperty, "SelectedBgBrush");
        };
        cell.MouseLeftButtonDown += (_, _) =>
        {
            SelectedDate = date;
            DateSelected?.Invoke(date);
        };

        return cell;
    }

    private void PrevMonth_Click(object sender, RoutedEventArgs e)
        => DisplayMonth = DisplayMonth.AddMonths(-1);

    private void NextMonth_Click(object sender, RoutedEventArgs e)
        => DisplayMonth = DisplayMonth.AddMonths(1);

    private void MonthHeader_DragMove(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            Window.GetWindow(this)?.DragMove();
    }
}
