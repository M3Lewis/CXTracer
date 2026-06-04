---
id: frontend.shortcut-capture.contract
type: invariant
priority: must
applies_when:
  - adding or changing shortcut capture UI
  - adding or changing shortcut matching in MainWindow or SettingsWindow
  - changing physical Avalonia Key normalization
code_anchors:
  - src/CodexLens/Views/ShortcutKeyInput.cs
  - src/CodexLens/Views/SettingsWindow.axaml.cs
  - src/CodexLens/Views/MainWindow.axaml.cs
  - src/CodexLens/Models/ShortcutGesture.cs
verify:
  - pressing only Ctrl, Shift, or Alt keeps capture mode active
  - Ctrl+S, Shift+S, and Alt+S capture successfully
  - Ctrl+Shift+' captures and matches successfully
  - invalid non-modifier input does not overwrite the existing saved shortcut
source:
  kind: bug_analysis
  ref: .trellis/tasks/05-26-fix-navigation-settings-interactions/break-loop.md
last_checked: 2026-06-04
---

# Rule

Shortcut capture completes only after a valid non-modifier final key is received with at least one of `Ctrl`, `Shift`, or `Alt`. Modifier-only keydown events must keep capture mode active.

# Why

A previous shortcut bug treated modifier keydown as terminal and validated only letters. That made valid user shortcuts such as `Ctrl+Shift+'` impossible to capture or match.

# Do

Normalize physical Avalonia `Key` values through one shared view-only helper before forwarding modifier/key data to the ViewModel.

# Do Not

Do not hard-code shortcut capture or matching to letters only, and do not duplicate physical-key normalization separately in Settings and MainWindow.
