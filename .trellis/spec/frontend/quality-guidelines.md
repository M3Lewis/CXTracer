# Frontend Quality Checklist

Use this checklist for Avalonia, SukiUI, ViewModel, and AXAML changes.

## Shell and Theme

- `App.axaml` still registers `SukiTheme` with explicit `ThemeColor`.
- `MainWindow.axaml` remains a `suki:SukiWindow`.
- `MainWindow.axaml.cs` derives from `SukiWindow`.
- New dialog/toast hosts, if any, are under `SukiWindow.Hosts`.

## Binding and Types

- Bound views and data templates have `x:DataType`.
- New ViewModel state uses `[ObservableProperty]` where appropriate.
- New commands use `[RelayCommand]`.
- Nullable warnings are addressed rather than suppressed.

## MVVM Boundaries

- IO and parsing stay in `Services/`.
- ViewModels do not reference visual tree controls.
- Code-behind remains view-only.
- Services do not depend on Avalonia/SukiUI controls.

## UI Behavior

- Search/filter changes update visible collections predictably.
- Raw events remain hidden unless `ShowRawEvents` or the Raw filter is active.
- Conversation pane contains only user, assistant, and final events.
- Internal `response_item` records do not produce duplicate user or assistant cards in Conversation.
- Execution pane contains command, output, diff, tool, and error events.
- Startup lists sessions without blocking on transcript summaries for every historical session.
- Large selected transcripts populate Conversation/Execution/Raw collections progressively instead of one UI-thread burst.
- Conversation and Execution arrow buttons continue moving across repeated clicks, not only the first click.
- With synchronized navigation enabled, pane arrow buttons and keyboard Up/Down continue through the same current-message state.
- Left/Right visibly changes the active Conversation/Execution pane border.
- Up/Down navigation visibly highlights the current message card.
- Rapid duplicate watcher events for the same path are debounced before UI collections are updated.
- Debounced live updates still respect `PinSelectedSession`: they update the selected file, but do not auto-switch when pinning is enabled.
- Small status badges such as `只读` and `History` stay close to text height and do not stretch to fill multiline rows.

## Verification

- Run `dotnet build .\CodexLens.sln` after UI changes.
- For layout changes, run the app and load `samples/sample-rollout.jsonl` or a copied real transcript.
- For startup/load changes, verify the window remains responsive while sessions and selected transcript events are still filling in.
- For scroll-button changes, verify repeated up/down clicks in both Conversation and Execution panes, including rapid repeated clicks before a render frame.
- For synchronized-navigation changes, run the checks in [Navigation Shared State](./atoms/navigation-shared-state.md).
- For settings-window checkbox changes, run the checks in [Settings Checkbox Three-State](./atoms/settings-checkbox-three-state.md) and [Proxy ViewModel Reentrancy](./atoms/proxy-viewmodel-reentrancy.md).
- For shortcut capture changes, run the checks in [Shortcut Capture Contract](./atoms/shortcut-capture-contract.md).
- For live-update changes, verify rapid repeated file appends do not duplicate events, and a file append updates the selected session without switching when `PinSelectedSession` is true.

## Avoid

- `async void` commands.
- Business behavior in AXAML click handlers.
- Disabling compiled bindings to bypass errors.
- New UI that writes to the Codex sessions directory.
