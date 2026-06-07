# Implementation Checklist

## Step 1: Add `IsEnriched` to `SessionInfo`

- [ ] Add `public bool IsEnriched { get; set; }` (plain auto-property)
- [ ] In `CopySummaryFrom`, set `IsEnriched = true` after copying fields

**File**: `src/CXTracer/Models/SessionInfo.cs`

## Step 2: Add background enrichment to `MainWindowViewModel`

- [ ] Add `private CancellationTokenSource? _enrichCts` field
- [ ] Add `StartBackgroundEnrichmentAsync(CancellationToken ct)` method:
  - Takes a snapshot of `Sessions.Where(s => !s.IsEnriched).ToList()`
  - Uses `Parallel.ForEachAsync` with `MaxDegreeOfParallelism = 4`
  - For each session: `Task.Run(() => _scanner.TryGetSessionSummary(path))`
  - On success: `Dispatcher.UIThread.Post(() => { session.CopySummaryFrom(summary); })`
  - Catches `OperationCanceledException` silently, logs other exceptions to `StatusMessage`
- [ ] In `RefreshAsync`, after `AddSessionsAsync`:
  - Cancel `_enrichCts`, create new one
  - Fire `_ = StartBackgroundEnrichmentAsync(_enrichCts.Token)`
- [ ] In `LoadSelectedSessionAsync`, after `session.CopySummaryFrom(summary)`:
  - Set `session.IsEnriched = true` (prevents background re-enrichment)

**File**: `src/CXTracer/ViewModels/MainWindowViewModel.cs`

## Step 3: Enrich newly upserted sessions

- [ ] In `UpsertSession`, when a NEW session is inserted (the `existing is null` branch):
  - After `Sessions.Insert(0, info)`, fire-and-forget enrich:
    ```
    _ = EnrichSingleSessionAsync(info);
    ```
- [ ] Add `EnrichSingleSessionAsync(SessionInfo session)` helper (same pattern as background loop but for one item)

**File**: `src/CXTracer/ViewModels/MainWindowViewModel.cs`

## Step 4: Cancellation cleanup in Dispose

- [ ] In `Dispose()`, cancel `_enrichCts`

**File**: `src/CXTracer/ViewModels/MainWindowViewModel.cs`

## Validation

```bash
dotnet build src/CXTracer/CXTracer.csproj --nologo -v q
```

- Launch app → all session cards should progressively show real titles within 1–2 seconds
- Click refresh → titles re-resolve
- Verify clicking a session still works normally
- Verify new sessions from file watcher get enriched
