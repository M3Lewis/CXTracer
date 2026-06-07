# Design: Session List Background Enrichment

## Architecture

The enrichment system is a fire-and-forget background loop managed by `MainWindowViewModel`. It runs after each session scan completes and enriches sessions in parallel.

### Data Flow

```
RefreshAsync()
  └─ ScanLight() → SessionInfo[] (basic metadata only)
  └─ AddSessionsAsync() → renders list immediately
  └─ StartBackgroundEnrichmentAsync(ct)
       └─ Parallel.ForEachAsync(sessions, maxDegree=4)
            ├─ Task.Run(() => _scanner.TryGetSessionSummary(path))
            └─ Dispatcher.UIThread → session.CopySummaryFrom(summary)
                                   → session.IsEnriched = true
```

### New State

| Location | Addition |
|---|---|
| `SessionInfo` | `public bool IsEnriched { get; set; }` (simple auto-property, no need for observable — it's bookkeeping only, not bound to UI) |
| `MainWindowViewModel` | `private CancellationTokenSource? _enrichCts` field |
| `MainWindowViewModel` | `StartBackgroundEnrichmentAsync(CancellationToken)` method |

### Key Decisions

1. **`IsEnriched` is NOT observable**: It's only checked by the background loop to skip already-enriched sessions. No UI binding needed.
2. **`CopySummaryFrom` already exists and handles property notifications**: `FirstPrompt`, `ProjectHint`, `EventCount`, `LastWriteTime`, `Length` are all `[ObservableProperty]` fields, so setting them triggers `PropertyChanged` → `DisplayTitle`/`DisplaySubtitle` update automatically.
3. **`LoadSelectedSessionAsync` must set `IsEnriched = true`**: After calling `session.CopySummaryFrom(summary)`, mark the session so the background loop skips it.
4. **`UpsertSession` enrichment**: After inserting a new session at index 0, fire-and-forget an async enrichment call for that single session.
5. **Cancellation chain**: `_enrichCts` is cancelled at the start of `RefreshAsync` and in `Dispose`.

### Compatibility

- No changes to `SessionScanner` or `CodexEventParser` — all existing enrichment logic is reused.
- No changes to AXAML — `DisplayTitle` binding already works; it just needs the data.
- `CopySummaryFrom` is called from the UI thread (via Dispatcher), same pattern as `LoadSelectedSessionAsync`.
