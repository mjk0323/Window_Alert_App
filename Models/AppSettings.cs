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
}
