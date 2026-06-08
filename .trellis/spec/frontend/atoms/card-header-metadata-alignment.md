---
id: frontend.card-header.metadata-alignment
type: layout
priority: should
applies_when:
  - adding or modifying metadata badges, capsules, or indicators in card headers
  - modifying layout inside ConversationListBox or ExecutionListBox item templates
code_anchors:
  - src/CXTracer/Views/MainWindow.axaml
verify:
  - ensure all header elements (badges, capsules, text) share a vertical center line
  - verify that numeric capsules use MinWidth to maintain a balanced, circular appearance when numbers are small
source:
  kind: human_confirmed
  ref: task-2026-06-08-event-sequence-capsules
last_checked: 2026-06-08
---

# Rule

When placing metadata indicators (e.g., sequence capsules, badges, tags) next to a timestamp in a card header:
1. **Vertical Alignment**: Wrap the elements in a layout container (e.g. `StackPanel` or `Grid`) and set `VerticalAlignment="Center"` on the container and on **every** child element (`Border`, `TextBlock`, etc.). Do not rely on default/baseline vertical alignment.
2. **Capsule Shape**: For numeric sequence numbers, use a high `CornerRadius` (e.g., `999`), a small padding (e.g., `5,1`), and an explicit `MinWidth` (e.g., `22`) to ensure that single-digit or double-digit values appear as neat circles, while larger values expand gracefully to capsules without stretching layout heights.

# Why

Without explicit vertical centering, the differing default line heights, paddings, and font sizes of timestamps (e.g., `FontSize="12"`) and badge labels (e.g., `FontSize="11"`) cause their vertical center lines to mismatch. This creates a staggered, unpolished layout. Explicitly setting `VerticalAlignment="Center"` forces Avalonia to align their bounding box centers precisely. 

Using `MinWidth` on circular borders prevents them from collapsing into thin ellipses when displaying single-digit numbers.

# Do

Use a clean aligned layout:

```xml
<StackPanel Orientation="Horizontal" Spacing="5" VerticalAlignment="Center">
  <Border CornerRadius="999" Padding="5,1" MinWidth="22" VerticalAlignment="Center">
    <TextBlock Text="{Binding Label}" FontSize="11" VerticalAlignment="Center" HorizontalAlignment="Center"/>
  </Border>
  <TextBlock Text="{Binding TimeText}" FontSize="12" VerticalAlignment="Center"/>
</StackPanel>
```
