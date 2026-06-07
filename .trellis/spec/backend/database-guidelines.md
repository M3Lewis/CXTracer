# Data and Persistence Guidelines

CXTracer does not use a database. The app is a read-only viewer over Codex CLI transcript JSONL files.

## Source of Truth

- Transcript files live outside the repository, usually under `%USERPROFILE%\.codex\sessions`.
- Users may also point the app at WSL or UNC session roots.
- The app must treat those files as externally owned and actively written by Codex CLI.

## Read-Only Contract

Allowed operations:

- `Directory.EnumerateFiles(rootPath, "*.jsonl", SearchOption.AllDirectories)`
- `FileInfo` metadata reads
- `FileSystemWatcher` for change notifications
- `FileStream` opened with `FileAccess.Read`

Required sharing mode for transcript reads:

```csharp
new FileStream(
    filePath,
    FileMode.Open,
    FileAccess.Read,
    FileShare.ReadWrite | FileShare.Delete,
    bufferSize: 64 * 1024,
    options: FileOptions.SequentialScan);
```

Current examples:

- `SessionScanner.ScanLight` enumerates session files and returns metadata-only `SessionInfo` rows without reading transcript content.
- `SessionScanner.TryGetSessionSummary` enriches a single selected/newest session by sampling transcript head lines and estimating line count.
- `SessionScanner.ReadHeadLines` samples the first lines of one transcript.
- `SessionScanner.EstimateLineCount` counts newline bytes for one selected/newest transcript up to a cap.
- `SessionReader.OpenReadShared` centralizes shared read access for full reads and tail reads.

## No Local Persistence Yet

- Do not add EF Core, SQLite, LiteDB, JSON cache files, or search indexes for routine features.
- Do not persist derived transcript data unless a PRD explicitly asks for local storage.
- Prefer lightweight metadata scans for session lists. Read transcript content only for the newest startup session or for a user-selected session.

## Tail State

`SessionReader` keeps in-memory tail state per file path:

- byte offset
- pending partial UTF-8 line
- last parsed line number

This state is process-local and should not be serialized.

### Tail Read Contract

`SessionReader.ReadAllAsync(filePath, cancellationToken)` and
`SessionReader.ReadAppendedAsync(filePath, cancellationToken)` must serialize
work per file path with a per-path `SemaphoreSlim`. This prevents a full reload
and an append read from racing against the same `_tailStates[filePath]` entry.

When `ReadAllAsync` finishes, record `stream.Position` as the next tail offset.
Do not use `new FileInfo(filePath).Length` for this state update: an active
session file may grow after the stream has reached its current end, and using
the later file length would skip bytes that were appended but never parsed.

`ReadAppendedAsync` must snapshot `FileInfo.Length` before allocating the read
buffer, read only up to the bytes actually returned by the stream, then set the
next offset to `state.Offset + totalRead`.

Validation and error behavior:

- missing file -> return an empty event list
- file length below stored offset -> treat as truncation/rotation and restart from offset `0`
- no new bytes -> return an empty event list
- partial final line -> keep it in `TailState.Pending` until a later newline
- appended byte count larger than `int.MaxValue` -> restart from offset `0` rather than allocating an oversized buffer
- cancellation -> throw `OperationCanceledException` to the caller

Good/base/bad cases:

- Good: full read records the actual stream offset, then appended reads continue from that exact byte.
- Base: repeated watcher notifications for the same unchanged file return no events.
- Bad: recording `FileInfo.Length` after a full read can skip bytes appended during the read window.

## Avoid

- Writing markers, locks, indexes, or metadata beside Codex session files.
- Opening transcript files without `FileShare.ReadWrite | FileShare.Delete`.
- Treating Codex JSONL as a stable public API; parser logic must stay tolerant.
