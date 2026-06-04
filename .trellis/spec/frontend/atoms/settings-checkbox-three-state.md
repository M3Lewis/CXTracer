---
id: frontend.settings.checkbox-three-state
type: pitfall
priority: must
applies_when:
  - adding or changing an Avalonia CheckBox bound to a bool or bool? ViewModel property
  - adding or changing SettingsWindow persistent preference controls
code_anchors:
  - src/CodexLens/Views/SettingsWindow.axaml
  - src/CodexLens/ViewModels/SettingsWindowViewModel.cs
verify:
  - clicking the checkbox visibly toggles between checked and unchecked
  - closing and reopening Settings preserves the current value
  - no click cycles through a null visual state
source:
  kind: bug_analysis
  ref: .trellis/tasks/05-26-fix-navigation-settings-interactions/break-loop.md
last_checked: 2026-06-04
---

# Rule

Avalonia `CheckBox` controls bound to persistent boolean settings must set `IsThreeState="False"` explicitly.

# Why

SukiUI themes may allow nullable `CheckBox.IsChecked` behavior to surface. If the control cycles through `null`, a settings proxy setter can reject the value and the checkbox appears stuck or visually reverts.

# Do

Bind settings checkboxes as two-state controls:

```xml
<CheckBox IsThreeState="False"
          IsChecked="{Binding IsSynchronizedNavigationEnabled, Mode=TwoWay}" />
```

# Do Not

Do not rely on the implicit Avalonia default for settings checkboxes whose value must persist as a non-null boolean.
