# PRD: System Tray Icon and Minimize/Close to Tray Settings

## Goal and User Value
Currently, CXTracer runs as a standard desktop window. Closing the window exits the application, and minimizing it leaves it in the taskbar. 

This task will:
- Add a system tray icon (TrayIcon) using the existing application icon.
- Introduce settings in the Settings panel:
  - **Minimize to Tray**: When minimized, hide the main window and keep the app running in the system tray.
  - **Close to Tray**: When closed, hide the main window instead of exiting.
- Provide a system tray context menu with "Open" and "Exit" actions to restore the window or exit the application completely.

## Confirmed Facts
- **Icon Files**: The application icon files are located in `src/CXTracer/Icons/` (including `AppIcon48.ico` and others).
- **Settings System**: AppSettings are loaded/saved by `AppSettingsService` and defined in `AppSettings.cs` (serialized using `AppJsonContext`).
- **Settings UI**: The settings are displayed in `SettingsWindow.axaml` and bound to `SettingsWindowViewModel.cs`.
- **Window Class**: The main window is `MainWindow.axaml` / `MainWindow.axaml.cs` which inherits from `SukiWindow`.

## Requirements

### REQ-1: Tray Icon Core Functionality
- **REQ-1.1**: A system tray icon is displayed when the application is running. It is always visible as long as the application is running.
- **REQ-1.2**: The tray icon uses `AppIcon48.ico` (registered as `AppIcon` in `Icons.axaml`).
- **REQ-1.3**: Clicking or double-clicking the tray icon restores and focuses the main window.
- **REQ-1.4**: The tray icon has a context menu (NativeMenu) with:
  - **Open/Restore**: Restores and focuses the main window.
  - **Exit**: Closes the application completely (bypassing close-to-tray settings).

### REQ-2: Settings Panel Integration
- **REQ-2.1**: Add two toggle options in `SettingsWindow.axaml`:
  - "Minimize to Tray" (最小化到系统托盘)
  - "Close to Tray" (关闭到系统托盘)
- **REQ-2.2**: The settings are loaded on startup, saved to `settings.json` upon change, and support localization (English and Chinese).

### REQ-3: Minimize & Close Behavior Customization
- **REQ-3.1**: If "Minimize to Tray" is enabled:
  - When the user minimizes the main window, the window is hidden (`Hide()`) and removed from the OS taskbar, keeping the tray icon visible.
- **REQ-3.2**: If "Close to Tray" is enabled:
  - When the user closes the main window (using the title bar Close button or Alt+F4), the window is hidden (`Hide()`) instead of closing, and the app keeps running in the background.
- **REQ-3.3**: Ensure that selecting "Exit" from the tray icon menu completely shuts down the application, avoiding cancel loops.

## Out of Scope
- Customizing tray icon colors or animations.
- Dynamic hiding of the tray icon itself (it remains visible as long as the app is running).

## Open Questions
- None.
