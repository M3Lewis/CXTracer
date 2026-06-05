# Upgrade to Avalonia 12 and SukiUI 7.0.1

## Goal

Upgrade the Codex Lens Avalonia desktop app from Avalonia 11.3.14 and SukiUI 6.1.1 to Avalonia 12 and SukiUI 7.0.1 without changing the product workflow or visual design.

The upgrade should leave the app buildable, runnable, and visually consistent with the current dense light desktop UI.

## Confirmed Facts

- The app is a single .NET desktop project at `src/CodexLens/CodexLens.csproj`.
- Current target framework is `net8.0`, which is supported by Avalonia 12 for desktop apps.
- Current package references are:
  - `Avalonia` 11.3.14
  - `Avalonia.Desktop` 11.3.14
  - `Avalonia.Fonts.Inter` 11.3.14
  - `Avalonia.Diagnostics` 11.3.14 in Debug only
  - `SukiUI` 6.1.1
  - `CommunityToolkit.Mvvm` 8.4.0
- The project enables Avalonia compiled bindings by default.
- `App.axaml` registers `SukiTheme Locale="zh-CN" ThemeColor="Orange"`.
- `MainWindow.axaml` and `SettingsWindow.axaml` are `suki:SukiWindow` views with `BackgroundStyle="Flat"`.
- `MainWindow.axaml.cs` uses visual-tree APIs for view-only scroll navigation. This is an accepted project exception, but it must still compile and behave after the framework upgrade.
- Repository search did not find current usage of `IClipboard`, `DataObject`, `WindowState` styling, `FuncMultiValueConverter`, explicit C# binding construction, `DispatcherTimer`, `Gestures.*`, or focus event handlers.
- Avalonia package references will be pinned to 12.0.4.
- Avalonia 12 removed `Avalonia.Diagnostics`; the current app does not call `AttachDevTools`, so preserving debug dev tools is not required unless explicitly requested.

## Requirements

- R1. Update the app to use SukiUI 7.0.1 exactly.
- R2. Update all Avalonia package references together to Avalonia 12.0.4.
- R3. Remove the obsolete `Avalonia.Diagnostics` reference or replace it only if debug developer tools are intentionally kept and verified.
- R4. Preserve `net8.0` unless the user separately requests a runtime/SDK upgrade to .NET 10.
- R5. Preserve the existing shell contract:
  - `App.axaml` keeps SukiUI theme registration with `Locale="zh-CN"` and `ThemeColor="Orange"`.
  - Main and settings windows remain `SukiWindow`.
  - `BackgroundStyle="Flat"` remains unless SukiUI 7 requires a compatible replacement.
  - compiled bindings remain enabled.
- R6. Fix any Avalonia 12 or SukiUI 7 compile/runtime breakages in AXAML and C# without introducing a second MVVM framework, theme system, navigation stack, or dialog/toast mechanism.
- R7. Update user-facing repo documentation that lists framework versions.
- R8. Keep behavior intact for session loading, search/filtering, transcript panes, settings, shortcut capture, and synchronized navigation.

## Acceptance Criteria

- [ ] `src/CodexLens/CodexLens.csproj` references SukiUI 7.0.1 and Avalonia 12 packages with aligned Avalonia versions.
- [ ] No stale `Avalonia.Diagnostics` 11.x reference remains.
- [ ] The solution restores successfully.
- [ ] Debug and Release builds complete successfully.
- [ ] Existing compiled AXAML bindings remain enabled and compile.
- [ ] `App.axaml`, `MainWindow.axaml`, and `SettingsWindow.axaml` preserve the SukiUI shell and orange light theme contract.
- [ ] The app launches on Windows and the main window renders without missing styles, resource errors, transparent/blank window issues, or startup exceptions.
- [ ] Manual smoke verification covers session list display, selecting a session, search/filter controls, settings window open/close, shortcut capture, transcript up/down buttons, keyboard pane navigation, raw events expander, and busy/status indicators.
- [ ] README or equivalent project documentation no longer advertises Avalonia 11 or SukiUI 6 as the active stack.
- [ ] Any remaining manual verification gaps are recorded with a reason.

## Out of Scope

- Migrating the app from `net8.0` to `net10.0`.
- Redesigning the UI or changing the orange light theme.
- Adding SukiUI.Dock, Suki dialogs/toasts, new navigation controls, or Avalonia 12 page navigation.
- Adding automated headless UI tests unless the upgrade requires behavior changes that are practical to cover that way.

## Open Questions

- None.
