using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using Window_Alert_App.Models;
using Window_Alert_App.Settings;

namespace Window_Alert_App.Tray;

public class TrayManager : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private AppSettings _settings;

    public event Action? OpenSettingsRequested;
    public event Action? RefreshRequested;
    public event Action<WidgetMode>? ModeChangeRequested;
    public event Action<bool>? WidgetVisibilityChangeRequested;

    public TrayManager(AppSettings settings)
    {
        _settings = settings;
    }

    public void Initialize()
    {
        _notifyIcon = new NotifyIcon
        {
            Visible = true,
            Text = "Window Alert App"
        };

        LoadIcon();
        BuildContextMenu();

        _notifyIcon.DoubleClick += (_, _) => OpenSettingsRequested?.Invoke();
    }

    private void LoadIcon()
    {
        if (_notifyIcon == null) return;

        var iconPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "Resources", "tray-icon.ico");

        if (File.Exists(iconPath))
            _notifyIcon.Icon = new Icon(iconPath);
        else
            _notifyIcon.Icon = SystemIcons.Application;
    }

    private void BuildContextMenu()
    {
        if (_notifyIcon == null) return;

        var menu = new ContextMenuStrip();

        var header = new ToolStripMenuItem("Window Alert App") { Enabled = false };
        header.Font = new Font(header.Font, System.Drawing.FontStyle.Bold);
        menu.Items.Add(header);
        menu.Items.Add(new ToolStripSeparator());

        // 위젯 보이기/숨기기
        var toggleWidget = new ToolStripMenuItem(
            _settings.WidgetEnabled ? "위젯 숨기기" : "위젯 보이기");
        toggleWidget.Click += (_, _) =>
        {
            _settings.WidgetEnabled = !_settings.WidgetEnabled;
            SettingsManager.Instance.Save(_settings);
            WidgetVisibilityChangeRequested?.Invoke(_settings.WidgetEnabled);
            toggleWidget.Text = _settings.WidgetEnabled ? "위젯 숨기기" : "위젯 보이기";
        };
        menu.Items.Add(toggleWidget);
        menu.Items.Add(new ToolStripSeparator());

        // 모드 전환
        var modeMenu = new ToolStripMenuItem("모드 전환");
        foreach (WidgetMode mode in Enum.GetValues<WidgetMode>())
        {
            var m = mode;
            var item = new ToolStripMenuItem(mode.ToString())
            {
                Checked = _settings.Mode == mode
            };
            item.Click += (_, _) =>
            {
                ModeChangeRequested?.Invoke(m);
                // 체크 상태 갱신
                foreach (ToolStripMenuItem child in modeMenu.DropDownItems)
                    child.Checked = child.Text == m.ToString();
            };
            modeMenu.DropDownItems.Add(item);
        }
        menu.Items.Add(modeMenu);
        menu.Items.Add(new ToolStripSeparator());

        // 지금 새로 고침
        var refresh = new ToolStripMenuItem("지금 새로 고침");
        refresh.Click += (_, _) => RefreshRequested?.Invoke();
        menu.Items.Add(refresh);

        // 설정
        var settings = new ToolStripMenuItem("설정...");
        settings.Click += (_, _) => OpenSettingsRequested?.Invoke();
        menu.Items.Add(settings);
        menu.Items.Add(new ToolStripSeparator());

        // 종료
        var exit = new ToolStripMenuItem("종료");
        exit.Click += (_, _) =>
        {
            _notifyIcon!.Visible = false;
            Application.Current.Shutdown();
        };
        menu.Items.Add(exit);

        _notifyIcon.ContextMenuStrip = menu;
    }

    public void UpdateModeCheck(WidgetMode mode)
    {
        _settings = SettingsManager.Instance.Load();
        if (_notifyIcon?.ContextMenuStrip == null) return;

        var modeMenu = _notifyIcon.ContextMenuStrip.Items
            .OfType<ToolStripMenuItem>()
            .FirstOrDefault(i => i.Text == "모드 전환");

        if (modeMenu == null) return;
        foreach (ToolStripMenuItem item in modeMenu.DropDownItems)
            item.Checked = item.Text == mode.ToString();
    }

    public void Dispose()
    {
        _notifyIcon?.Dispose();
    }
}
