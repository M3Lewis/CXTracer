# Implementation Plan

## Checklist

- [x] Update `MainWindowViewModel` with active-pane state, sync-navigation state, shortcut capture state, and helper methods for pane-local/global navigation targets.
- [x] Add a small persisted settings model/service for sync-navigation enabled state and confirmed shortcut.
- [x] Update `MainWindow.axaml` to add active-pane border styling, finite-width Execution wrapping, and a Settings button.
- [x] Add `SettingsWindow` and `SettingsWindowViewModel` for sync-navigation and shortcut settings.
- [x] Update `MainWindow.axaml.cs` to route left/right/up/down keys, pane button clicks, shortcut capture, and scroll-to-event behavior.
- [x] Ensure pane UI button clicks set the active pane before navigation.
- [x] Ensure sync-navigation off preserves existing pane-local repeated up/down behavior.
- [x] Ensure sync-navigation on walks visible Conversation and Execution events in chronological order without skipping multiple Execution entries.
- [x] Ensure sync-navigation enabled state and confirmed shortcut are saved and restored across app restarts.
- [x] Build with `dotnet build .\CodexLens.sln`.
- [ ] Manually verify normal and narrow window widths for Execution wrapping and timestamp visibility.
- [ ] Manually verify shortcut capture, invalid shortcut rejection, and shortcut toggle behavior.

## Validation

- `dotnet build .\CodexLens.sln` was attempted, but the running `CodexLens` process locked `bin\Debug\net8.0\CodexLens.exe` and `.dll`.
- `dotnet build .\src\CodexLens\CodexLens.csproj -o .\artifacts\build-check-project` passed with 0 warnings and 0 errors after moving settings into `SettingsWindow`.
- `git diff --check` passed.
- Run the app with a transcript containing both Conversation and Execution events.
- Verify repeated mouse clicks on all four pane arrow buttons.
- Verify keyboard left/right/up/down when focus is on the transcript workspace.
- Verify sync-navigation off and on.
- Verify Execution long text wraps and there is no need for horizontal scrolling to see timestamps.
- Restart the app and verify the sync-navigation enabled state and shortcut are restored.

## Risk Points

- Key handling can interfere with text entry in `TextBox` controls; guard navigation key handling when text inputs are focused.
- Disabling horizontal scrolling can expose layout bugs if item width is not constrained correctly.
- Progressive event loading means navigation lists may be incomplete while loading; methods should operate on currently visible collections and continue working as more events arrive.
- Settings persistence should stay narrow; avoid adding a broad configuration framework.
