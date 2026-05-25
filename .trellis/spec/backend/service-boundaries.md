# Service Boundaries

Codex Lens has local services rather than backend layers. The important boundary is between filesystem/parsing work and presentation state.

## Services

Services may use filesystem APIs, JSON APIs, and plain model types.

- `SessionScanner` discovers transcript files and creates `SessionInfo` summaries.
- `SessionReader` reads complete files and appended chunks into `DisplayEvent` lists.
- `SessionWatcher` wraps `FileSystemWatcher` and raises normalized session change events.
- `CodexEventParser` parses one JSONL line and classifies it into conversation, execution, or raw panes.

`SessionReader` also owns transcript tail coordination:

- `_tailStates` stores offset, pending partial line, and line number per path.
- `_tailLocks` stores one `SemaphoreSlim` per path so reads for the same file are serialized.
- `_sync` protects both dictionaries and any direct tail-state mutation.
- `Forget(filePath)` clears tail state when a caller intentionally wants the next read to start fresh.

Services should not know about:

- Avalonia controls or visual tree objects
- `MainWindow`
- SukiUI controls
- UI selection, filters, or scroll position

## Models

Models hold display data and simple derived properties.

- `DisplayEvent` contains pane/kind/title/text/raw JSON and brush properties used by the current UI.
- `SessionInfo` contains file metadata, prompt/project summaries, and status text.
- `EventKind` and `EventPane` define parser output categories.

`DisplayEvent` currently references `Avalonia.Media` brushes. Treat this as presentation-model coupling in the single desktop project, not as a domain model pattern to spread into services.

## ViewModel

`MainWindowViewModel` owns:

- observable collections bound by `MainWindow.axaml`
- `RootPath`, search text, selected filter, selected session, busy/status flags
- refresh/load/clear/default-root commands
- filter application and live update coordination
- marshaling watcher events back to `Dispatcher.UIThread`

## View

Code-behind is allowed only for view-only behavior that requires Avalonia visual tree access. Current example: `MainWindow.axaml.cs` scrolls to adjacent rendered message cards by inspecting `ContentPresenter` containers.

## Avoid

- Calling `Directory`, `File`, or `JsonDocument` directly from AXAML code-behind.
- Moving visual tree logic into `MainWindowViewModel`.
- Adding a repository or domain layer for the current transcript viewer behavior.
