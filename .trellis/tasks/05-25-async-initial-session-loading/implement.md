# Implementation Plan

## Checklist

- [x] Add lightweight session discovery to `SessionScanner`.
- [x] Add one-file enrichment to `SessionScanner` for newest/selected sessions.
- [x] Ensure `SessionInfo` raises computed-property notifications when summary fields change.
- [x] Update `RefreshAsync()` so startup lists sessions from lightweight metadata without reading every transcript.
- [x] Keep automatic startup load limited to the newest session.
- [x] Update `LoadSelectedSessionAsync()` so event collection population is cancellable and batched.
- [x] Update watcher path handling so only the selected file reads appended transcript content.
- [x] Update specs if new conventions are introduced.
- [x] Run `dotnet build .\CodexLens.sln`.
- [x] Verify `git diff --check`.

## Validation

Required:

```powershell
dotnet build .\CodexLens.sln
git -c safe.directory=I:/CodexLens diff --check
```

Manual checks:

- Launch app with many session files.
- Confirm window paints and accepts input while scan/load is in progress.
- Confirm newest session loads automatically and progressively.
- Confirm older session summaries are not enriched until selection.
- Click a large older session and confirm UI stays responsive while cards appear.
- Select another session during load and confirm previous load is canceled without mixed cards.

## Rollback Points

- If lightweight discovery causes unacceptable session titles, keep lazy loading but add a selected-session summary refresh.
- If automatic newest load still feels too heavy, disable auto-load and leave only the list populated.
- If chunking introduces visual glitches, reduce batch size before changing the data model.
