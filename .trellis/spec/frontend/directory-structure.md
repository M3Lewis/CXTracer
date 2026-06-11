# Frontend Directory Structure

The current UI structure decomposes large view and view model files into cohesive partial classes and child controls to keep them maintainable.

```text
src/CXTracer/
  App.axaml
  App.axaml.cs
  Program.cs
  Icons/Icons.axaml
  Views/
    MainWindow.axaml
    MainWindow.axaml.cs
    MainWindow.Input.cs
    MainWindow.Navigation.cs
    MainWindow.Tray.cs
    DetailPopupOverlay.axaml
    DetailPopupOverlay.axaml.cs
    ImageViewerOverlay.axaml
    ImageViewerOverlay.axaml.cs
    SettingsWindow.axaml
    SettingsWindow.axaml.cs
  ViewModels/
    MainWindowViewModel.cs
    MainWindowViewModel.Commands.cs
    MainWindowViewModel.Filtering.cs
    MainWindowViewModel.Navigation.cs
    MainWindowViewModel.Properties.cs
    MainWindowViewModel.Sessions.cs
    MainWindowViewModel.Settings.cs
    FilterOptionItem.cs
    SettingsWindowViewModel.cs
  Models/
  Services/
```

## Views

`Views/MainWindow.axaml` owns:
- the `SukiWindow` shell
- sessions root toolbar
- session list
- conversation pane
- execution pane
- raw events expander
- status/progress footer

Modals and complex overlays (such as the message detail panel and the image viewer) are extracted into dedicated `UserControl` classes (`DetailPopupOverlay`, `ImageViewerOverlay`) to keep the main layout clean and maintainable.

The view code-behind is split by concern into:
- `MainWindow.axaml.cs`: Core setup, lifecycle, and view model registration.
- `MainWindow.Input.cs`: Keyboard navigation events, keyboard shortcuts, and clipboard handlers.
- `MainWindow.Navigation.cs`: Scroll view synchronization and item anchoring.
- `MainWindow.Tray.cs`: System tray icon lifecycle and window state management.

## ViewModels

`ViewModels/MainWindowViewModel.cs` and its partial siblings own:
- **Core / Lifecycle** (`MainWindowViewModel.cs`): Fields, collections, constructor, and cleanup.
- **Properties** (`MainWindowViewModel.Properties.cs`): Observable property definitions and change handlers.
- **Commands** (`MainWindowViewModel.Commands.cs`): Relay commands and shortcut capture triggers.
- **Sessions** (`MainWindowViewModel.Sessions.cs`): Session file scanning and enrichment logic.
- **Filtering** (`MainWindowViewModel.Filtering.cs`): Event search and search-highlight queries.
- **Navigation** (`MainWindowViewModel.Navigation.cs`): Transcript navigation and sync targets.
- **Settings** (`MainWindowViewModel.Settings.cs`): Localization and app settings.

## Icons and Resources

`Icons/Icons.axaml` stores reusable path resources loaded from `App.axaml`.

Use static resources for repeated vector paths, as seen with `IconChevronUp` and `IconChevronDown`. Avoid duplicating inline path data across buttons.

## When to Split

Split files only when a new feature creates a real boundary:
- a reusable visual fragment used in more than one place
- a second top-level screen or dialog
- a ViewModel section with independent lifecycle or tests
- shared converters or attached properties used by multiple views
- a source file exceeding size limits (see [File Size Limits](./atoms/file-size-limits.md))

## Avoid

- Creating `Pages/`, `Controls/`, `Converters/`, or `Behaviors/` folders before there is code to put in them.
- Moving service logic into `Views/`.
- Splitting ViewModels into artificial layers that break MVVM data bindings. Keep them as cohesive partial classes.

