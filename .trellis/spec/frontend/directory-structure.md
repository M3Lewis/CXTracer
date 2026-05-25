# Frontend Directory Structure

The current UI is intentionally small: one Suki window, one ViewModel, and supporting models/services.

```text
src/CodexLens/
  App.axaml
  App.axaml.cs
  Program.cs
  Icons/Icons.axaml
  Views/MainWindow.axaml
  Views/MainWindow.axaml.cs
  ViewModels/MainWindowViewModel.cs
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

`Views/MainWindow.axaml.cs` owns only view-only scroll behavior that requires rendered visual tree access.

## ViewModels

`ViewModels/MainWindowViewModel.cs` owns:

- observable collections for sessions and display events
- selected session and filter state
- search and raw visibility state
- busy and status text
- refresh, clear search, and default-root commands
- live update handling from `SessionWatcher`

## Icons and Resources

`Icons/Icons.axaml` stores reusable path resources loaded from `App.axaml`.

Use static resources for repeated vector paths, as seen with `IconChevronUp` and `IconChevronDown`. Avoid duplicating inline path data across buttons.

## When to Split

Split files only when a new feature creates a real boundary:

- a reusable visual fragment used in more than one place
- a second top-level screen or dialog
- a ViewModel section with independent lifecycle or tests
- shared converters or attached properties used by multiple views

## Avoid

- Creating `Pages/`, `Controls/`, `Converters/`, or `Behaviors/` folders before there is code to put in them.
- Moving service logic into `Views/`.
- Splitting `MainWindowViewModel` just to make it look layered.
