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
  ref: 2026-06-08-scroll-jumpiness-virtualization
last_checked: 2026-06-08
---

# Rule

When implementing keyboard navigation, focus switching, or scroll alignment on large event streams, use virtualizing list controls (`ListBox` with default recycling panels) rather than flat `ItemsControl` hosts. Follow these constraints:

1. **Suppressed Selection Chrome**: Since transcript lists are read-only views, strip all default selection styling from the generated `ListBoxItem` template using transparent hosts.
2. **Keyboard Focus Separation**: Set `Focusable="False"` on list hosts (like ListBox) AND their corresponding item containers (like ListBoxItem style setters) to prevent them from capturing keyboard focus or tab loops. This ensures pointer clicks select items normally but keyboard focus remains at the parent Window/Host level for global navigation.
3. **Double-Pass Scroll Realization**: When scrolling to or aligning an off-screen item, do not assume its container is materialized. First call `ListBox.ScrollIntoView(index)` to force container creation.
4. **Stable Extent-Space Alignment**: When calculating precise top-alignment or navigation offsets, do not use `TranslatePoint` relative to the list or viewport. Viewport coordinate translations are sensitive to scroll transformations and layout passes, leading to race conditions during rapid/synchronized scrolling. Instead, locate the parent `ListBoxItem` of the realized item container and use its `Bounds.Y` coordinate (which represents the item's static extent-space layout offset inside the virtualizing panel).
5. **Brush and Resource Caching**: Do not initialize color or thickness resources inline inside high-frequency property bindings (e.g. `IBrush CardBackground => new SolidColorBrush(...)`). Declare them as `static readonly` fields.
6. **Virtualization Trade-off**: For dense transcript views where precise top-alignment (pinning items to the top of the viewport) is required during step-by-step navigation, completely disable virtualized panels (replace default `VirtualizingStackPanel` with standard `StackPanel`). This ensures all items remain persistently in the visual tree, maintaining static and stable bounds layout offsets. This allows instant, synchronous single-frame top alignment and completely avoids the scroll jumpiness and layout race conditions caused by dynamic recycling.

# Why

- A flat `ItemsControl` inside a `ScrollViewer` materializes all items simultaneously. For a 300+ item session, this floods the visual tree with thousands of elements, causing pane switching, tab switching, and rendering pipelines to freeze.
- Virtualizing controls only materialize elements visible within the viewport. Visual descendant lookups for off-screen items return null.
- Calling `TranslatePoint` on a visual element relative to a parent that contains/is a `ScrollViewer` returns viewport-relative coordinates. Comparing viewport-relative offsets with `scrollViewer.Offset.Y` (which is in extent-relative coordinates) introduces a coordinate-space mismatch. Furthermore, after `ScrollIntoView()` is called, the scroll position changes asynchronously; reading viewport coordinates before the next layout pass completes yields stale positions.
- In contrast, a virtualizing panel positions `ListBoxItem`s using arrange-pass coordinates. `ListBoxItem.Bounds.Y` is the static, absolute Y offset in the panel's scroll extent. This value is invariant to the current scroll offset and does not suffer from layout/render pass synchronization issues.
- Dynamic recycling panels (like VirtualizingStackPanel) recycle off-screen containers, which causes layout Bounds to constantly recalculate and flicker during rapid or synchronized keyboard navigation. In contrast, disabling virtualization inside the ListBox forces all event cards to stay in the Visual Tree with invariant layout Y coordinates, making exact scroll alignment robust, instant, and flicker-free.

# Do

- Use `ListBox` with `Classes="transcriptList"` and `SelectionMode="Toggle"` (or no-selection) for transcript panes.
- To scroll to an event `evt` and align it cleanly at the top:
  ```csharp
  // 1. Try immediate alignment if container is already in tree
  if (TryGetEventContainer(listBox, evt, out var target))
  {
      TryScrollContainerToTop(scrollViewer, target, vsp);
      return;
  }

  // 2. Force realization and layout pass synchronously (in case virtualization is enabled)
  var itemIndex = FindEventIndex(listBox, evt);
  if (itemIndex >= 0)
  {
      listBox.ScrollIntoView(itemIndex);
      listBox.UpdateLayout();
      if (TryGetEventContainer(listBox, evt, out target))
      {
          TryScrollContainerToTop(scrollViewer, target, vsp);
      }
  }
  ```
- Calculate the extent-space position using the parent `ListBoxItem`'s layout bounds:
  ```csharp
  private static double? GetContainerExtentTop(ContentPresenter target)
  {
      // Walk up to the ListBoxItem that template-hosts this ContentPresenter
      var item = target.GetVisualAncestors().OfType<ListBoxItem>().FirstOrDefault();
      if (item is null) return null;
      
      // ListBoxItem.Bounds.Y is its static offset in the scroll extent
      return item.Bounds.Y;
  }
  ```
- Declare standard brushes statically:
  ```csharp
  private static readonly IBrush BgUser = new SolidColorBrush(Color.Parse("#EAF7F0"));
  ```

# Do Not

- Do not use `ScrollViewer` wrapping `ItemsControl` for scroll lists that can contain more than 50 items.
- Do not use `TranslatePoint` to calculate target scroll offsets when coordinating navigation, as it produces viewport-relative offsets and causes mismatch errors when comparing to `Offset.Y`.
- Do not call `GetVisualDescendants()` on list controls to find or count all items in a sequence (this destroys virtualization benefits and misses off-screen items).
- Do not let the `ListBox` or `ListBoxItem` receive keyboard focus directly if key inputs are captured at the window level.
- Do not force VirtualizingStackPanel when precise top-alignment scrolling is required and rendering performance is not a bottleneck. Using a simple StackPanel provides rock-solid coordinate stability.
