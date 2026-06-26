using System.Windows;
using System.Windows.Media;
using Window_Alert_App.Core;
using Window_Alert_App.Models;
using Window_Alert_App.Settings;
using Window_Alert_App.Tray;
using Window_Alert_App.UI;
using Window_Alert_App.Views;
using Window_Alert_App.Widget;

namespace Window_Alert_App;

public partial class App : Application
{
    private AppSettings _settings = null!;
    private GoogleCalendarService _calendarService = null!;
    private ToastNotificationService _toastService = null!;
    private NotificationScheduler _scheduler = null!;
    private TrayManager _trayManager = null!;
    private WidgetViewModel _widgetViewModel = null!;
    private WidgetWindow? _widgetWindow;
    private WideWindow? _wideWindow;
    private SettingsWindow? _settingsWindow;
    private DayDetailPopup? _dayDetailPopup;
    private readonly CancellationTokenSource _cts = new();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settings = SettingsManager.Instance.Load();
        ApplyTheme(_settings.ThemeColor);
        ApplyFontSize(_settings.FontSizeLevel);
        _calendarService = new GoogleCalendarService();
        _toastService = new ToastNotificationService();
        _scheduler = new NotificationScheduler(_calendarService, _toastService, _settings);

        _toastService.RegisterActivationHandler();

        _widgetViewModel = new WidgetViewModel(_settings);
        _widgetViewModel.ModeChangeRequested += OnModeChangeRequested;
        _widgetViewModel.RefreshRequested += async () =>
        { try { await _scheduler.RefreshNowAsync(); } catch { } };

        _scheduler.EventsRefreshed += events =>
            Dispatcher.Invoke(() => _widgetViewModel.UpdateEvents(events));

        _trayManager = new TrayManager(_settings);
        _trayManager.OpenSettingsRequested += OpenSettings;
        _trayManager.RefreshRequested += async () =>
        { try { await _scheduler.RefreshNowAsync(); } catch { } };
        _trayManager.ModeChangeRequested += OnModeChangeRequested;
        _trayManager.WidgetVisibilityChangeRequested += OnWidgetVisibilityChanged;
        _trayManager.Initialize();

        InitializeWidgetWindows();

        if (_settings.IsGoogleAuthenticated)
            await _calendarService.AuthenticateAsync(_cts.Token);
        await _scheduler.StartAsync(_cts.Token);
    }

    private void InitializeWidgetWindows()
    {
        if (!_settings.WidgetEnabled) return;

        if (_settings.Mode == WidgetMode.Wide)
        {
            OpenWideWindow();
        }
        else
        {
            OpenWidgetWindow();
        }
    }

    private void OpenWidgetWindow()
    {
        if (_widgetWindow != null) return;

        _widgetWindow = new WidgetWindow(_settings, _widgetViewModel);
        _widgetWindow.DateSelectedForPopup += OpenDayDetailPopup;
        _widgetWindow.Show();
    }

    private void OpenWideWindow()
    {
        if (_wideWindow != null) return;

        _wideWindow = new WideWindow(_widgetViewModel, _calendarService);
        _wideWindow.ModeChangeRequested += OnModeChangeRequested;
        _wideWindow.Closed += (_, _) =>
        {
            _wideWindow = null;
            // Wide 닫으면 Normal로 복귀
            if (_settings.Mode == WidgetMode.Wide)
                OnModeChangeRequested(WidgetMode.Normal);
        };
        _wideWindow.Show();
    }

    private void OnModeChangeRequested(WidgetMode mode)
    {
        _settings.Mode = mode;
        SettingsManager.Instance.Save(_settings);
        _widgetViewModel.CurrentMode = mode;
        _trayManager.UpdateModeCheck(mode);

        _dayDetailPopup?.Close();
        _dayDetailPopup = null;

        switch (mode)
        {
            case WidgetMode.Wide:
                _widgetWindow?.Hide();
                OpenWideWindow();
                break;

            case WidgetMode.Compact:
            case WidgetMode.Normal:
                _wideWindow?.Close();
                if (_widgetWindow == null)
                    OpenWidgetWindow();
                else
                {
                    _widgetWindow.Show();
                    _widgetWindow.ApplyMode(mode);
                }
                break;
        }
    }

    private void OnWidgetVisibilityChanged(bool visible)
    {
        _settings.WidgetEnabled = visible;
        SettingsManager.Instance.Save(_settings);

        if (visible)
        {
            InitializeWidgetWindows();
        }
        else
        {
            _widgetWindow?.Hide();
            _wideWindow?.Hide();
        }
    }

    private void OpenDayDetailPopup(DateTime date)
    {
        _dayDetailPopup?.Close();
        _dayDetailPopup = null;

        if (_widgetWindow == null) return;

        _dayDetailPopup = new DayDetailPopup(date, _widgetViewModel, _calendarService);
        _dayDetailPopup.PositionNearWidget(_widgetWindow);
        _dayDetailPopup.Closed += (_, _) => _dayDetailPopup = null;
        _dayDetailPopup.Show();
    }

    public void ApplyTheme(string theme)
    {
        var res = Current.Resources;

        static Color ParseHex(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 8)
                return Color.FromArgb(
                    Convert.ToByte(hex[0..2], 16), Convert.ToByte(hex[2..4], 16),
                    Convert.ToByte(hex[4..6], 16), Convert.ToByte(hex[6..8], 16));
            return Color.FromRgb(
                Convert.ToByte(hex[0..2], 16),
                Convert.ToByte(hex[2..4], 16),
                Convert.ToByte(hex[4..6], 16));
        }
        static SolidColorBrush Brush(string hex) => new(ParseHex(hex));

        switch (theme)
        {
            case "White":
                res["WidgetBgBrush"]        = Brush("#F0FFFFFF");
                res["WidgetBorderBrush"]    = Brush("#30000000");
                res["AccentBrush"]          = Brush("#6366F1");
                res["AccentLightBrush"]     = Brush("#4F46E5");
                res["TextPrimaryBrush"]     = Brush("#1E1E2E");
                res["TextSecondaryBrush"]   = Brush("#6B7280");
                res["TodayHighlightBrush"]  = Brush("#6366F1");
                res["UrgentBrush"]          = Brush("#EF4444");
                res["HoverBgBrush"]         = Brush("#1A000000");
                res["PressedBgBrush"]       = Brush("#33000000");
                res["ScrollThumbBrush"]     = Brush("#44000000");
                res["SelectedBgBrush"]      = Brush("#336366F1");
                break;
            case "Blue": // 치이카와 모몽가: 하늘색 배경 + 스카이블루 포인트
                res["WidgetBgBrush"]        = Brush("#F0E8F4FF");
                res["WidgetBorderBrush"]    = Brush("#30000000");
                res["AccentBrush"]          = Brush("#7EC8E3");
                res["AccentLightBrush"]     = Brush("#5AAFE6");
                res["TextPrimaryBrush"]     = Brush("#2C1F14");
                res["TextSecondaryBrush"]   = Brush("#7C6858");
                res["TodayHighlightBrush"]  = Brush("#2E9FC7");
                res["UrgentBrush"]          = Brush("#EF4444");
                res["HoverBgBrush"]         = Brush("#1A000000");
                res["PressedBgBrush"]       = Brush("#33000000");
                res["ScrollThumbBrush"]     = Brush("#44000000");
                res["SelectedBgBrush"]      = Brush("#337EC8E3");
                break;
            case "Purple": // 산리오 쿠로미: 연보라 배경 + 진한 보라
                res["WidgetBgBrush"]        = Brush("#F0FAF5FF");
                res["WidgetBorderBrush"]    = Brush("#30000000");
                res["AccentBrush"]          = Brush("#6B21A8");
                res["AccentLightBrush"]     = Brush("#7C3AED");
                res["TextPrimaryBrush"]     = Brush("#1A0B2E");
                res["TextSecondaryBrush"]   = Brush("#6B46A8");
                res["TodayHighlightBrush"]  = Brush("#6B21A8");
                res["UrgentBrush"]          = Brush("#EF4444");
                res["HoverBgBrush"]         = Brush("#1A000000");
                res["PressedBgBrush"]       = Brush("#33000000");
                res["ScrollThumbBrush"]     = Brush("#44000000");
                res["SelectedBgBrush"]      = Brush("#336B21A8");
                break;
            case "Green":
                res["WidgetBgBrush"]        = Brush("#F0ECFDF5");
                res["WidgetBorderBrush"]    = Brush("#30000000");
                res["AccentBrush"]          = Brush("#10B981");
                res["AccentLightBrush"]     = Brush("#059669");
                res["TextPrimaryBrush"]     = Brush("#064E3B");
                res["TextSecondaryBrush"]   = Brush("#2D6A4F");
                res["TodayHighlightBrush"]  = Brush("#10B981");
                res["UrgentBrush"]          = Brush("#EF4444");
                res["HoverBgBrush"]         = Brush("#1A000000");
                res["PressedBgBrush"]       = Brush("#33000000");
                res["ScrollThumbBrush"]     = Brush("#44000000");
                res["SelectedBgBrush"]      = Brush("#3310B981");
                break;
            case "Pink":
                res["WidgetBgBrush"]        = Brush("#F0FFF1F5");
                res["WidgetBorderBrush"]    = Brush("#30000000");
                res["AccentBrush"]          = Brush("#EC4899");
                res["AccentLightBrush"]     = Brush("#DB2777");
                res["TextPrimaryBrush"]     = Brush("#831843");
                res["TextSecondaryBrush"]   = Brush("#9D5070");
                res["TodayHighlightBrush"]  = Brush("#EC4899");
                res["UrgentBrush"]          = Brush("#EF4444");
                res["HoverBgBrush"]         = Brush("#1A000000");
                res["PressedBgBrush"]       = Brush("#33000000");
                res["ScrollThumbBrush"]     = Brush("#44000000");
                res["SelectedBgBrush"]      = Brush("#33EC4899");
                break;
            default: // Black (다크)
                res["WidgetBgBrush"]        = Brush("#CC1E1E2E");
                res["WidgetBorderBrush"]    = Brush("#44FFFFFF");
                res["AccentBrush"]          = Brush("#6C8EBF");
                res["AccentLightBrush"]     = Brush("#A8C4E8");
                res["TextPrimaryBrush"]     = Brush("#F0F0F5");
                res["TextSecondaryBrush"]   = Brush("#90909A");
                res["TodayHighlightBrush"]  = Brush("#4A90D9");
                res["UrgentBrush"]          = Brush("#E06C75");
                res["HoverBgBrush"]         = Brush("#33FFFFFF");
                res["PressedBgBrush"]       = Brush("#55FFFFFF");
                res["ScrollThumbBrush"]     = Brush("#44FFFFFF");
                res["SelectedBgBrush"]      = Brush("#446C8EBF");
                break;
        }
    }

    public void ApplyFontSize(int level)
    {
        var res = Current.Resources;
        res["FontSizeXSmall"] = (double)(10 + level);
        res["FontSizeSmall"]  = (double)(11 + level);
        res["FontSizeBase"]   = (double)(12 + level);
        res["FontSizeLarge"]  = (double)(13 + level);
    }

    public void OpenSettings()
    {
        if (_settingsWindow != null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settings, _calendarService);
        _settingsWindow.Closed += (_, _) =>
        {
            _settingsWindow = null;
            // 설정 저장 후 위젯 상태 동기화
            OnWidgetVisibilityChanged(_settings.WidgetEnabled);
            if (_settings.Mode != _widgetViewModel.CurrentMode)
                OnModeChangeRequested(_settings.Mode);
        };
        _settingsWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        _cts.Cancel();
        _settings.WidgetLeft = _widgetWindow?.Left ?? _settings.WidgetLeft;
        _settings.WidgetTop = _widgetWindow?.Top ?? _settings.WidgetTop;
        if (_widgetWindow != null && _settings.Mode == WidgetMode.Normal)
        {
            _settings.NormalWidth = _widgetWindow.Width;
            _settings.NormalHeight = _widgetWindow.Height;
        }
        SettingsManager.Instance.Save(_settings);

        _trayManager.Dispose();

        try { await _scheduler.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(3)); }
        catch { }

        _calendarService.Dispose();
        base.OnExit(e);
    }
}
