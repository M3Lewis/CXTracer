# Execution Pane Scroll Not Following Synchronized Navigation

## Goal

When synchronized navigation is enabled and the user presses Down/Up, both the active pane and the companion pane must scroll their respective target events to the top of the viewport. Currently, the execution (companion) pane's event is logically selected but not visually scrolled into view, making it invisible when the scroll position doesn't match.

## User Value

Users rely on synchronized navigation to correlate conversation messages with execution events at the same timestamp. If the companion pane doesn't scroll, the feature is broken — the user has to manually scroll the execution pane to find the correlated event.

## Confirmed Facts

### Root Cause

`ScrollEventIntoView` (MainWindow.axaml.cs:227) has a two-step process:
1. `listBox.ScrollIntoView(itemIndex)` — requests the virtualized panel to realize the container
2. `GetItemContainers(listBox).FirstOrDefault(x => IsEventContainer(x, evt))` — immediately searches for the realized container to top-align it

**The problem**: With the new `ListBox` virtualization (from the pane-switch-lag fix), `ScrollIntoView` is **asynchronous** — the VirtualizingStackPanel schedules container realization for a future layout pass. The immediate `GetItemContainers` call runs before the container exists in the visual tree, so the top-alignment step silently fails (returns null → no scroll adjustment).

The companion event (execution pane) is most affected because:
- It's scrolled first in `ScrollNavigationTarget` (line 170)
- Its container is likely off-screen (not yet realized)
- After `ScrollIntoView` fires but before layout completes, the code moves on to scroll the active pane's target

### Call Flow

```
NavigatePane → GetSynchronizedNavigationTarget → ScrollNavigationTarget
  → ScrollEventIntoView(companion)  // execution pane — fails silently
  → ScrollEventIntoView(target)     // conversation pane — may also fail
```

### Code Anchors

- `MainWindow.axaml.cs:161` — `ScrollNavigationTarget`
- `MainWindow.axaml.cs:227` — `ScrollEventIntoView`
- `MainWindow.axaml.cs:246` — the failing `GetItemContainers` lookup after `ScrollIntoView`

## Requirements

### REQ-1: Companion scroll alignment

When synchronized navigation scrolls a companion event into view, the companion's card must be visible at or near the top of its pane viewport, matching the same top-alignment behavior as the active pane's target.

**AC-1.1**: Pressing Down repeatedly with sync nav enabled always shows both the conversation and execution events for each navigation step.
**AC-1.2**: The companion event card's top edge is within 20px of the pane's visible top.

### REQ-2: Off-screen container realization

`ScrollEventIntoView` must handle virtualized containers that may not be immediately realized after `ScrollIntoView`.

**AC-2.1**: Scrolling to an event that was previously off-screen correctly top-aligns the card.

## Out of Scope

- Changing the synchronized navigation matching logic (`GetCompanionEvent`, `GetSynchronizedNavigationTarget`)
- Changing the companion selection algorithm
- Non-synchronized (independent) Up/Down navigation (this already works correctly with `ScrollToAdjacentMessage`)

## Open Questions

_None — root cause is confirmed from code inspection._
