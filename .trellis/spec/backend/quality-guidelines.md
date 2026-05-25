# Service Quality Checklist

Use this checklist when changing services, parser behavior, or transcript IO.

## Read-Only Safety

- No code writes to, deletes from, or creates files under the selected sessions root.
- Transcript files are opened with `FileAccess.Read`.
- Active files are opened with `FileShare.ReadWrite | FileShare.Delete`.
- Generated caches or indexes are not introduced without explicit product scope.

## Parser Behavior

- Unknown JSONL shapes fall back to raw events instead of crashing.
- Reasoning and plan events stay out of the conversation pane unless raw is enabled.
- Internal `response_item` events stay out of the conversation pane even when they carry `role=user` or `role=assistant`; the outer message/final event is the user-visible chat event.
- User, assistant, final, command, output, diff, tool, and error classification remain covered.
- New parser heuristics are based on keys/types first; avoid overmatching arbitrary free text.

## Async and UI Responsiveness

- Full transcript loads remain async and cancellable.
- File watcher callbacks marshal to `Dispatcher.UIThread` before mutating observable collections.
- Long scans stay off the UI thread with `Task.Run` or a better async design.
- `SessionReader` serializes `ReadAllAsync` and `ReadAppendedAsync` per file path so `_tailStates` cannot race.
- Tail offsets advance from `stream.Position` / actual bytes read, not from a later `FileInfo.Length` observation.

## Resource Management

- Streams and readers are disposed with `using` / `await using`.
- `SessionWatcher.Stop()` detaches event handlers before disposing.
- `MainWindowViewModel.Dispose()` cancels load state and disposes the watcher.
- `MainWindowViewModel.Dispose()` also cancels, disposes, and clears any watcher debounce token sources.
- If per-path semaphores are added or replaced, document their lifetime; they are currently app-session scoped with the reader.

## Verification

- Run `dotnet build .\CodexLens.sln` after service or ViewModel changes.
- For parser changes, exercise `samples/sample-rollout.jsonl` or a real copied transcript and check that user/assistant messages do not appear twice.
- For watcher/tail changes, verify duplicate rapid change events, active appends, partial final lines, and truncation/rotation behavior.
