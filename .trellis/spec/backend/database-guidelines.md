# Data and Persistence Guidelines

Codex Lens does not use a database. The app is a read-only viewer over Codex CLI transcript JSONL files.

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

- `SessionScanner.ReadHeadLines` samples the first lines of a transcript.
- `SessionScanner.EstimateLineCount` counts newline bytes up to a cap.
- `SessionReader.OpenReadShared` centralizes shared read access for full reads and tail reads.

## No Local Persistence Yet

- Do not add EF Core, SQLite, LiteDB, JSON cache files, or search indexes for routine features.
- Do not persist derived transcript data unless a PRD explicitly asks for local storage.
- Prefer recomputing summaries from transcript files; the current max scan is capped at 300 files.

## Tail State

`SessionReader` keeps in-memory tail state per file path:

- byte offset
- pending partial UTF-8 line
- last parsed line number

This state is process-local and should not be serialized.

## Avoid

- Writing markers, locks, indexes, or metadata beside Codex session files.
- Opening transcript files without `FileShare.ReadWrite | FileShare.Delete`.
- Treating Codex JSONL as a stable public API; parser logic must stay tolerant.
