---
id: frontend.theming.default-window-icon
type: architecture_decision
priority: must
applies_when:
  - creating or modifying windows
  - adding new dialogs or views that inherit from Window
code_anchors:
  - src/CXTracer/Icons/Icons.axaml
  - src/CXTracer/Views/MainWindow.axaml
  - src/CXTracer/Views/SettingsWindow.axaml
verify:
  - Check that SukiWindow/Window Icon attribute is set to StaticResource AppIcon
  - Check that Icons.axaml defines AppIcon pointing to AppIcon48.ico
source:
  kind: human_confirmed
  ref: task-2026-06-07-app-icon
last_checked: 2026-06-07
---

# Rule

The default window icon for all application windows (`MainWindow`, `SettingsWindow`, and any future windows/dialogs) must be defined as `AppIcon` pointing to `avares://CXTracer/Icons/AppIcon48.ico`.

# Why

`AppIcon48.ico` (48x48) is the optimized size for Windows titlebar/taskbar rendering, keeping the icon sharp and transparent. Larger sizes (like 256x256) or PNG assets with nested white backgrounds look blurry or out of place on the Windows taskbar.

# How to Reference

Declare the resource in `Icons.axaml`:
```xml
<WindowIcon x:Key="AppIcon">avares://CXTracer/Icons/AppIcon48.ico</WindowIcon>
```

And reference it in XAML on every window root:
```xml
Icon="{StaticResource AppIcon}"
```
