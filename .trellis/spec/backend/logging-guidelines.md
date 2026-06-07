# Logging Guidelines

CXTracer currently has no application logging pipeline. `Program.BuildAvaloniaApp()` uses Avalonia's `LogToTrace()` for framework diagnostics.

## Current Policy

- User-facing operation failures are reported through `MainWindowViewModel.StatusMessage`.
- Malformed transcript lines are preserved as raw display events by `CodexEventParser`.
- No transcript contents are written to logs.

## When Adding Logging

If a feature needs durable diagnostics, prefer `ILogger<T>` and keep logging behind service boundaries.

Useful log candidates:

- scan start/end with root path and file count
- watcher start/stop and watcher errors
- transcript read failures with file path and exception type
- parser classification failures without dumping full transcript content by default

## Sensitive Data

Codex transcript files can contain prompts, commands, paths, and tool outputs. Do not log full raw JSONL lines, command output, diffs, or assistant responses unless a diagnostic feature explicitly asks the user to export them.

## Avoid

- Adding Serilog or another logging stack just for routine UI status messages.
- Logging the same exception in both a service and the ViewModel unless each adds distinct context.
- Writing logs under `%USERPROFILE%\.codex` or beside transcript files.
