# Verification Matrix

| Requirement / Acceptance Criteria | Verification Type | Evidence | Status |
|---|---|---|---|
| Audit covers the main `src/` structure before selecting findings | Task research | `research/code-health-audit.md` records the original `src/` audit and findings | pass |
| Final audit report includes a PASS / WATCH / FAIL verdict | Task research | `research/code-health-audit.md` verdict is `WATCH` | pass |
| Each finding includes evidence, maintenance risk, small fix, grill question, and recommended answer | Task research | `research/code-health-audit.md` includes those sections for all 3 findings | pass |
| Findings are written to the task research directory | Task research | `research/code-health-audit.md` exists under the active task | pass |
| Transcript shared-open file access has one service-level owner used by scanning and reading | Static check + build | `rg "new FileStream\\(|FileShare.ReadWrite|OpenReadShared" src/CodexLens` shows `FileStream` and share flags only in `SessionFileAccess`; `SessionReader` and `SessionScanner` call `SessionFileAccess.OpenReadShared(...)`; `dotnet build .\\CodexLens.sln --configuration Release --no-restore` passed | pass |
| Shortcut capture behavior is shared between `MainWindow` and `SettingsWindow` without moving Avalonia key/event details into ViewModels | Static check + build | `ShortcutCaptureInput.TryHandleCapture(...)` is called by both window code-behind files; `ShortcutCaptureInput` and `ShortcutKeyInput` stay under `Views`; ViewModels receive only modifier booleans and normalized key text; Release build passed | pass |
| Unused eager session scan API is removed or made explicit | Static check + build | `rg "public IReadOnlyList<SessionInfo> Scan\\(" src/CodexLens` returns no matches; Release build passed | pass |
| Focused validation proves changed service and UI code compile | Build | `dotnet build .\\CodexLens.sln --configuration Release --no-restore` passed with 0 warnings / 0 errors after elevated rerun | pass |
| Touched source files are format-clean | Format check | `dotnet format .\\CodexLens.sln --verify-no-changes --no-restore --include <touched source files>` passed | pass |

## Notes

- The first build attempt failed because the sandbox blocked Avalonia telemetry output under `C:\Users\M3\AppData\Local\AvaloniaUI\BuildServices\buildtasks.log`; the same command passed after approved elevation.
- Full-repo `dotnet format --verify-no-changes --no-restore` still reports pre-existing whitespace issues in `src/CodexLens/Models/DisplayEvent.cs`, which was not touched by this task.
- No test project exists in `CodexLens.sln`, so no automated unit test command was available.
