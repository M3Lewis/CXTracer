# Fix settings window and checkbox UI regressions

## Goal

Fix the settings-related UI regressions as one coherent, reviewable task:

- SettingsWindow first-open layout/status display should be stable and should not show unrelated long main-window session status.
- SettingsWindow buttons should keep visible borders on hover, matching the previously fixed main-window toolbar button behavior.
- Checkbox controls used for app preferences should visibly toggle into checked and unchecked states in both SettingsWindow and MainWindow.

## User Value

The settings and toolbar controls should behave predictably on first use. Users should be able to see button boundaries while interacting, and clicking preference checkboxes should visibly reflect the actual state without requiring trust in hidden persistence.

## Confirmed Facts

- `src/CodexLens/Views/MainWindow.axaml` defines scoped `Button.toolbarButton` and `ToggleButton.toolbarButton` styles with template-owned border fixes for hover/pressed/checked states.
- `src/CodexLens/Views/SettingsWindow.axaml` currently uses plain `Button` controls for shortcut capture, OK, and Close, so it does not receive the main-window hover-border fix.
- `.trellis/spec/frontend/atoms/toolbar-button-visual-state-metrics.md` records the prior hover-border regression and requires button visual-state fixes to preserve borders and metrics without touching unrelated controls.
- `.trellis/spec/frontend/atoms/settings-checkbox-three-state.md` requires persistent settings checkboxes to be explicitly two-state and visibly toggle checked/unchecked.
- `SettingsWindow` synchronization checkbox is bound through `SettingsWindowViewModel.IsSynchronizedNavigationEnabled` and already uses a local proxy field pattern to avoid synchronous binding reentrancy.
- `MainWindow` `Pin selected` checkbox is bound to `MainWindowViewModel.PinSelectedSession`, a generated non-nullable bool property.
- A partial fix for SettingsWindow first-open layout/status display has already been made in the working tree:
  - `SettingsWindowViewModel` owns a settings-local status message instead of directly proxying `MainWindowViewModel.StatusMessage`.
  - `SettingsWindow.axaml` uses content-measured height, all-auto rows, and status text trimming.

## Requirements

1. Preserve the existing SettingsWindow first-open fix.
   - Opening SettingsWindow while the main window status is a long `Viewing <session>.jsonl` message must not display that long session status in the settings footer.
   - The settings footer may show a concise settings-local message such as `Settings ready.` or setting-specific feedback.
   - The shortcut capture, OK, and Close rows must remain visible and evenly spaced on first open.

2. Unify SettingsWindow button hover behavior with main-window toolbar text buttons.
   - SettingsWindow shortcut capture, OK, and Close buttons must keep a visible border during pointer hover.
   - Hover/pressed states must not change button size or cause layout shift.
   - The fix should reuse or mirror the proven `toolbarButton` behavior rather than inventing unrelated metrics.
   - Scope must not accidentally change transcript pane arrow buttons, session list item visuals, or unrelated card borders.

3. Fix preference checkbox visual toggling.
   - SettingsWindow `Synchronized navigation` checkbox must visibly toggle between checked and unchecked when clicked.
   - MainWindow `Pin selected` checkbox must visibly toggle between checked and unchecked when clicked.
   - Neither checkbox may cycle through a null/indeterminate visual state.
   - The underlying ViewModel state must match the visible checkbox state after each click.
   - Existing persistence behavior for synchronized navigation must remain intact.

4. Preserve existing shortcut behavior.
   - Shortcut capture must still accept modifier + non-modifier key combinations according to the existing shortcut capture contract.
   - Confirming a shortcut must still persist it to app settings.

## Acceptance Criteria

- [ ] First opening SettingsWindow after a session is loaded shows a stable settings layout, not a footer dominated by `Viewing <long jsonl file>`.
- [ ] SettingsWindow shortcut capture button, OK button, and Close button retain visible borders on hover.
- [ ] Hovering SettingsWindow buttons does not change their measured size or shift nearby text/buttons.
- [ ] MainWindow `Default` and `Refresh` button hover behavior remains unchanged from the previous fix.
- [ ] Clicking SettingsWindow `Synchronized navigation` visibly checks it; clicking again visibly unchecks it.
- [ ] Closing and reopening SettingsWindow preserves the synchronized navigation checkbox value.
- [ ] Clicking MainWindow `Pin selected` visibly checks it; clicking again visibly unchecks it.
- [ ] No preference checkbox enters an indeterminate/null visual state during normal clicks.
- [ ] Shortcut capture still works for valid modifier shortcuts, including punctuation keys covered by the existing shortcut contract.
- [ ] Build or equivalent compile validation is run. If local NuGet configuration blocks build, record the exact blocker and run narrower static checks available in the environment.

## Out of Scope

- Redesigning the full settings window visual language.
- Changing where app settings are saved.
- Changing session loading, session pinning semantics, or synchronized navigation behavior beyond making the existing controls visibly reflect state.
- Adding new settings categories or moving controls between MainWindow and SettingsWindow.
- Fixing global PowerShell profile noise or machine-level NuGet configuration, except documenting it if it blocks validation.

## Relevant Files

- `src/CodexLens/Views/SettingsWindow.axaml`
- `src/CodexLens/Views/SettingsWindow.axaml.cs`
- `src/CodexLens/ViewModels/SettingsWindowViewModel.cs`
- `src/CodexLens/Views/MainWindow.axaml`
- `src/CodexLens/ViewModels/MainWindowViewModel.cs`
- `.trellis/spec/frontend/atoms/toolbar-button-visual-state-metrics.md`
- `.trellis/spec/frontend/atoms/settings-checkbox-three-state.md`

## Open Questions

None currently blocking planning. The reported behavior maps directly to existing controls and prior specs.
