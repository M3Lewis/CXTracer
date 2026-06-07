# Service Directory Structure

CXTracer is currently a single-project desktop app:

```text
src/CXTracer/
  App.axaml
  App.axaml.cs
  Program.cs
  Icons/
    Icons.axaml
  Models/
    DisplayEvent.cs
    EventKind.cs
    EventPane.cs
    SessionFileChangedEventArgs.cs
    SessionInfo.cs
  Services/
    CodexEventParser.cs
    SessionFileAccess.cs
    SessionReader.cs
    SessionScanner.cs
    SessionWatcher.cs
  ViewModels/
    MainWindowViewModel.cs
  Views/
    MainWindow.axaml
    MainWindow.axaml.cs
```

## Responsibilities

- `Services/` owns transcript IO, JSONL parsing, scanning, tailing, and file watching.
- `Models/` owns simple data objects and display projection objects used by the UI.
- `ViewModels/` owns screen state, commands, filtering, selection, and UI-thread coordination.
- `Views/` owns AXAML layout and view-only behavior such as scrolling within visible item containers.
- `Icons/Icons.axaml` owns reusable vector path resources loaded from `App.axaml`.

## Current Examples

- `Services/SessionScanner.cs` enumerates `*.jsonl` files and builds `SessionInfo` summaries.
- `Services/SessionFileAccess.cs` owns shared-open `FileStream` creation for active transcript files.
- `Services/SessionReader.cs` reads whole transcript files and appended chunks while preserving tail state.
- `Services/CodexEventParser.cs` classifies unknown Codex JSONL shapes into display events.
- `ViewModels/MainWindowViewModel.cs` orchestrates refresh, selection, filters, and live updates.
- `Views/MainWindow.axaml.cs` contains scroll navigation glue that needs Avalonia visual tree access.

## Placement Rules

- New transcript parsing logic belongs in `CodexEventParser` or a narrowly named service under `Services/`.
- New file access code belongs in a service, not in a ViewModel or code-behind.
- New transcript file read paths should reuse `SessionFileAccess.OpenReadShared(...)` instead of duplicating file-open flags.
- New user-visible state belongs in `MainWindowViewModel` until there are multiple screens that justify splitting ViewModels.
- New display-only fields belong on model types only when they are reused by bindings.

## Avoid

- Adding Clean Architecture projects before there is a real domain boundary.
- Putting JSON parsing or filesystem traversal in AXAML code-behind.
- Creating generic `Utils` folders for unrelated helpers.
- Tracking `bin/` or `obj/` output; root `.gitignore` excludes both.
