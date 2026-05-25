# Async Initial Session Loading Design

## Scope

Fix startup and explicit session-load freezes by changing two behaviors:

1. Session discovery becomes lightweight for non-newest sessions.
2. Transcript event population becomes progressive instead of one large UI-thread update.

## Current Problem

`MainWindowViewModel` starts `RefreshAsync()` from the constructor. `RefreshAsync()` scans up to 300 files, fills `Sessions`, then auto-selects the newest session. Selection triggers `LoadSelectedSessionAsync()`, which reads the whole file and calls `ApplyFilter()`. `ApplyFilter()` clears and fills UI-bound `ObservableCollection` instances synchronously, which can freeze Avalonia while cards are generated.

`SessionScanner.Scan()` also reads head lines and estimates line count for every discovered file. That means startup touches many transcript files before the user asks for them.

## Proposed Behavior

- Startup discovery enumerates `*.jsonl` files and creates metadata-only `SessionInfo` rows for the session list.
- Only the newest session gets enriched and loaded automatically.
- Older sessions keep filename/time/path metadata until selected.
- Selecting any session cancels the previous load, enriches that session if needed, reads the transcript, and progressively populates visible event collections.
- Existing read-only file access rules remain unchanged.

## Data Flow

Startup:

```text
MainWindowViewModel constructor
  -> RefreshAsync()
  -> SessionScanner.ScanLight()
  -> batch-add SessionInfo rows to UI
  -> select/load newest session only
```

User selection:

```text
SelectedSession changed
  -> cancel previous _loadCts
  -> LoadSelectedSessionAsync(session)
  -> ensure selected session summary is enriched if needed
  -> SessionReader.ReadAllAsync(filePath, ct)
  -> progressively add visible events to UI-bound collections
```

Watcher update:

```text
SessionWatcher event
  -> debounce by path
  -> update metadata for changed file
  -> append events only if the changed file is selected
```

## Contracts

`SessionScanner` should expose two levels of work:

- Lightweight discovery: enumerate files and create `SessionInfo.FromFile(file)` only.
- Enrichment: read head lines, first prompt, project hint, and estimated line count for one selected or newest file.

`MainWindowViewModel` should keep all `ObservableCollection` mutation on the UI thread, but large additions should be chunked and yield between batches.

Suggested event population contract:

- Clear visible event collections once at the start of a load.
- Add events in batches.
- Check cancellation between batches.
- Yield back to Avalonia between batches.
- Keep counts accurate after each batch or at batch boundaries.

## Compatibility

- Keep current `SessionInfo` properties and bindings.
- Keep `SelectedSession` as the selection trigger.
- Keep `RefreshCommand` and `OpenDefaultRootCommand`.
- Keep `PinSelectedSession` behavior for live updates.
- No database, cache, or writes under `.codex/sessions`.

## Trade-Offs

- Older session titles may initially fall back to filename because first prompt/project hint are intentionally lazy.
- Progressive rendering means the selected transcript may visibly fill in over a short period instead of appearing all at once.
- Avoiding virtualization in this task keeps scope smaller, but chunking collection updates is still required.

## Risks

- Property changes on `SessionInfo` need to notify computed binding properties such as `DisplayTitle`, `DisplaySubtitle`, and `StatusText`.
- Selection changes during progressive population must cancel cleanly to avoid mixing events from two sessions.
- Sorting sessions during watcher updates must not retrigger unwanted selection loads.
