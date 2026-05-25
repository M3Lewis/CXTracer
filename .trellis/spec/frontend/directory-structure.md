# Avalonia + SukiUI Directory Structure

Recommended directory organization for the desktop presentation layer.

## Standard Structure

```text
src/
├── MyApp.Desktop/
│   ├── App.axaml
│   ├── App.axaml.cs
│   ├── Program.cs
│   ├── Views/
│   │   ├── MainWindow.axaml
│   │   ├── MainWindow.axaml.cs
│   │   ├── Pages/
│   │   ├── Dialogs/
│   │   └── Controls/
│   ├── ViewModels/
│   │   ├── Shell/
│   │   ├── Pages/
│   │   ├── Dialogs/
│   │   └── Controls/
│   ├── Services/
│   │   ├── ThemeService.cs
│   │   ├── NavigationService.cs
│   │   └── Notifications/
│   ├── Behaviors/
│   ├── AttachedProperties/
│   ├── Converters/
│   ├── Styles/
│   └── Assets/
├── MyApp.Application/
├── MyApp.Domain/
└── MyApp.Infrastructure/
```

## Folder Responsibilities

- `Views/`: AXAML and code-behind for windows, pages, dialogs, and reusable controls
- `ViewModels/`: state, commands, validation, navigation state, and presentation logic
- `Services/`: desktop-only orchestration such as theming, notifications, and shell coordination
- `Behaviors/`: reusable interaction logic that should stay declarative in AXAML
- `AttachedProperties/`: small control extensions and view-only state
- `Converters/`: lightweight formatting logic for bindings
- `Styles/`: shared resource dictionaries and control styles

## Naming Conventions

- views end with `View` or `Window`
- view models end with `ViewModel`
- dialogs use `*DialogView` and `*DialogViewModel` when they are first-class features
- reusable controls use domain names, not generic labels such as `CustomControl1`

## Shell Separation

Keep shell-specific files easy to find:

- `MainWindow` owns `SukiWindow`
- shell navigation ViewModel lives under `ViewModels/Shell/`
- dialog and toast manager wiring stays close to the shell

## Forbidden Patterns

- putting all Views and ViewModels into one flat folder
- mixing dialog ViewModels with domain services
- storing shared styles inside individual page folders
- keeping behaviors and attached properties inside random code-behind files
