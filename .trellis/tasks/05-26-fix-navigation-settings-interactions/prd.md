# Fix navigation settings interactions

## Goal

Fix SettingsWindow sync navigation toggle and shortcut capture interaction regressions.

## Requirements

- Settings window `Synchronized navigation` checkbox must toggle and stay checked/unchecked normally.
- The checkbox must update the same persisted sync-navigation setting used by main-window keyboard behavior.
- Shortcut capture must not finish when the user presses only `Ctrl`, `Shift`, or `Alt`.
- Shortcut capture must remain active while modifier-only keys are pressed.
- Shortcut capture must record only a valid `Ctrl`, `Shift`, and/or `Alt` plus one supported non-modifier key.
- Supported shortcut keys must include letters, digits, and common punctuation keys such as `Ctrl+Shift+'`.
- Invalid non-modifier input during capture must not overwrite the existing shortcut.
- The fix must preserve settings persistence across restarts.
- With synchronized navigation enabled, mouse up/down buttons and keyboard up/down must advance through the same message sequence.
- Pressing left/right must visibly mark the active `Conversation` or `Execution` pane with a thicker outer border.
- Up/down navigation from either keyboard or mouse buttons must visibly mark the current message with a thicker outer border.
- After the fix, run break-loop analysis and update specs so future shortcut-capture UI handles modifier-only keydown events correctly.

## Acceptance Criteria

- [ ] Opening Settings and clicking `Synchronized navigation` toggles the checkbox visibly.
- [ ] Closing and reopening Settings shows the current sync-navigation state.
- [ ] Capturing a shortcut and pressing only `Ctrl`, `Shift`, or `Alt` keeps capture mode active.
- [ ] Capturing a shortcut and pressing `Ctrl+S`, `Shift+S`, `Alt+S`, or another modifier-plus-key displays the captured shortcut.
- [ ] Capturing a shortcut and pressing `Ctrl+Shift+'` displays `Ctrl+Shift+'`.
- [ ] Clicking `OK` persists the captured shortcut.
- [ ] Invalid capture input does not replace the previously saved shortcut.
- [ ] With sync navigation enabled, repeated mouse down-button clicks continue advancing messages just like repeated Down key presses.
- [ ] Pressing Left/Right visibly moves the active-pane outer border between Conversation and Execution.
- [ ] Navigating with Up/Down or pane buttons visibly highlights the current message card.
- [ ] Build passes.

## Notes

- Keep `prd.md` focused on requirements, constraints, and acceptance criteria.
- Lightweight tasks can remain PRD-only.
- For complex tasks, add `design.md` for technical design and `implement.md` for execution planning before `task.py start`.

## Confirmed Context

- `SettingsWindowViewModel` currently proxies settings from `MainWindowViewModel`.
- `SettingsWindow.axaml` binds the checkbox to `IsSynchronizedNavigationEnabled`.
- `SettingsWindow.axaml.cs` previously ended capture on any non-letter key, including standalone modifier keydown events and valid punctuation shortcut keys.
