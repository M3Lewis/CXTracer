---
id: frontend.virtualized-list-navigation
type: performance
priority: must
applies_when:
  - implementing or modifying transcript list scrolling
  - navigating between event panels via key/buttons
  - updating scroll alignment or synchronizing pane scroll offsets
code_anchors:
  - src/CXTracer/Views/MainWindow.axaml
  - src/CXTracer/Views/MainWindow.axaml.cs
verify:
  - switching pane focus is fast and does not block the UI thread
  - ScrollEventIntoView correctly scrolls to off-screen/unrealized items
  - no hover/selection highlight visual artifacts show up in read-only lists
  - brush properties do not allocate new SolidColorBrush instances on access
source:
  kind: bug_history
  ref: 2026-06-07-pane-switch-lag-fix
last_checked: 2026-06-07
---

# Rule

When implementing keyboard navigation, focus switching, or scroll alignment on large event streams, use virtualizing list controls (`ListBox` with default recycling panels) rather than flat `ItemsControl` hosts. Follow these constraints:

1. **Suppressed Selection Chrome**: Since transcript lists are read-only views, strip all default selection styling from the generated `ListBoxItem` template using transparent hosts.
2. **Keyboard Focus Separation**: Set `Focusable="False"` on list hosts to prevent them from capturing arrow keys or tab loops. Keep key listeners at the parent Window/Host level.
3. **Double-Pass Scroll Realization**: When scrolling to or aligning an off-screen item, do not assume its container is materialized. First call `ListBox.ScrollIntoView(index)` to force container creation, then query `ContainerFromIndex()` or visual descendants to compute precise alignment offsets.
4. **Brush and Resource Caching**: Do not initialize color or thickness resources inline inside high-frequency property bindings (e.g. `IBrush CardBackground => new SolidColorBrush(...)`). Declare them as `static readonly` fields.

# Why

- A flat `ItemsControl` inside a `ScrollViewer` materializes all items simultaneously. For a 300+ item session, this floods the visual tree with thousands of elements, causing pane switching, tab switching, and rendering pipelines to freeze.
- Virtualizing controls only materialize elements visible within the viewport. Thus, visual descendants lookups for off-screen items return null. Performing coordinate-based math (`TranslatePoint`) directly on off-screen indices without first scrolling to realize them causes synchronization errors or navigation failures.
- Creating new brushes on every binding layout pass causes garbage collection pressure and layout thread overhead.

# Do

- Use `ListBox` with `Classes="transcriptList"` and `SelectionMode="Toggle"` (or no-selection) for transcript panes.
- To scroll to an event `evt`:
  ```csharp
  var itemIndex = listBox.Items.Cast<object>().ToList().FindIndex(x =>
      x is DisplayEvent de && string.Equals(de.Id, evt.Id, StringComparison.Ordinal));
  if (itemIndex >= 0)
  {
      listBox.ScrollIntoView(itemIndex);
  }
  // Now it is safe to locate the container and top-align
  var target = GetItemContainers(listBox).FirstOrDefault(x => IsEventContainer(x, evt));
  if (target is not null)
  {
      ScrollContainerToTop(listBox, scrollViewer, target);
  }
  ```
- Declare standard brushes statically:
  ```csharp
  private static readonly IBrush BgUser = new SolidColorBrush(Color.Parse("#EAF7F0"));
  ```

# Do Not

- Do not use `ScrollViewer` wrapping `ItemsControl` for scroll lists that can contain more than 50 items.
- Do not call `GetVisualDescendants()` on list controls to find or count all items in a sequence (this destroys virtualization benefits and misses off-screen items).
- Do not let the `ListBox` receive keyboard focus directly if key inputs are captured at the window level.
