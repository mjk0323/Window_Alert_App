using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Window_Alert_App.Core;
using Window_Alert_App.Models;
using Window_Alert_App.Settings;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using TextAlignment = System.Windows.TextAlignment;

namespace Window_Alert_App.UI;

public partial class SettingsWindow : Window
{
    private readonly GoogleCalendarService _calendarService;
    private readonly AppSettings _settings;
    private string _selectedTheme;

    private static readonly (string Key, string Label, string Swatch, string Text)[] Themes =
    [
        ("Black",  "검정",  "#1E1E2E", "#F0F0F5"),
        ("White",  "흰색",  "#FFFFFF",  "#1E1E2E"),
        ("Blue",   "파란색", "#B8E4F2",  "#2C1F14"),
        ("Purple", "보라색", "#E9D5FF",  "#1A0B2E"),
        ("Green",  "초록색", "#A7F3D0",  "#064E3B"),
        ("Pink",   "핑크색", "#FBCFE8",  "#831843"),
    ];

    private static Color ParseHex(string hex)
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

    public SettingsWindow(AppSettings settings, GoogleCalendarService calendarService)
    {
        InitializeComponent();
        _settings = settings;
        _calendarService = calendarService;
        _selectedTheme = settings.ThemeColor;

        calendarService.AuthenticationChanged += (_, authenticated) =>
            Dispatcher.Invoke(() => UpdateAuthStatus(authenticated));

        BuildThemeSwatches();
        LoadValues();
    }

    private void BuildThemeSwatches()
    {
        foreach (var (key, label, swatch, text) in Themes)
        {
            var isSelected = key == _selectedTheme;
            var outerBorder = new Border
            {
                Width = 48, Height = 48,
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0, 0, 8, 8),
                BorderThickness = new Thickness(2),
                BorderBrush = isSelected
                    ? new SolidColorBrush(ParseHex("#4A90D9"))
                    : new SolidColorBrush(Colors.Transparent),
                Cursor = Cursors.Hand,
                Tag = key
            };

            var inner = new Border
            {
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(ParseHex(swatch)),
            };
            var tb = new TextBlock
            {
                Text = label,
                FontSize = 9,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(ParseHex(text)),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(2)
            };
            inner.Child = tb;
            outerBorder.Child = inner;

            outerBorder.MouseLeftButtonDown += (_, _) => SelectTheme(key);
            ThemePanel.Children.Add(outerBorder);
        }
    }

    private void SelectTheme(string key)
    {
        _selectedTheme = key;
        foreach (Border b in ThemePanel.Children.OfType<Border>())
        {
            b.BorderBrush = b.Tag?.ToString() == key
                ? new SolidColorBrush(ParseHex("#4A90D9"))
                : new SolidColorBrush(Colors.Transparent);
        }
    }

    private void LoadValues()
    {
        StartupCheck.IsChecked = StartupService.IsStartupEnabled();
        WidgetEnabledCheck.IsChecked = _settings.WidgetEnabled;

        CompactRadio.IsChecked = _settings.Mode == WidgetMode.Compact;
        NormalRadio.IsChecked = _settings.Mode == WidgetMode.Normal;
        WideRadio.IsChecked = _settings.Mode == WidgetMode.Wide;

        FontSmallRadio.IsChecked  = _settings.FontSizeLevel == -1;
        FontMediumRadio.IsChecked = _settings.FontSizeLevel == 0;
        FontLargeRadio.IsChecked  = _settings.FontSizeLevel == 1;
        FontXLargeRadio.IsChecked = _settings.FontSizeLevel == 2;

        UpdateAuthStatus(_calendarService.IsAuthenticated);
    }

    private void UpdateAuthStatus(bool authenticated)
    {
        if (authenticated)
        {
            AuthStatusText.Text = "연결됨";
            ConnectBtn.Visibility = Visibility.Collapsed;
            DisconnectBtn.Visibility = Visibility.Visible;
        }
        else
        {
            AuthStatusText.Text = "연결된 계정 없음";
            ConnectBtn.Visibility = Visibility.Visible;
            DisconnectBtn.Visibility = Visibility.Collapsed;
        }
    }

    private async void ConnectBtn_Click(object sender, RoutedEventArgs e)
    {
        ConnectBtn.IsEnabled = false;
        ConnectBtn.Content = "연결 중...";
        await _calendarService.AuthenticateAsync();
        ConnectBtn.IsEnabled = true;
        ConnectBtn.Content = "계정 연결";
    }

    private async void DisconnectBtn_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Google 계정 연결을 해제할까요?\n캘린더 데이터가 더 이상 동기화되지 않습니다.",
            "연결 해제", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;
        await _calendarService.RevokeAuthAsync();
    }

    private void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        _settings.WidgetEnabled = WidgetEnabledCheck.IsChecked == true;
        _settings.Mode = CompactRadio.IsChecked == true ? WidgetMode.Compact
                       : WideRadio.IsChecked == true ? WidgetMode.Wide
                       : WidgetMode.Normal;
        _settings.ThemeColor = _selectedTheme;
        _settings.FontSizeLevel = FontSmallRadio.IsChecked == true ? -1
                                 : FontLargeRadio.IsChecked == true ? 1
                                 : FontXLargeRadio.IsChecked == true ? 2
                                 : 0;

        StartupService.SetStartup(StartupCheck.IsChecked == true);
        SettingsManager.Instance.Save(_settings);

        (Application.Current as App)?.ApplyTheme(_selectedTheme);
        (Application.Current as App)?.ApplyFontSize(_settings.FontSizeLevel);

        Close();
    }

    private void CancelBtn_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
