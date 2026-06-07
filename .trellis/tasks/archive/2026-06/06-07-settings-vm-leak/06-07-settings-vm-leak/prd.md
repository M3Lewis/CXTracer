# PRD: Settings VM Memory Leak

## Goal
Fix the memory leak caused by `SettingsWindowViewModel` subscribing to `MainWindowViewModel.PropertyChanged` but never unsubscribing when the settings window is closed.

## User Value
Ensures the app does not accumulate leaked windows and view models in memory during extended usage.

## Confirmed Facts
- `SettingsWindowViewModel` subscribes to `_main.PropertyChanged` in its constructor.
- `SettingsWindowViewModel` implements `IDisposable` to unsubscribe.
- `MainWindow.Settings_Click` constructs a new `SettingsWindowViewModel` and sets it as the `DataContext` of `SettingsWindow`.
- When the window closes, `Dispose()` is never called.

## Requirements
- **REQ-1**: Explicitly dispose of `SettingsWindowViewModel` when the Settings window is closed.
- **REQ-2**: Verify that the subscription cleanup prevents memory leaks.
