# Session List Details Visibility

## Goal

Ensure all session cards in the left-hand session list show their detailed titles (first user prompt) and project hints automatically, without requiring the user to click each session individually.

## User Value

Users cannot distinguish sessions at a glance because unselected sessions display only the raw filename (e.g. `rollout.jsonl`) and "Unknown project". Automatically enriching all sessions lets users scan, identify, and navigate the session list efficiently.

## Confirmed Facts

- **`ScanLight`** (`SessionScanner.cs:19`): Returns `SessionInfo.FromFile()` instances with only basic file metadata (`FileName`, `LastWriteTime`, `Length`). Does NOT read file content.
- **`Enrich`** (`SessionScanner.cs:53`): Reads the first 200 lines to extract `FirstPrompt`, `ProjectHint`, and `EventCount`. Called inside `TryGetSessionSummary`.
- **Enrichment only on selection**: `LoadSelectedSessionAsync` (`MainWindowViewModel.cs:360`) is the sole caller of `TryGetSessionSummary`, triggered by clicking a session.
- **`UpsertSession`** (`MainWindowViewModel.cs:485`): Called when file watcher detects changes. Uses `TryGetSession` (no enrichment), so newly discovered sessions also lack titles.
- **`DisplayTitle`** (`SessionInfo.cs:27`): Falls back to `FileName` when `FirstPrompt` is empty.
- **Performance**: Each file read ≈1–5ms on SSD. 300 files × 4 parallel = ~1–2s total, entirely off the UI thread.

## Requirements

### REQ-1: Background parallel enrichment after scan

- **AC-1.1**: `RefreshAsync` displays the session list immediately using basic file names.
- **AC-1.2**: After the list is rendered, a background task enriches all unenriched sessions using `TryGetSessionSummary`, with `MaxDegreeOfParallelism = 4`.
- **AC-1.3**: Each enriched session's properties update on the UI thread via `CopySummaryFrom`, causing `DisplayTitle` and `DisplaySubtitle` to refresh in place.

### REQ-2: Enrichment skipping and deduplication

- **AC-2.1**: Sessions already enriched (by user selection or a previous background pass) are skipped.
- **AC-2.2**: An `IsEnriched` flag on `SessionInfo` tracks enrichment state.

### REQ-3: New session enrichment

- **AC-3.1**: When `UpsertSession` inserts a new session, it is also asynchronously enriched.

### REQ-4: Cancellation and lifecycle

- **AC-4.1**: A new scan/refresh cancels any active background enrichment.
- **AC-4.2**: `Dispose` cancels background enrichment.

## Out of Scope

- Persistent metadata cache (summaries are re-read from files on each startup)
- Viewport-based lazy loading (rejected: I/O cost is negligible, and pre-loading provides better UX)
