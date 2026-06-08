# Execution Plan: Synchronized Navigation Scroll Optimization

This document outlines the step-by-step checklist to implement the scroll optimization.

## Checklist

### Phase 1: View Layer Refactoring (`MainWindow.axaml.cs`)
- [ ] Optimize helper `FindEventIndex(ListBox, DisplayEvent)` to use a simple allocation-free loop over `listBox.Items`.
- [ ] Refactor `TryGetContainerExtentBounds` to accept `ListBoxItem` directly (removing `VirtualizingStackPanel` parameter) and directly read `item.Bounds.Y` for the extent coordinate.
- [ ] Refactor `ScrollContainerToTop` to accept `ListBoxItem` and call `TryGetContainerExtentBounds`.
- [ ] Refactor `TryGetEventContainer` to return `ListBoxItem` using `FindEventIndex` and direct container lookup (`ContainerFromIndex`).
- [ ] Refactor `GetAnchorEvent` to iterate sequentially via `ContainerFromIndex` and break early when Y exceeds the viewport threshold.
- [ ] Remove obsolete visual tree search methods (`GetItemContainers`, `FindVirtualizingStackPanel`).

### Phase 2: Verification
- [ ] Run `dotnet build` to ensure compiler correctness.
- [ ] Run CXTracer and navigate between conversation and execution panes under sync mode to verify responsive, lag-free rendering.

## Risky Files and Rollback Points

- **Target File**: [MainWindow.axaml.cs](file:///k:/Code/ACTIVE/CXTracer/src/CXTracer/Views/MainWindow.axaml.cs)
- **Rollback**: Standard git discard:
  ```bash
  git restore src/CXTracer/Views/MainWindow.axaml.cs
  ```
