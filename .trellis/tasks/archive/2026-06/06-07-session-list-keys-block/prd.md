# Session List Key Navigation Separation

## Goal

Prevent the session ListBox and its items from capturing keyboard focus and handling Up/Down arrow keys. Ensure that clicking a session item does not lock keyboard focus in the session list, allowing window-level arrow key navigation to control the active transcript pane immediately.

## User Value

Currently, clicking a session item shifts keyboard focus to the session list. If the user immediately presses the Up/Down arrow keys, the application switches the active session instead of navigating the transcript events. This causes accidental session switches and disrupts navigation.

## Confirmed Facts

- **Global Key Navigation**: `Window_KeyDown` in `MainWindow.axaml.cs` handles arrow keys globally to steer the active transcript pane and events.
- **Transcript List Focus**: `ListBox.transcriptList` has `Focusable="False"` to prevent items from stealing key focus, keeping listeners at the window level.
- **Session List Focus**: The session `ListBox` currently does not have `Focusable="False"`, nor do its `ListBoxItem`s. Clicking them captures focus.
- **Mouse Selection**: Pointer clicks still trigger item selection in Avalonia ListBoxes even when `Focusable="False"` is applied.

## Requirements

### REQ-1: Disable keyboard focus on Session List

- **AC-1.1**: The session `ListBox` control in `MainWindow.axaml` must have `Focusable="False"`.
- **AC-1.2**: The `ListBoxItem` style selector for `ListBox.sessionList` must set `Focusable` to `False`.

### REQ-2: Ensure global transcript navigation persists

- **AC-2.1**: Clicking a session card must change the selected session normally.
- **AC-2.2**: Immediately after clicking a session card, pressing Up/Down arrow keys must navigate the active transcript events (conversation/execution) without changing the selected session.
