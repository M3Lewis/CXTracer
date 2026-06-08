# PRD: Mitigate Conversation-Execution Switching Lag during Sync

## Goal

When synchronized navigation is enabled, switching between the 'conversation' and 'execution' panes (e.g. by pressing Left/Right arrow keys or navigating steps) causes noticeable UI lag and latency. This task aims to optimize the scroll coordination and container lookup code to achieve instant, lag-free pane switching.

## Confirmed Facts

1. **Virtualization Trade-off**: As per `virtualized-list-navigation.md` spec, virtualization has been disabled in the transcript `ListBox` controls (using a standard `StackPanel` instead of `VirtualizingStackPanel`) to ensure stable layout coordinates and precise synchronous top alignment.
2. **Performance Bottleneck**: Disabling virtualization keeps all items in the visual tree. Under this state, the current code in [MainWindow.axaml.cs](file:///k:/Code/ACTIVE/CXTracer/src/CXTracer/Views/MainWindow.axaml.cs) calls `GetVisualDescendants()` up to 4–5 times in a single frame to retrieve and filter `ContentPresenter` containers, traversing thousands of elements repeatedly.
3. **Optimized API Available**: Avalonia's `ItemsControl` (inherited by `ListBox`) supports `ContainerFromIndex(index)` and `ContainerFromItem(item)` for O(1) container lookup, which retrieves the `ListBoxItem` container directly without traversing the visual tree.

## Requirements

1. **Eliminate Visual Tree Traversals**: Optimize `GetAnchorEvent`, `TryGetEventContainer`, `ScrollEventIntoView`, and `ScrollContainerToTop` in `MainWindow.axaml.cs` to use direct container lookups (`ContainerFromIndex`) instead of traversing the entire visual tree via `GetVisualDescendants()`.
2. **Early Loop Exit**: In `GetAnchorEvent`, iterate sequentially and exit the container loop immediately once target Y coordinates exceed the viewport offset + tolerance, avoiding unnecessary scanning of the remaining items.
3. **Preserve Scroll Alignment**: Maintain correct synchronous scroll top-alignment (with 8px padding) for navigated items.

## Acceptance Criteria

- [ ] Switching between Conversation and Execution panes with sync enabled responds instantly without UI thread block or visible lag.
- [ ] Keyboard navigation (Up/Down/Left/Right) remains functional and scrolls target/companion events cleanly to the top viewport offset.
- [ ] No regressions in scroll alignment correctness.
- [ ] Code compiles and runs cleanly.
