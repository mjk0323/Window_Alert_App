using System.Windows;
using System.Windows.Interop;
using Window_Alert_App.Models;
using Window_Alert_App.Settings;
using Window_Alert_App.Views;

namespace Window_Alert_App.Widget;

public partial class WidgetWindow : Window
{
    public WidgetViewModel ViewModel { get; }
    private readonly AppSettings _settings;
    private CompactView? _compactView;
    private NormalView? _normalView;
    private bool _applyingMode;

    public event Action<DateTime>? DateSelectedForPopup;

    public WidgetWindow(AppSettings settings, WidgetViewModel viewModel)
    {
        InitializeComponent();
        _settings = settings;
        ViewModel = viewModel;
        DataContext = viewModel;

        Left = settings.WidgetLeft;
        Top = settings.WidgetTop;

        viewModel.ModeChangeRequested += OnModeChangeRequested;

        ApplyMode(settings.Mode, animate: false);

        Loaded += OnLoaded;
        LocationChanged += OnLocationChanged;
        SizeChanged += OnSizeChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        DesktopLayerHelper.Apply(this);
        ContentGrid.SizeChanged += (_, e2) => ContentGrid.Clip = new System.Windows.Media.RectangleGeometry(
            new Rect(0, 0, e2.NewSize.Width, e2.NewSize.Height), 11, 11);
    }

    private void OnModeChangeRequested(WidgetMode mode)
    {
        if (mode == WidgetMode.Wide)
        {
            // Wide는 별도 창 (App.xaml.cs에서 처리)
            return;
        }
        ApplyMode(mode);
    }

    public void ApplyMode(WidgetMode mode, bool animate = true)
    {
        _applyingMode = true;
        _settings.Mode = mode;

        switch (mode)
        {
            case WidgetMode.Compact:
                if (_compactView == null)
                {
                    _compactView = new CompactView { DataContext = ViewModel };
                    _compactView.DateSelectedForPopup += date => DateSelectedForPopup?.Invoke(date);
                }
                ContentArea.Content = _compactView;
                ContentViewbox.Stretch = System.Windows.Media.Stretch.None;
                ResizeMode = ResizeMode.NoResize;
                SizeToContent = SizeToContent.Height;
                Width = 310;
                break;

            case WidgetMode.Normal:
                if (_normalView == null)
                {
                    _normalView = new NormalView { DataContext = ViewModel };
                    _normalView.DateSelectedForPopup += date => DateSelectedForPopup?.Invoke(date);
                }
                ContentArea.Content = _normalView;
                ContentViewbox.Stretch = System.Windows.Media.Stretch.Uniform;
                SizeToContent = SizeToContent.Manual;
                ResizeMode = ResizeMode.CanResizeWithGrip;
                Width = _settings.NormalWidth;
                Height = _settings.NormalHeight;
                break;
        }
        _applyingMode = false;
    }

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        _settings.WidgetLeft = Left;
        _settings.WidgetTop = Top;
        SettingsManager.Instance.Save(_settings);
    }

    private void OnSizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
    {
        if (_applyingMode || _settings.Mode != WidgetMode.Normal) return;
        _settings.NormalWidth = Width;
        _settings.NormalHeight = Height;
        SettingsManager.Instance.Save(_settings);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        // 우클릭 컨텍스트 메뉴
        MouseRightButtonDown += (_, _) => ShowContextMenu();
    }

    private void ShowContextMenu()
    {
        var menu = new System.Windows.Controls.ContextMenu();

        void AddModeItem(string label, WidgetMode mode)
        {
            var item = new System.Windows.Controls.MenuItem
            {
                Header = label,
                IsChecked = _settings.Mode == mode
            };
            item.Click += (_, _) => ViewModel.SwitchModeCommand.Execute(mode.ToString());
            menu.Items.Add(item);
        }

        if (_settings.Mode != WidgetMode.Compact) AddModeItem("Compact 모드", WidgetMode.Compact);
        if (_settings.Mode != WidgetMode.Normal) AddModeItem("Normal 모드", WidgetMode.Normal);
        AddModeItem("Wide 모드", WidgetMode.Wide);

        menu.Items.Add(new System.Windows.Controls.Separator());
        var refresh = new System.Windows.Controls.MenuItem { Header = "지금 새로 고침" };
        refresh.Click += (_, _) => ViewModel.RequestRefreshCommand.Execute(null);
        menu.Items.Add(refresh);

        var settings = new System.Windows.Controls.MenuItem { Header = "설정..." };
        settings.Click += (_, _) =>
        {
            // App에서 처리
            (Application.Current as App)?.OpenSettings();
        };
        menu.Items.Add(settings);
        menu.Items.Add(new System.Windows.Controls.Separator());

        var exit = new System.Windows.Controls.MenuItem { Header = "종료" };
        exit.Click += (_, _) => Application.Current.Shutdown();
        menu.Items.Add(exit);

        menu.IsOpen = true;
    }
}
