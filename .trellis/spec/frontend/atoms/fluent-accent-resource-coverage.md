---
id: frontend.theme.fluent-accent-resource-coverage
type: pitfall
priority: should
applies_when:
  - changing app theme accent color
  - styling Fluent controls under SukiUI
  - fixing black selection, progress, or scrollbar indicators
code_anchors:
  - src/CodexLens/App.axaml
  - src/CodexLens/Views/MainWindow.axaml
verify:
  - selected session check icon uses the orange accent
  - footer progress indicator uses the orange accent
  - transcript/raw scroll indicators use the orange accent
  - toolbar button hover borders remain visible after accent changes
source:
  kind: human_confirmed
  ref: task-2026-06-04-fix-toolbar-selection-theme-colors
last_checked: 2026-06-04
---

# Rule

When relying on SukiUI `ThemeColor="Orange"`, verify Fluent template-owned accent visuals separately and provide explicit accent resources or scoped template-child styles when they stay black.

# Why

SukiUI's theme color does not reliably recolor every nested Fluent control template. The session-list selected check icon and footer progress indicator stayed black even though the app theme was orange, and the issue was only visible through runtime UI testing.

# Do

Keep `SystemAccentColor`, `SystemAccentColorBrush`, and Fluent accent fill/text brushes aligned with the app's orange accent in `App.axaml`. For controls whose template visuals still ignore those resources, scope overrides to the affected surface in `MainWindow.axaml`.

# Do Not

Do not assume `ThemeColor="Orange"` is enough for `ListBoxItem` selection glyphs, `ProgressBar`, or `ScrollBar` thumbs. Do not hardcode unrelated black/neutral indicators for selected or busy state visuals.
