---
id: frontend.session-list.item-template-styling
type: pitfall
priority: should
applies_when:
  - styling Codex sessions list items
  - changing selected session row background or border
  - modifying ListBoxItem styles in MainWindow.axaml
code_anchors:
  - src/CodexLens/Views/MainWindow.axaml
verify:
  - selected session row background covers the full visible card area
  - selected session row border thickness and color visibly change when edited
  - selected row chrome does not render as a second partial background/border outside the session card
  - non-selected session rows keep the same dense spacing and readable card shape
source:
  kind: human_confirmed
  ref: 2026-06-04 session-list-border-background-debugging
last_checked: 2026-06-04
---

# Rule

When styling the Codex sessions list row chrome, keep the generated `ListBoxItem` template chrome and the visible session card boundary deliberately separated.

# Why

The Fluent/Suki `ListBoxItem` template owns selection chrome such as selected indicators, focus layers, and presenter-hosted backgrounds. Styling only the `DataTemplate` root `Border` can make the white background cover only the text/content area, leaving template-owned areas unchanged. Styling only `ListBoxItem.BorderBrush` or `ListBoxItem.BorderThickness` may also appear to do nothing if the active template does not draw those properties in the visible layer.

# Do

Keep the selected-row visual contract split across the right layer:

- Use `ListBox.sessionList ListBoxItem` for padding reset and template-level normalization when the default template chrome interferes with the visible session card.
- Use a named card class inside the session item template, such as `Border.sessionCard`, for the visible card background and border.
- Use a selected descendant selector, such as `ListBox.sessionList ListBoxItem:selected Border.sessionCard`, when the selected card border/background must differ.
- If replacing the generated item template, keep it minimal: transparent host, no duplicate border/background, and a stretched `ContentPresenter` that passes through the item content/template.

# Do Not

Do not assume that changing `ListBoxItem.BorderThickness`, `ListBoxItem.BorderBrush`, or a root item-template `Border.Background` is enough to change the full visible selected row.

Do not leave both default `ListBoxItem` selection chrome and a visible `Border.sessionCard` trying to draw the same row background/border; that recreates partial backgrounds or double-border artifacts.
