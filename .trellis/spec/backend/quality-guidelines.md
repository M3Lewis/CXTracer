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
- User, assistant, final, command, output, diff, tool, and error classification remain covered.
- New parser heuristics are based on keys/types first; avoid overmatching arbitrary free text.

## Async and UI Responsiveness

- Full transcript loads remain async and cancellable.
- File watcher callbacks marshal to `Dispatcher.UIThread` before mutating observable collections.
- Long scans stay off the UI thread with `Task.Run` or a better async design.

## Resource Management

- Streams and readers are disposed with `using` / `await using`.
- `SessionWatcher.Stop()` detaches event handlers before disposing.
- `MainWindowViewModel.Dispose()` cancels load state and disposes the watcher.

## Verification

- Run `dotnet build .\CodexLens.sln` after service or ViewModel changes.
- For parser changes, exercise `samples/sample-rollout.jsonl` or a real copied transcript.
- For watcher/tail changes, verify active append behavior with a file that is still writable.
