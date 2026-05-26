# Transcript Pane Navigation Design

## Scope

This task changes the main transcript workspace only:

- `MainWindow.axaml` for wrapping, active-pane border styling, and the Settings button.
- `SettingsWindow.axaml` for sync-navigation and shortcut settings.
- `MainWindow.axaml.cs` for rendered-control scrolling, key handling, shortcut capture, and pane focus routing.
- `MainWindowViewModel.cs` for navigation state, synchronized-navigation state, shortcut text, and chronological event selection.
- `SettingsWindowViewModel.cs` for a focused settings-window binding surface that proxies shared navigation settings from `MainWindowViewModel`.
- `Services/AppSettingsService.cs` and a small settings model for persisted navigation preferences.
- Existing `DisplayEvent` data is reused; parser and reader output should not change.

## Layout

Execution wrapping should be fixed by removing the event stream's horizontal layout pressure. The Execution `ScrollViewer` should not allow horizontal scrolling for normal event cards, and item content should be constrained to the viewport width so `TextWrapping="Wrap"` has a finite width.

Raw events can keep horizontal scrolling because raw JSON inspection benefits from preserving long lines.

The active transcript pane should be represented by ViewModel state, not by Avalonia keyboard focus alone. Conversation and Execution pane borders bind their brush/thickness to that state. The selected border uses the existing orange theme direction and is about three times the default pane border thickness.

## Navigation Model

The ViewModel owns:

- active pane: Conversation or Execution.
- sync-navigation enabled: default `false`.
- optional pending/confirmed shortcut display.
- current global navigation event id or current event line number.

Sync-navigation enabled state is loaded from persisted settings during ViewModel construction. If settings are missing or unreadable, defaults are used and the footer status can report the problem without blocking transcript browsing.

When sync-navigation is disabled:

- Left/right switches active pane only.
- Up/down and the pane UI buttons move within the active or clicked pane.
- The other pane is not forced to move.

When sync-navigation is enabled:

- Conversation and Execution visible events are merged into one chronological list.
- Ordering key is `Timestamp` when present, then `LineNumber` as a tie-breaker and fallback.
- Up/down and pane UI buttons move to the previous/next item in that merged list.
- If the target item belongs to the other pane, active pane switches to that pane.
- The target pane scrolls to the target event. The non-target pane should also be aligned to the nearest visible event at or before the target ordering key when such an event exists, so both panes stay in the same chronological region.

## View And Code-Behind Boundary

`MainWindow.axaml.cs` remains responsible for:

- receiving key events from the window.
- capturing a shortcut after the user enters shortcut capture mode.
- translating key gestures into ViewModel actions.
- scrolling an event item into view by finding the rendered `ContentPresenter`.

`MainWindowViewModel` remains responsible for:

- deciding the next target event.
- toggling sync-navigation.
- validating accepted shortcut shapes as model state.
- exposing bindable text for shortcut and status.
- calling the settings service when confirmed preferences change.

The ViewModel must not reference `ScrollViewer`, `ItemsControl`, or visual-tree APIs.

## Shortcut Capture

The main transcript toolbar exposes only a Settings button. The dedicated settings window exposes:

- a sync-navigation checkbox/toggle.
- a shortcut editor button showing the current shortcut or an empty/unset label.
- a confirm action for the captured shortcut.

Clicking the shortcut editor puts the UI into capture mode. The next valid `Ctrl`, `Shift`, and/or `Alt` plus letter key becomes the pending shortcut text. Confirmation promotes the pending shortcut to the active toggle shortcut. Invalid input does not overwrite the existing shortcut and should update footer status.

The runtime key handler compares incoming key events against the confirmed shortcut and toggles sync-navigation when matched. Text entry controls should retain normal typing behavior; navigation shortcuts should not trigger while a text input is actively editing text unless the captured shortcut explicitly uses modifiers and matches outside text editing.

`SettingsWindowViewModel` should not duplicate persisted state. It relays the shared navigation settings owned by `MainWindowViewModel`, so changes made in the settings window immediately affect main-window keyboard behavior and persistence.

## Persistence

Add a small app settings service under `Services/`:

- Settings file location: a Codex Lens-specific directory under the user's local application data folder.
- File format: JSON.
- Initial settings: sync navigation disabled, no shortcut assigned.
- Persisted fields: sync-navigation enabled state and confirmed shortcut components/text.
- Save timing: after toggling sync-navigation through the setting/shortcut and after confirming a new shortcut.
- Failure handling: do not crash the app; keep current in-memory state and report a concise footer status.

The settings service owns filesystem paths and serialization. The ViewModel owns when preferences change. Views do not read or write settings directly.

## Trade-Offs

The global chronological cursor is more predictable than trying to infer a hidden relationship between one Conversation message and many Execution events. It also makes the user's proposed "walk through execution events until the next conversation message" behavior natural.

Persisting settings matches the user's expectation for an explicit settings panel, but it adds a new app-settings persistence path. Keep the service narrow to avoid creating a broad configuration framework before the app needs one.
