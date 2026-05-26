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
- Rapid duplicate watcher events for the same path are debounced before UI collections are updated.
- Debounced live updates still respect `PinSelectedSession`: they update the selected file, but do not auto-switch when pinning is enabled.
- Small status badges such as `只读` and `History` stay close to text height and do not stretch to fill multiline rows.

## Verification

- Run `dotnet build .\CodexLens.sln` after UI changes.
- For layout changes, run the app and load `samples/sample-rollout.jsonl` or a copied real transcript.
- For startup/load changes, verify the window remains responsive while sessions and selected transcript events are still filling in.
- For scroll-button changes, verify repeated up/down clicks in both Conversation and Execution panes.
- For settings-window changes, verify the Settings button opens one reusable window, sync navigation can be toggled there, shortcut capture shows the pending shortcut, OK persists it, and restarting restores it.
- For shortcut capture changes, verify pressing only `Ctrl`, `Shift`, or `Alt` does not exit capture mode before testing a full modifier-plus-letter shortcut.
- For live-update changes, verify rapid repeated file appends do not duplicate events, and a file append updates the selected session without switching when `PinSelectedSession` is true.

## Avoid

- `async void` commands.
- Business behavior in AXAML click handlers.
- Disabling compiled bindings to bypass errors.
- New UI that writes to the Codex sessions directory.
