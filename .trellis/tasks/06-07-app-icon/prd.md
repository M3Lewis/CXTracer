# PRD: Add App Icon

## Goal and User Value
Currently, CXTracer does not display an application icon in the window titlebar or taskbar when running. 

This task will:
- Define the `WindowIcon` resource in `Icons.axaml` referencing the user's provided `AppIcon.png`.
- Update all application windows (`MainWindow.axaml` and `SettingsWindow.axaml`) to reference this icon resource.

## Confirmed Facts
- **Icon File**: The file is already placed at `k:\Code\ACTIVE\CXTracer\src\CXTracer\Icons\AppIcon.png`.
- **Icons Resource Include**: `App.axaml` already registers `avares://CXTracer/Icons/Icons.axaml`.
- **Avalonia Resource Globbing**: `CXTracer.csproj` includes `Icons\**` as `AvaloniaResource`, which means `AppIcon.png` is compiled into the assembly resources.

## Requirements

### REQ-1: Resource Definition in Icons.axaml
- **AC-1.1**: Add a `WindowIcon` resource to `Icons.axaml` with the key `AppIcon`, referencing the URI `avares://CXTracer/Icons/AppIcon.png`.

### REQ-2: Apply Icon to Windows
- **AC-2.1**: Update `MainWindow.axaml` to reference `Icon="{StaticResource AppIcon}"`.
- **AC-2.2**: Update `SettingsWindow.axaml` to reference `Icon="{StaticResource AppIcon}"`.

## Out of Scope
- Converting `.png` to `.ico` for compilation as a native PE header icon in `csproj` (unless requested by the user, as PNG is natively supported by Avalonia windows).
