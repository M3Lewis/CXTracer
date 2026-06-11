# Product Requirements Document

## Goal and User Value
`MainWindow.axaml.cs` is over 630 lines of code. It contains a mix of core window logic, complex tray icon lifecycle management, custom scroll synchronization/virtualized layout geometry calculation, and keyboard inputs/copy operations. To improve codebase maintainability and ensure we adhere to the single-responsibility principles in the View layer, we will reorganize and refactor `MainWindow.axaml.cs` using `partial class` splits.

## Confirmed Facts
- `MainWindow` is a partial class inheriting from `SukiWindow`.
- It currently holds tray variables (`_trayIcon`, `_openMenuItem`, etc.), scroll coordinates (`_conversationScrollViewer`, etc.), and view model tracking.
- Reorganizing it into separate partial files will not change visual appearance, layout, or behaviors.

## Requirements
1. **Partial Class Separation**: Split `MainWindow.axaml.cs` into:
   - `MainWindow.axaml.cs`: Core window setup, view model registrations, and basic click events (e.g. settings).
   - `MainWindow.Tray.cs`: System tray icon lifecycle (initialize, update, remove, minimize/close intercepting).
   - `MainWindow.Navigation.cs`: Scroll view synchronizer, binary search visual item anchoring, and relative message navigation.
   - `MainWindow.Input.cs`: Keyboard navigation events, text input state detection, and clipboard helpers.
2. **Namespace Integrity**: Keep everything in `CXTracer.Views` namespace.
3. **No Behavior Modification**: Keep the logic identical to avoid regressions in SukiUI interactions, tray restore, or keyboard focus.

## Acceptance Criteria
- Code splits are completed cleanly.
- The app builds with zero compilation errors.
- Clicking the close button minimized to tray continues to work correctly, keyboard navigation remains functional, and scroll alignment behaves identically.
