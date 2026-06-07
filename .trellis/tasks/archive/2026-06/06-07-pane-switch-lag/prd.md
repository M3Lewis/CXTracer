# Fix Conversation/Execution Pane Switching Lag

## Problem

When switching focus between the left (Conversation) and right (Execution) transcript panes via keyboard arrows or navigation buttons, the UI stutters noticeably. The lag is proportional to session size — large sessions with hundreds of events freeze the UI for visible durations.

## Root Cause Analysis

Three compounding factors:

### 1. No UI Virtualization (Primary)
Both panes use `ItemsControl` inside `ScrollViewer`. `ItemsControl` does **not** virtualize — it materializes every item template in the visual tree at once. A session with 300 events produces 300+ fully realized card templates with multiple nested `Border`, `StackPanel`, `TextBlock`, and `Grid` controls each.

### 2. Expensive Visual Tree Walks (Secondary)
`GetItemContainers()` in `MainWindow.axaml.cs` calls `itemsControl.GetVisualDescendants().OfType<ContentPresenter>()`, then filters and **sorts by `TranslatePoint()`** for every container. This is O(n) coordinate math on the UI thread on every navigation action.

### 3. Brush Allocation per Access (Minor)
`DisplayEvent` computed properties (`CardBackground`, `CardBorder`, etc.) call `new SolidColorBrush(Color.Parse(hex))` on every property access. Each layout pass may access these multiple times, creating garbage and parse overhead.

## Proposed Solution

### Phase 1: Enable Virtualization
Replace `ItemsControl` + `ScrollViewer` with `ListBox` (which has a built-in `VirtualizingStackPanel`) for both Conversation and Execution panes. This is the single highest-impact change.

**Constraints:**
- Must preserve the existing card template appearance (rounded borders, colors, spacing).
- Must suppress the default ListBox selection chrome — these panes are display-only, not selection targets.
- Must preserve scroll navigation behavior (`ScrollContainerToTop`, `GetItemContainers`, etc.) — these methods may need adaptation to work with virtualized containers.
- The `RawEvents` expander with `MaxHeight="220"` can stay as-is (small item count, bounded height).

### Phase 2: Optimize Navigation Helpers
Adapt `GetItemContainers()` to work with the virtualized `ListBox`:
- Use `ListBox.ContainerFromIndex()` / `ListBox.ItemContainerGenerator` instead of walking the visual tree.
- For scroll-to-item, use `ListBox.ScrollIntoView()` where possible, falling back to manual offset calculation only when needed.

### Phase 3: Cache Brushes
Make `DisplayEvent` brush properties use static cached `SolidColorBrush` instances instead of allocating on every access.

## Acceptance Criteria

1. Left/Right arrow pane switching feels instantaneous (no perceptible stutter) on sessions with 500+ events.
2. Up/Down navigation within a pane scrolls smoothly to the next message card.
3. Synchronized navigation (when enabled) still works: companion pane scrolls to the time-aligned event.
4. All existing card styling (background color, border, role badge, text formatting) is preserved pixel-for-pixel.
5. The active pane orange border indicator still works (`Classes.active` binding).
6. The existing search/filter functionality continues to work.
7. Application builds without warnings.

## Out of Scope

- Changing the GridSplitter or overall two-pane layout.
- Adding new features (this is a performance fix only).
- Modifying the session list panel (left sidebar) — it already has acceptable performance.
