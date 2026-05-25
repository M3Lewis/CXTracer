# Make initial session loading asynchronous

## Goal

Fix startup UI freeze by making the first session scan/load path asynchronous and keeping the window responsive while sessions initialize.

## Requirements

- The main window must become interactive immediately on first launch.
- Initial session discovery must not block the UI thread.
- UI collection updates after discovery must avoid long UI-thread stalls when many sessions exist.
- On startup, automatically load only the newest session transcript.
- Startup must not read transcript content from older sessions.
- Older sessions must be loaded only after explicit user selection.
- Loading the newest transcript must not freeze the UI during startup.
- User-initiated transcript loading must not freeze the UI while events are added and rendered.
- The session list may show filename/metadata-only placeholders for unloaded sessions until a session is explicitly loaded.
- Existing manual `Refresh` behavior should remain available.
- Existing read-only contract must remain unchanged: no writes to `.codex/sessions`.

## Acceptance Criteria

- [x] On first launch, the window renders and accepts input while session discovery is still running.
- [x] Startup shows a busy/progress status while scanning sessions.
- [x] The session list is populated without a long single UI-thread batch update.
- [x] Opening or loading a transcript remains cancellable.
- [x] Startup reads transcript content for the newest session only.
- [x] Non-newest session transcript content is not read until the user clicks that session.
- [x] Clicking a session keeps the UI responsive while transcript events are loaded into Conversation, Execution, and Raw views.
- [x] The newest session can render progressively without blocking input.
- [x] `dotnet build .\CodexLens.sln` passes.

## Notes

- Reported bug: first software open freezes the UI because loading work runs during startup.
- Evidence from code:
  - `MainWindowViewModel` starts `_ = RefreshAsync();` inside the constructor.
  - `RefreshAsync` scans with `Task.Run`, then clears and fills `Sessions` on the UI thread.
  - After scan, it auto-selects `Sessions[0]`, which triggers `LoadSelectedSessionAsync`.
  - `LoadSelectedSessionAsync` reads the transcript and then populates conversation/execution/raw collections.
  - `SessionReader.ReadAllAsync` reads and parses asynchronously, but after it returns, `ApplyFilter()` clears and fills UI-bound `ObservableCollection` instances synchronously on the UI thread.
  - Large transcripts can therefore freeze the UI even when loaded by an explicit user click.
  - `SessionScanner.Scan` samples up to 300 files and reads head lines plus line counts for each.
- Existing relevant specs:
  - `frontend/state-management.md`: async commands, cancellation, UI-thread collection updates.
  - `backend/quality-guidelines.md`: long scans stay off the UI thread.
- User decision:
  - Include user-clicked transcript loading in this task.
  - Default startup may load the newest session.
  - Default startup must not load transcript content for any non-newest session.

## Open Questions

- None blocking. Planning can proceed with the current scope.
