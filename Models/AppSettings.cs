namespace Window_Alert_App.Models;

public enum WidgetMode { Compact, Normal, Wide }

public class AppSettings
{
    public int PollingIntervalMinutes { get; set; } = 5;
    public int LookAheadHours { get; set; } = 24;

    public bool WidgetEnabled { get; set; } = true;
    public WidgetMode Mode { get; set; } = WidgetMode.Normal;
    public double WidgetLeft { get; set; } = 50;
    public double WidgetTop { get; set; } = 50;

    public bool RunAtStartup { get; set; } = false;
    public bool IsGoogleAuthenticated { get; set; } = false;

    public string ThemeColor { get; set; } = "Black";
    public int FontSizeLevel { get; set; } = 0;  // -1=소, 0=기본, 1=대, 2=특대
    public double NormalWidth { get; set; } = 330;
    public double NormalHeight { get; set; } = 360;
}
