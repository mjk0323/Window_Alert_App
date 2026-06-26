# Window Alert App — Claude Instructions

## Project
C# 12 / .NET 8 / WPF desktop widget. Google Calendar sync + Windows toast notifications.
See `master_plan.md` for full architecture.

## Core Rules

### 1. Minimize Code Changes
- If one line is enough, change **only that line**.
- Never touch code outside the scope of the request.
- No refactoring, renaming, or cleanup unless explicitly asked.

### 2. No Bloat
- Write the shortest correct solution — always prefer concise over verbose.
- Favor inline logic over extracting new methods/classes unless reuse is certain.
- No helper abstractions for one-off operations.
- No fallback/error handling for impossible scenarios.
- No comments unless the WHY is genuinely non-obvious.
- Three similar lines is better than a premature abstraction.

### 2a. Keep the Codebase Small
- Don't create new files when an existing file can hold the change.
- Don't introduce new classes, interfaces, or layers unless strictly necessary.
- If a function can be removed rather than replaced, remove it.
- Prefer deleting dead code over leaving it commented out.

### 3. Test After Every Change
- Build and run the app after each change.
- Verify the specific feature works end-to-end.
- Report clearly: "works" or "doesn't work + reason".
- Never claim success without testing.

### 4. No Unrequested Features
- Implement exactly what was asked. Nothing more.
- No "nice to have" extras, no design improvements, no future-proofing.

## General Best Practices
- Prefer editing existing files over creating new ones.
- No security vulnerabilities (validate only at system boundaries).
- Use `async/await` consistently — no blocking calls on UI thread.
- Follow existing MVVM patterns (CommunityToolkit.Mvvm source generators).
- Match the existing code style — don't introduce new patterns without reason.

## Known Pitfalls
- **WPF + WinForms namespace conflict**: Tray code uses `System.Windows.Forms`.
  Use alias `using WinForms = System.Windows.Forms;` if both namespaces needed in one file.
- **SettingsManager** is a singleton — access via `SettingsManager.Instance`.
- **CompletedEventsStore** auto-purges after 1 day — don't rely on stale data.

## Build
```powershell
dotnet build    # compile check
dotnet run      # run app
```
