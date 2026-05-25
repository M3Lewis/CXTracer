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
- Execution pane contains command, output, diff, tool, and error events.
- Rapid duplicate watcher events for the same path are debounced before UI collections are updated.
- Debounced live updates still respect `PinSelectedSession`: they update the selected file, but do not auto-switch when pinning is enabled.

## Verification

- Run `dotnet build .\CodexLens.sln` after UI changes.
- For layout changes, run the app and load `samples/sample-rollout.jsonl` or a copied real transcript.
- For live-update changes, verify rapid repeated file appends do not duplicate events, and a file append updates the selected session without switching when `PinSelectedSession` is true.

## Avoid

- `async void` commands.
- Business behavior in AXAML click handlers.
- Disabling compiled bindings to bypass errors.
- New UI that writes to the Codex sessions directory.
