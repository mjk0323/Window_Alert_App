# Window Alert App — Master Plan

## Purpose
Windows desktop widget that syncs Google Calendar and shows event notifications.
Runs in the system tray; displays a floating widget with multiple view modes.

## Tech Stack
| Layer | Technology |
|-------|-----------|
| Language | C# 12 / .NET 8 |
| UI | WPF (main UI) + WinForms (system tray) |
| MVVM | CommunityToolkit.Mvvm 8.4 (source generators) |
| Calendar API | Google.Apis.Calendar.v3 |
| Notifications | Microsoft.Toolkit.Uwp.Notifications (toast) |
| Packaging | MSIX (Windows App Store) |
| Target OS | Windows 10 19041+ |

## Features
1. Google Calendar OAuth 2.0 sync (polling every N minutes)
2. Toast notifications (configurable advance time, snooze)
3. Widget view modes: Compact / Normal / Wide
4. Day detail popup + event edit window
5. System tray icon with context menu
6. 6 themes (White, Blue, Purple, Green, Pink, Dark)
7. Mark events as completed (1-day expiry)
8. Windows startup auto-launch (registry)
9. MSIX packaging for distribution

## Directory Structure
```
Window_Alert_App/
├── Core/               # Business logic services
│   ├── GoogleCalendarService.cs
│   ├── NotificationScheduler.cs
│   ├── ToastNotificationService.cs
│   └── StartupService.cs
├── Models/             # Data models + ViewModels
│   ├── CalendarEvent.cs
│   ├── CalendarEventViewModel.cs
│   └── AppSettings.cs
├── Settings/           # Persistence
│   ├── SettingsManager.cs
│   └── CompletedEventsStore.cs
├── UI/                 # Settings window
│   └── SettingsWindow.xaml(.cs)
├── Views/              # Widget view modes
│   ├── NormalView.xaml(.cs)
│   ├── CompactView.xaml(.cs)
│   ├── WideWindow.xaml(.cs)
│   ├── DayDetailPopup.xaml(.cs)
│   └── EventEditWindow.xaml(.cs)
├── Widget/             # Main widget
│   ├── WidgetWindow.xaml(.cs)
│   ├── WidgetViewModel.cs
│   └── DesktopLayerHelper.cs
├── Controls/           # Custom controls
│   └── CalendarGrid.xaml(.cs)
├── Tray/               # System tray
│   └── TrayManager.cs
├── Resources/          # Styles + assets
│   └── Styles.xaml
├── Packaging/          # MSIX packaging
│   ├── Package.appxmanifest
│   └── build-msix.ps1
└── Secrets/            # OAuth credentials (git-ignored)
    └── client_secrets.json
```

## Key Files
| File | Role |
|------|------|
| App.xaml.cs | App entry point, service wiring |
| Core/GoogleCalendarService.cs | Google Calendar API client |
| Core/NotificationScheduler.cs | Polling loop + notification timers |
| Core/ToastNotificationService.cs | Windows toast formatting + display |
| Widget/WidgetViewModel.cs | Main MVVM view model |
| Widget/WidgetWindow.xaml.cs | Main widget window (drag, mode switch) |
| Settings/SettingsManager.cs | JSON settings persistence singleton |
| Tray/TrayManager.cs | System tray icon + context menu |

## Build & Run
```powershell
dotnet build
dotnet run
```

## Package for Distribution
```powershell
.\Packaging\build-msix.ps1
```
