# Codex Lens Avalonia Frontend Guidelines

Frontend here means the Avalonia/SukiUI desktop presentation layer in `src/CodexLens`.

## Pre-Development Checklist

- Read the guideline document that matches the files you will edit.
- Read every atom under `./atoms/` whose `applies_when` entries match the task.
- Treat atom files as the source of truth for active rules; guideline files provide routing and background.
- If a guideline and an atom conflict, follow the atom and update the stale guideline.

## Quality Check

- Re-read the applicable atoms before review.
- Run the verification items listed in each applicable atom's `verify` field.
- If a task creates, moves, merges, or deletes an atom, update this index in the same change.

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

## Active Spec Atoms

Active atoms live under `./atoms/`. They hold durable, scoped, evidence-backed rules that are not just source summaries. Read the atoms whose `applies_when` entries match the task; guideline files may link to atoms, but the atom file is the source of truth for the rule.

### Settings and Shortcut Input

- [Settings Checkbox Three-State](./atoms/settings-checkbox-three-state.md) - two-state checkbox requirements for persistent settings controls.
- [Proxy ViewModel Reentrancy](./atoms/proxy-viewmodel-reentrancy.md) - avoiding synchronous binding-write reentrancy when a Settings ViewModel proxies MainWindow state.
- [Shortcut Capture Contract](./atoms/shortcut-capture-contract.md) - modifier-only keydown, punctuation shortcuts, and shared physical-key normalization.

### Transcript Navigation

- [Navigation Shared State](./atoms/navigation-shared-state.md) - shared ViewModel-owned state for mouse and keyboard transcript navigation.
- [Active Border Style Precedence](./atoms/active-border-style-precedence.md) - class-style active indicators must not be hidden by local border values.
- [Session List Item Template Styling](./atoms/session-list-item-template-styling.md) - selected session row background/border fixes must target the generated item container and visible card layer without breaking the default selected indicator.

### Toolbar Controls

- [Toolbar Button Visual State Metrics](./atoms/toolbar-button-visual-state-metrics.md) - toolbar button hover/pressed/checked fixes must preserve borders, size, and target scope.

### Theme Accent

- [Fluent Accent Resource Coverage](./atoms/fluent-accent-resource-coverage.md) - SukiUI theme color must be verified against Fluent template-owned selection, progress, and scroll indicators.

## Tech Stack

- Avalonia 12.0.4
- SukiUI 7.0.1
- CommunityToolkit.Mvvm 8.4.0
- C# latest, nullable enabled
- AXAML compiled bindings enabled in the project file

## Core Rules

- Keep `MainWindow` as a `SukiWindow`.
- Keep `SukiTheme Locale="zh-CN" ThemeColor="Orange"` registered in `App.axaml`.
- Use `x:DataType` for bound views and templates.
- Keep business and IO logic out of AXAML code-behind.
- Preserve the desktop tool layout: dense, scan-friendly, and immediately usable.
