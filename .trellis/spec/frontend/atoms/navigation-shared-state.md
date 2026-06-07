---
id: frontend.navigation.shared-state
type: invariant
priority: must
applies_when:
  - changing transcript pane arrow buttons
  - changing keyboard Up, Down, Left, or Right navigation
  - changing synchronized navigation behavior
  - changing current message highlighting
code_anchors:
  - src/CXTracer/ViewModels/MainWindowViewModel.cs
  - src/CXTracer/Views/MainWindow.axaml.cs
  - src/CXTracer/Models/DisplayEvent.cs
verify:
  - mouse up/down buttons and keyboard Up/Down advance from the same current message when synchronized navigation is enabled
  - Left/Right changes the active Conversation or Execution pane state
  - Up/Down navigation visibly marks the current message in the active sequence
source:
  kind: bug_analysis
  ref: .trellis/tasks/05-26-fix-navigation-settings-interactions/break-loop.md
last_checked: 2026-06-04
---

# Rule

Pane-navigation buttons and keyboard navigation must share ViewModel-owned navigation state for the active pane and current transcript event.

# Why

A previous regression split mouse navigation from keyboard navigation. Mouse buttons used the clicked pane while keyboard arrows used active pane state, so synchronized navigation diverged after moving between panes.

# Do

Let code-behind compute visual anchors and scroll offsets, but write the navigated `DisplayEvent` and active pane back to `MainWindowViewModel`.

# Do Not

Do not let click handlers own independent navigation state or continue from only the pane named by the clicked button when synchronized navigation is enabled.
