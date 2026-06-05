# Technical Design

## Scope

This task is a dependency and compatibility upgrade for the existing Avalonia/SukiUI desktop shell. It should be implemented as a narrow migration, not a UI redesign.

Primary touched areas:

- `src/CodexLens/CodexLens.csproj`
- `src/CodexLens/App.axaml`
- `src/CodexLens/Program.cs`
- `src/CodexLens/Views/MainWindow.axaml`
- `src/CodexLens/Views/MainWindow.axaml.cs`
- `src/CodexLens/Views/SettingsWindow.axaml`
- `src/CodexLens/Views/SettingsWindow.axaml.cs`
- `README.md` or any equivalent stack/version documentation

## Version Strategy

- SukiUI is exact: `7.0.1`.
- Avalonia package versions must stay aligned across all Avalonia packages.
- Use Avalonia `12.0.4` exactly.
- Keep `TargetFramework` as `net8.0`. Avalonia 12 supports .NET 8 and later for desktop apps, and changing to .NET 10 would expand the task into runtime/toolchain migration.
- Keep `CommunityToolkit.Mvvm` unchanged unless restore/build proves a compatibility issue.

## Diagnostics Strategy

Avalonia 12 removed `Avalonia.Diagnostics`.

The current project only references `Avalonia.Diagnostics` in Debug and does not call `AttachDevTools` or `AttachDeveloperTools`. The default implementation path should remove the stale package reference.

Only add `AvaloniaUI.DiagnosticsSupport` if preserving Avalonia Dev Tools becomes an explicit requirement or the project already has a code path that needs it after closer inspection.

## Shell and Theme Contract

The current shell contract is project-specific and must survive the upgrade:

- `App.axaml` registers `SukiTheme`.
- `Locale="zh-CN"` and `ThemeColor="Orange"` stay explicit.
- Main and settings windows stay as `SukiWindow`.
- `BackgroundStyle="Flat"` stays unless SukiUI 7 renamed or removed it.
- Hardcoded light palette values remain current reality. Do not attempt dark-mode support in this task.
- AXAML compiled bindings stay enabled and should be fixed, not disabled.

## Breaking-Change Checks

Repository search found no current usage of several high-risk Avalonia 12 APIs:

- clipboard `IDataObject` / `DataObject`
- `WindowState` in styles
- `FuncMultiValueConverter`
- C# construction of Avalonia bindings
- `DispatcherTimer`
- `Gestures.*`
- focus event handlers

The implementation should still rebuild and inspect errors because SukiUI 7 may expose transitive template or control changes not visible from static search.

Known local touchpoints to verify:

- SukiUI namespaces: `clr-namespace:SukiUI;assembly=SukiUI` and `clr-namespace:SukiUI.Controls;assembly=SukiUI`
- `SukiTheme` properties: `Locale`, `ThemeColor`
- `SukiWindow` properties: `BackgroundStyle`
- visual-tree extension methods: `GetVisualDescendants`, `GetVisualAncestors`
- `ContentPresenter` template usage and `/template/` selectors
- `PathIcon`, `ProgressBar`, `ScrollBar`, `ListBoxItem`, and `CheckBox` style selectors

## Rollback

The upgrade can be rolled back by restoring the prior package references and any migration edits in `src/CodexLens` and documentation. No data migration is expected.

If SukiUI 7.0.1 proves incompatible with Avalonia 12 or the existing shell, stop implementation and record the exact restore/build/runtime failure before choosing a fallback version.
