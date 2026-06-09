# System Tray Technical Design

## System Tray Integration
- We initialize the tray icon programmatically within the `MainWindow` lifecycle.
- The tray icon loads the application standard `AppIcon` (`AppIcon48.ico`) from the shared resource dictionary.
- A `NativeMenu` is created with two options:
  - **Open CXTracer**: Restores the main window to active focus.
  - **Exit**: Sets a boolean flag to skip tray interceptors and shuts down the application cleanly.
- The tray icon is always visible when the application runs, as confirmed by the user.

## Window Interception Rules
- **Minimize to Tray**: Overrode `OnPropertyChanged` on `MainWindow`. When `WindowState` changes to `Minimized` and `MinimizeToTray` settings is enabled, we invoke `Hide()`.
- **Close to Tray**: Overrode `OnClosing` on `MainWindow`. When the window closing event is triggered, unless the "Exit" menu item from the tray icon is clicked (setting `_isExiting = true`), we cancel the close event (`e.Cancel = true;`) and call `Hide()`.

## Dynamic Translation Support
- We hook the `PropertyChanged` event on the `MainWindowViewModel` from the code-behind of `MainWindow`.
- When the `CurrentLanguage` property changes, we immediately update the tray icon's context menu labels and tooltip using localized resource keys via `viewModel.L(...)`.
- This ensures full support for en-US / zh-CN without requiring app restart.

## AOT-Compatible Settings
- Added `MinimizeToTray` and `CloseToTray` directly to `AppSettings` model class.
- Since `AppSettings` is registered in `AppJsonContext` for source generation, serializing these boolean properties does not violate Native AOT restrictions.
