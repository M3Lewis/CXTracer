---
id: frontend.navigation.active-border-style-precedence
type: pitfall
priority: should
applies_when:
  - styling active transcript panes
  - styling current message indicators
  - changing class-based active UI states in AXAML
code_anchors:
  - src/CXTracer/Views/MainWindow.axaml
verify:
  - Left/Right visibly moves the active pane border
  - Up/Down visibly moves the current message indicator
  - active state changes do not shift surrounding layout
source:
  kind: bug_analysis
  ref: .trellis/tasks/05-26-fix-navigation-settings-interactions/break-loop.md
last_checked: 2026-06-04
---

# Rule

Do not set local `BorderBrush` or `BorderThickness` values on controls whose active state is controlled by class styles.

# Why

Local AXAML values override style setters. A previous active-pane style appeared broken because local border values hid the `Classes.active` border setters.

# Do

Keep active-state border values in class styles or templates so state changes remain visible without changing layout dimensions unexpectedly.

# Do Not

Do not mix local border values with class-set active border values on the same control.
