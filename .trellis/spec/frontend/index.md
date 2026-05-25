# Avalonia + SukiUI Frontend Guidelines

Production frontend guidelines for desktop applications built with Avalonia, SukiUI, and CommunityToolkit.Mvvm.

## Structure

### [Directory Structure](./directory-structure.md)

Recommended folders for Views, ViewModels, controls, UI services, and assets.

### [App Shell & Hosts](./app-shell-hosts.md)

How to bootstrap `SukiTheme`, replace `Window` with `SukiWindow`, and place dialog and toast hosts correctly.

### [Theming & Windowing](./theming-windowing.md)

Theme color, locale, base theme switching, background styles, and title bar customization.

### [Components & Controls](./component-guidelines.md)

View composition rules and when to use `SukiSideMenu`, `SettingsLayout`, dialogs, toasts, and inline status controls.

### [Behaviors & Attached Properties](./hook-guidelines.md)

Avalonia equivalents for reusable view logic that should not live in code-behind.

### [State Management](./state-management.md)

Strict MVVM guidance for commands, navigation state, shared state, and service boundaries.

### [Notifications & Dialogs](./notifications-and-dialogs.md)

How to expose and use `ISukiDialogManager` and `ISukiToastManager` without breaking MVVM boundaries.

### [Type Safety](./type-safety.md)

Compiled bindings, AOT-safe patterns, converters, and version compatibility constraints.

### [Quality Checklist](./quality-guidelines.md)

Review checklist for shell setup, bindings, theming, performance, and forbidden patterns.

### [Common Issues / Pitfalls](../big-question/index.md)

Known SukiUI setup and runtime failure modes that should be checked first during diagnosis.

## Tech Stack

- **UI Framework**: Avalonia 11
- **UI Library**: SukiUI 6
- **MVVM Toolkit**: CommunityToolkit.Mvvm
- **DI**: Microsoft.Extensions.DependencyInjection
- **Language**: C# / AXAML

## Usage

These guidelines are intended to be used as:

1. **Project bootstrap rules** for new Avalonia + SukiUI apps
2. **Implementation reference** while building desktop screens and flows
3. **Code review checklist** for MVVM, bindings, and host setup
4. **Onboarding material** for engineers new to Avalonia or SukiUI

## Core Rules

- Use `SukiWindow` as the desktop shell, not plain `Window`.
- Initialize `SukiTheme` in `App.axaml` and always set `ThemeColor`.
- Put `SukiDialogHost` and `SukiToastHost` only inside `SukiWindow.Hosts`.
- Prefer SukiUI controls and theme resources over custom styling.
- Keep ViewModels UI-agnostic except for intentional desktop presentation managers.
