# Error Handling

The app reads files owned by another process and parses an unstable JSONL format. Error handling should preserve the read-only contract, keep the UI responsive, and avoid hiding defects outside expected boundaries.

## Expected Failures

Expected failures include:

- missing or invalid session root path
- active transcript files changing while being read
- malformed or partial JSONL lines
- file watcher events for files that no longer exist
- a transcript being truncated or rotated

Current examples:

- `MainWindowViewModel.RefreshAsync` reports a missing root path in `StatusMessage` and stops the watcher.
- `CodexEventParser.ParseLine` converts malformed JSON into a raw `DisplayEvent` titled `Parse error`.
- `SessionReader.ReadAppendedAsync` resets tail state when a file length shrinks.
- `SessionScanner.TryGetSession` returns `null` for missing or non-JSONL files.

## Catch Rules

- Catch exceptions at UI operation boundaries when the app can show a meaningful status message.
- Catch malformed JSON at the line parser boundary and keep the raw line visible.
- Do not catch broad exceptions inside low-level helpers unless the helper's contract explicitly allows a fallback.

Accepted narrow fallback:

```csharp
private static int EstimateLineCount(string filePath, int maxLines)
{
    try
    {
        // best-effort scan
    }
    catch
    {
        return 0;
    }
}
```

This is acceptable because line count is only a summary hint.

## Cancellation

- Long reads accept `CancellationToken`.
- Selection changes cancel the previous load through `_loadCts`.
- `OperationCanceledException` may be swallowed at the ViewModel boundary because it represents a superseded UI operation.

## Avoid

- Swallowing exceptions in parser classification or UI commands without a user-visible status.
- Retrying filesystem reads in a tight loop.
- Replacing raw parse failures with empty events; raw visibility is part of diagnosis.
