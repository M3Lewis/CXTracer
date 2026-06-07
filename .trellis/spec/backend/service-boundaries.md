# Service Boundaries

CXTracer has local services rather than backend layers. The important boundary is between filesystem/parsing work and presentation state.

## Services

Services may use filesystem APIs, JSON APIs, and plain model types.

- `SessionScanner` discovers transcript files and creates `SessionInfo` summaries.
- `SessionReader` reads complete files and appended chunks into `DisplayEvent` lists.
- `SessionFileAccess` owns the shared `FileStream` open policy for reading active transcript files.
- `SessionWatcher` wraps `FileSystemWatcher` and raises normalized session change events.
- `CodexEventParser` parses one JSONL line and classifies it into conversation, execution, or raw panes.
- `AppSettingsService` reads and writes CXTracer app preferences, not Codex session transcript files.

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

`AppSettingsService` contract:

- Location: a CXTracer-specific directory under `Environment.SpecialFolder.LocalApplicationData`.
- Format: JSON.
- Current fields: `IsSynchronizedNavigationEnabled` and `SyncNavigationShortcut`.
- Missing file: return default settings.
- Invalid or inaccessible file: let the caller catch and report a user-visible status.
- Writes: create the settings directory if needed, write a temporary JSON file, then replace the settings file.

This service is allowed to write app preferences. It must never write to the Codex CLI session tree.

`SessionFileAccess` contract:

- Open transcript files with `FileAccess.Read`.
- Share active files with `FileShare.ReadWrite | FileShare.Delete`.
- Use the shared buffer size and sequential-scan options from `SessionFileAccess`.
- New transcript readers should call `SessionFileAccess.OpenReadShared(...)` instead of duplicating `new FileStream(...)` arguments.

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
- persisted navigation preference state and the timing of settings saves

`SettingsWindowViewModel` may proxy settings owned by `MainWindowViewModel`, but should not duplicate persistence state. This keeps global keyboard behavior and the settings window reading the same source of truth.

## View

Code-behind is allowed only for view-only behavior that requires Avalonia visual tree access. Current example: `MainWindow.axaml.cs` scrolls to adjacent rendered message cards by inspecting `ContentPresenter` containers.

## Avoid

- Calling `Directory`, `File`, or `JsonDocument` directly from AXAML code-behind.
- Moving visual tree logic into `MainWindowViewModel`.
- Adding a repository or domain layer for the current transcript viewer behavior.
