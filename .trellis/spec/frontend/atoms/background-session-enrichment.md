---
id: frontend.background-session-enrichment
type: performance
priority: must
applies_when:
  - scanning or loading the session directory list
  - watching for live session file updates
code_anchors:
  - src/CXTracer/ViewModels/MainWindowViewModel.cs
  - src/CXTracer/Services/SessionScanner.cs
verify:
  - session list renders immediately with raw filenames on start/refresh
  - enrichment runs off the UI thread and updates properties on the UI thread
  - active enrichment cancels when a new refresh is triggered or window is disposed
source:
  kind: human_confirmed
  ref: task-2026-06-07-session-list-details
last_checked: 2026-06-07
---

# Rule

To keep the application highly responsive while loading numerous sessions, implement a two-pass loading flow:

1. **First Pass (Synchronous/Light)**:
   - Call `ScanLight()` to extract basic file properties (`FileName`, `LastWriteTime`, `Length`) quickly. Add these light session objects to the bound collection to display the list instantly.

2. **Second Pass (Background/Parallel)**:
   - Run file enrichment (reading the first 200 lines to parse the first prompt and project hint) on a background thread.
   - Use `Parallel.ForEachAsync` with a controlled degree of parallelism (`MaxDegreeOfParallelism = 4`).
   - Track enrichment status using a non-observable `IsEnriched` property on the session model to prevent redundant parsing.
   - Dispatch property updates back to the UI thread via `Dispatcher.UIThread` so bound UI components refresh seamlessly.

3. **Cancellation & Lifecycle**:
   - Maintain a class-scoped `CancellationTokenSource? _enrichCts`.
   - Cancel the active token source before starting a new scan/refresh, and during ViewModel `Dispose()`.

# Why

- Reading hundreds of log files synchronously on the UI thread blocks rendering and causes the app window to freeze during directory scans.
- Background parallelization utilizes SSD disk bandwidth efficiently without locking or oversubscribing UI rendering resources.
- Strict cancellation management prevents race conditions, memory leaks, and background updates to discarded/stale session lists.
