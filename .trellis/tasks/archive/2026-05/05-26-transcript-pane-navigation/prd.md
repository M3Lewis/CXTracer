# Improve transcript pane navigation

## Goal

Improve execution text wrapping, keyboard navigation between panes, focus indication, and optional synchronized navigation across conversation and execution panes.

## Requirements

- Execution pane event cards must wrap long command/output text to the available pane width.
- Execution pane timestamps must remain visible without dragging a horizontal scrollbar.
- Left and right arrow keys must switch the active transcript pane between Conversation and Execution.
- The active pane must have a clear orange focus border that is about three times thicker than the default pane border.
- Up and down arrow keys must invoke the same previous/next navigation behavior as the pane's up/down UI buttons.
- Clicking a pane's up/down UI buttons must participate in the same navigation model as keyboard up/down.
- A main-toolbar Settings button must open a dedicated settings window.
- The settings window must expose an option to enable or disable synchronized navigation between Conversation and Execution.
- The settings window must allow assigning a shortcut for toggling synchronized navigation.
- The sync-navigation enabled state and confirmed shortcut must persist across app restarts.
- Shortcut assignment must support `Ctrl`, `Shift`, and/or `Alt` plus one letter key.
- Shortcut assignment must enter a capture state after the user clicks the shortcut editor, record the next valid shortcut, display it, and commit it through an explicit confirmation action.
- Invalid shortcut input must leave the existing shortcut unchanged and give inline/footer feedback.
- The sync-navigation shortcut must toggle the same setting exposed by the settings-panel option.
- When synchronized navigation is disabled, up/down navigation is pane-local.
- When synchronized navigation is enabled, up/down navigation uses a single global chronological cursor over visible Conversation and Execution events.
- Synchronization must handle multiple execution events between two conversation messages.
- Synchronization must use transcript event ordering data already available on `DisplayEvent` (`Timestamp` when present, with `LineNumber` as a stable fallback/tie-breaker).
- Navigation and synchronization state belongs in `MainWindowViewModel`; rendered-control scrolling and visual-tree coordinate work stays in `MainWindow.axaml.cs`.
- Startup and selected-session loading must remain responsive; this task must not reintroduce eager loading of old sessions.

## Acceptance Criteria

- [ ] Long Execution text wraps inside the pane at normal and narrow window widths.
- [ ] Execution event timestamps remain visible without horizontal scrolling.
- [ ] Left/right keyboard input switches active pane between Conversation and Execution.
- [ ] The active pane is visually indicated by a thicker orange border.
- [ ] Up/down keyboard input moves repeatedly through events in the active pane when sync is off.
- [ ] Existing Conversation and Execution up/down buttons still move repeatedly across events.
- [ ] The main toolbar exposes a Settings button instead of inline sync-navigation controls.
- [ ] The settings window exposes a sync-navigation toggle.
- [ ] The settings window exposes a shortcut editor for the sync-navigation toggle.
- [ ] The sync-navigation enabled state and confirmed shortcut reload after closing and reopening the app.
- [ ] Clicking the shortcut editor waits for a valid modifier-plus-letter shortcut and then shows the captured shortcut.
- [ ] Confirming the shortcut makes that shortcut toggle synchronized navigation.
- [ ] Invalid shortcut input does not overwrite the previous shortcut.
- [ ] With sync off, Conversation and Execution up/down controls do not force the other pane to move.
- [ ] With sync on, up/down moves through a combined chronological stream of visible Conversation and Execution events.
- [ ] Multiple Execution events that fall between two Conversation messages are reachable without being skipped by sync navigation.
- [ ] `dotnet build .\CodexLens.sln` passes.

## Notes

- Keep `prd.md` focused on requirements, constraints, and acceptance criteria.
- Lightweight tasks can remain PRD-only.
- For complex tasks, add `design.md` for technical design and `implement.md` for execution planning before `task.py start`.

## Confirmed Context

- `MainWindow.axaml` currently uses two transcript panes backed by `ConversationEvents` and `ExecutionEvents`.
- Existing up/down buttons are implemented in `MainWindow.axaml.cs` because scrolling requires `ItemsControl`, `ScrollViewer`, visual descendants, and rendered coordinates.
- Existing frontend spec allows view-only scroll mechanics in code-behind, but application/navigation state should remain in `MainWindowViewModel`.
- `DisplayEvent` already carries `Pane`, `Timestamp`, `LineNumber`, `TimeText`, `Title`, and `Text`, which are enough to define chronological navigation without changing parser output.
- The Execution `ScrollViewer` currently enables horizontal scrolling, which can prevent text wrapping from constraining to the visible pane width.
- The current app uses inline footer status instead of routine modal dialogs/toasts.
- There is no existing settings page or shortcut-binding model; this task should add a dedicated settings window for navigation behavior.
- There is no existing app settings storage service, so persistence requires adding a small JSON settings service.

## Open Questions

- None.
