# Implementation Walkthrough

## Modified Files and Details

1. **`src/CXTracer/Services/AppSettings.cs`**
   - Added standard boolean properties `MinimizeToTray` and `CloseToTray`.

2. **`src/CXTracer/ViewModels/MainWindowViewModel.cs`**
   - Declared MVVM observable fields `_minimizeToTray` and `_closeToTray`.
   - Bound saving logic in `OnMinimizeToTrayChanged` and `OnCloseToTrayChanged`.
   - Updated `LoadSettings()` and `SaveSettings()` to serialize these settings.

3. **`src/CXTracer/ViewModels/SettingsWindowViewModel.cs`**
   - Added proxy properties `MinimizeToTray` and `CloseToTray` pointing to the main view model properties.
   - Updated property change event handlers to propagate updates to/from the main view model.

4. **`src/CXTracer/Views/SettingsWindow.axaml`**
   - Increased grid rows from 8 to 12.
   - Added "System Tray" heading and corresponding checkboxes bound to `MinimizeToTray` and `CloseToTray`.

5. **`src/CXTracer/Localization/en-US.axaml` & `zh-CN.axaml`**
   - Added translation resources:
     - `SystemTray`
     - `MinimizeToTray`
     - `CloseToTray`
     - `TrayOpen`
     - `TrayExit`
     - `TrayToolTip`

6. **`src/CXTracer/Views/MainWindow.axaml.cs`**
   - Managed lifecycle of `TrayIcon` dynamically.
   - Overrode `OnClosing` to support hiding the window when `CloseToTray` is active.
   - Overrode `OnPropertyChanged` to intercept `WindowState.Minimized` and hide the window when `MinimizeToTray` is active.
   - Handled dynamic localization changes to update tray labels when current language changes.
