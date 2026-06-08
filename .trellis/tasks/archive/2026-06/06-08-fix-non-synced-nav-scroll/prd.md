# Fix non-synced navigation scroll position

## Goal

When synchronized navigation is **disabled**, using Up/Down arrow keys or the UI chevron buttons to navigate between messages should scroll the target message to the **top** of the pane viewport — consistent with the behavior when synchronized navigation is enabled.

Currently, the target message ends up at an unpredictable position (often not at the top), breaking the user's spatial model of "step through messages one by one, each pinned to the top."

## Confirmed Facts

- **Two navigation paths exist** in `NavigatePane` ([MainWindow.axaml.cs:221–251](file:///K:/Code/ACTIVE/CXTracer/src/CXTracer/Views/MainWindow.axaml.cs#L221-L251)):
  - **Synced path** (L248–250): calls `ScrollEventIntoView` → `listBox.ScrollIntoView(itemIndex)` (realizes container) → `ScrollContainerToTop`. Works correctly.
  - **Non-synced path** (L241–245): calls `ScrollToAdjacentMessage` → `GetItemContainers` (only visible/buffered containers) → `GetAdjacentIndex` → `ScrollContainerToTop`.
- `ScrollContainerToTop` ([L445–463](file:///K:/Code/ACTIVE/CXTracer/src/CXTracer/Views/MainWindow.axaml.cs#L445-L463)) reads `ListBoxItem.Bounds.Y` (extent-relative arrange offset) and sets `scrollViewer.Offset` to `top - 8px`.
- `GetItemContainers` ([L364–378](file:///K:/Code/ACTIVE/CXTracer/src/CXTracer/Views/MainWindow.axaml.cs#L364-L378)) only returns realized containers from the VirtualizingStackPanel — items outside the viewport + buffer are invisible.
- `GetNextIndex` / `GetPreviousIndex` ([L397–443](file:///K:/Code/ACTIVE/CXTracer/src/CXTracer/Views/MainWindow.axaml.cs#L397-L443)) search the realized container list using a tolerance-based threshold against `scrollViewer.Offset.Y`.
- The synced path calls `listBox.ScrollIntoView(itemIndex)` before `ScrollContainerToTop`, ensuring the target container is realized and laid out. The non-synced path does **not** do this.
- `ConversationEvents` / `ExecutionEvents` are `ObservableCollection<DisplayEvent>`, directly used as `ItemsSource` for the ListBoxes.
- `CurrentTranscriptEvent` tracks the currently navigated-to event; set by both paths.

## Root Cause

The non-synced `ScrollToAdjacentMessage` operates solely on already-realized containers from the VirtualizingStackPanel. Failure modes:

1. **Boundary case**: target item near the edge of the virtualization buffer has stale `ListBoxItem.Bounds.Y` or gets recycled after offset change.
2. **Tall messages**: if a message card exceeds viewport height, the "next" message is never realized, so `GetNextIndex` falls back to `containers.Count - 1`.
3. **Missing ScrollIntoView**: the synced path calls `listBox.ScrollIntoView` first to ensure target exists with correct layout; the non-synced path skips this entirely.

## Approved Fix Direction

Rewrite `ScrollToAdjacentMessage` to determine the target `DisplayEvent` from the **data source** (`ItemsSource`) rather than visual-tree containers, then reuse `ScrollEventIntoView` for scroll positioning. This makes both paths share the same "realize container → top-align" mechanism.

Specifically:
- Use the anchor event (via `GetAnchorEvent`) or `CurrentTranscriptEvent` to find the current position in the `ItemsSource` collection.
- Compute `next = index + direction` on the data source.
- Call `ScrollEventIntoView` on the target `DisplayEvent`.

## Requirements

- REQ-1: After non-synced Up/Down navigation (arrow key or UI button), the target message card's top edge must be aligned to the pane viewport top (with the existing 8px padding), matching synced-navigation behavior.
- REQ-2: Navigation must work correctly regardless of message card height (including cards taller than the viewport).
- REQ-3: No regression to synchronized navigation behavior.
- REQ-4: No regression to keyboard shortcut handling (Left/Right pane switching, Escape, shortcut capture).

## Acceptance Criteria

- [ ] AC-1 (REQ-1): Navigate down 10+ messages using Down arrow without sync enabled — each message's top edge is at the top of the viewport (±8px padding).
- [ ] AC-2 (REQ-1): Navigate up 10+ messages using Up arrow without sync enabled — same top-alignment behavior.
- [ ] AC-3 (REQ-1): Same behavior using the UI chevron buttons.
- [ ] AC-4 (REQ-2): When a message card is taller than the viewport, navigating past it still lands the next message at the viewport top.
- [ ] AC-5 (REQ-3): With sync navigation enabled, Up/Down still works as before (cross-pane sync, correct scroll position).
- [ ] AC-6 (REQ-4): Left/Right pane switching, Escape to close detail, and shortcut capture remain unaffected.

## Out of Scope

- Smooth scroll animation (current behavior is instant offset jump).
- Mouse wheel or touchpad scrolling behavior.
- Any changes to the synchronized navigation logic itself.
