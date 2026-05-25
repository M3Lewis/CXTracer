# Codex Lens Avalonia Frontend Guidelines

Frontend here means the Avalonia/SukiUI desktop presentation layer in `src/CodexLens`.

## Structure

### [Directory Structure](./directory-structure.md)

Current single-window layout and where Views, ViewModels, icons, and UI-only code belong.

### [App Shell & Hosts](./app-shell-hosts.md)

Current `App.axaml` and `MainWindow` setup with `SukiTheme` and `SukiWindow`.

### [Theming & Windowing](./theming-windowing.md)

Project theme choices, hardcoded palette reality, and how to evolve it safely.

### [Components & Controls](./component-guidelines.md)

How the current dense two-pane transcript UI is composed.

### [Behaviors & View Logic](./hook-guidelines.md)

Avalonia equivalents for hooks and the current code-behind exception for scroll navigation.

### [State Management](./state-management.md)

CommunityToolkit.Mvvm patterns used by `MainWindowViewModel`.

### [Notifications & Dialogs](./notifications-and-dialogs.md)

Current status-message approach and when Suki dialogs/toasts would be appropriate.

### [Type Safety](./type-safety.md)

Compiled bindings, `x:DataType`, source-generated properties/commands, and nullable rules.

### [Quality Checklist](./quality-guidelines.md)

Build and review checklist for UI changes.

## Tech Stack

- Avalonia 11.3.14
- SukiUI 6.1.1
- CommunityToolkit.Mvvm 8.4.0
- C# latest, nullable enabled
- AXAML compiled bindings enabled in the project file

## Core Rules

- Keep `MainWindow` as a `SukiWindow`.
- Keep `SukiTheme Locale="zh-CN" ThemeColor="Blue"` registered in `App.axaml`.
- Use `x:DataType` for bound views and templates.
- Keep business and IO logic out of AXAML code-behind.
- Preserve the desktop tool layout: dense, scan-friendly, and immediately usable.
