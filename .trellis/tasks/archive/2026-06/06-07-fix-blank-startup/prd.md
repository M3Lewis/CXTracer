# Fix blank conversation on startup

## Goal

Fix the bug where the conversation and execution panels remain completely blank on application startup, forcing the user to manually click a session to display its content.

## Confirmed Facts

- On application startup, `MainWindowViewModel`'s constructor calls `RefreshAsync()`.
- `RefreshAsync()` calls `PopulateSessionsFilteredAsync(CancellationToken.None)`.
- During `PopulateSessionsFilteredAsync`, the view model sets `_selectionChanging = true` to guard against list clearance triggers, and then sets `SelectedSession = Sessions.FirstOrDefault()` (which is the first loaded session).
- Because `_selectionChanging` is `true` during property changes, the property-change callback `OnSelectedSessionChanged` returns early, and `LoadSelectedSessionAsync(value)` is never executed.
- When `PopulateSessionsFilteredAsync` completes, it sets `_selectionChanging = false`.
- Back in `RefreshAsync()`, the startup select-first-item block (`if (SelectedSession is null && Sessions.Count > 0)`) checks if `SelectedSession` is `null`. But since `PopulateSessionsFilteredAsync` already set it to `Sessions.FirstOrDefault()`, `SelectedSession` is not `null` and this block is skipped entirely.
- As a result, the first session is highlighted/selected in the UI list, but its underlying data events are never loaded.

## Requirements

1. **Auto-Load Selected Session on Selection Change**:
   - At the end of `PopulateSessionsFilteredAsync`, after resetting `_selectionChanging = false`, if the final `SelectedSession` is not `null` and differs from the previous selection (e.g. at startup when it changes from `null` to the first session, or when filters change), trigger `LoadSelectedSessionAsync(SelectedSession)`.
2. **Eliminate Redundant Selection Logic**:
   - Remove the duplicate selection logic in `RefreshAsync()`.
3. **No Redundant Loading**:
   - Ensure that if the selection path has not changed (e.g., when refreshing or typing filters that don't change the selected item), the session is not re-loaded.

## Acceptance Criteria

- [ ] On initial startup, the first session is automatically selected AND its conversation and execution events are fully loaded and displayed.
- [ ] Typing search filters that keep the currently selected session visible does not trigger redundant file reloads.
- [ ] Typing search filters that filter out the currently selected session correctly selects the first session of the new filtered list and loads its events.
- [ ] Clearing search text restores the full list and, if the selected session changes, loads its events.
- [ ] No regression in SukiUI styles, window themes, or memory cleanup.
