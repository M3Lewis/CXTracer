# Implicit Rules Exploration

## Entry: 2026-06-08 - repeated non-synced navigation drift

### Trigger

The user reported that the current Trellis task bug is still present: after repeatedly pressing the transcript navigation chevrons or Up/Down keys, the Conversation pane can show a partially clipped previous card at the top instead of the navigated target card aligned to the viewport top.

### Observed Evidence

- User screenshots show the normal state with a full first visible message card, then a drifted state where the top visible message card is clipped while navigation controls and the pane header remain visible.
- The current dirty diff in `src/CXTracer/Views/MainWindow.axaml.cs` changed non-synced navigation from `ScrollToAdjacentMessage` to `FindAdjacentMessage`, so target choice now comes from the data source instead of only realized containers.
- The remaining shared `ScrollEventIntoView` path still calls `listBox.ScrollIntoView(itemIndex)` and immediately searches realized `ContentPresenter` containers. If the target container has not been realized/arranged yet, the explicit top-align step does not run and Avalonia's default `ScrollIntoView` position remains.
- `.trellis/spec/frontend/atoms/virtualized-list-navigation.md` requires double-pass realization and extent-space alignment for virtualized transcript lists.
- `.trellis/spec/frontend/hook-guidelines.md` still contains older rendered-card targeting guidance. The task PRD and active atom supersede that stale guideline for this fix.

### Inferred Hidden Rules

- Data-source target selection is necessary but not sufficient for virtualized list navigation; the top-align pass must tolerate deferred container realization after `ScrollIntoView`.
- Rapid repeated navigation must ignore stale queued alignment attempts for earlier targets in the same pane, otherwise an older dispatcher callback can move the viewport after a newer key/button press.
- The fix target is the shared `ScrollEventIntoView`/top-align helper path. The AXAML pane layout, synchronized-navigation target calculation, mouse wheel scrolling, and raw events panel are out of scope.

### Action Guidance

- Keep non-synced adjacent target selection index-based against the `ItemsSource`.
- After `ScrollIntoView`, force or retry layout-aware top alignment for the target container. Use the existing `ListBoxItem.Bounds.Y` extent-space calculation once the target container is realized.
- Add per-pane request versioning so queued retries for old navigation targets cannot override the latest target while the user is holding an arrow key or repeatedly clicking a chevron.
- Preserve synchronized navigation behavior by keeping `ScrollEventIntoView` as the shared final positioning path for both panes.

### Confidence

High: the current diff already addresses the PRD's first root cause, and the screenshots match the remaining timing failure where default `ScrollIntoView` makes an item visible without the custom top alignment running.

## Entry: 2026-06-08 - target event pushed outside viewport

### Trigger

The user corrected the failure model: after pressing arrow keys, the viewport can jump to messages earlier than the previous/current navigation target, which pushes the current target event outside the visible area. The fix must guarantee that the navigated event appears in the viewport, not only attempt top alignment.

### Observed Evidence

- The patched code stopped retrying as soon as the target container was found and `TryScrollContainerToTop` returned true.
- That does not verify the final viewport state after Avalonia's deferred `ScrollIntoView`/layout work and subsequent render pass.
- The user specifically observed a temporal/order mismatch: visible messages can be earlier than the intended current message, so navigation state and viewport state can diverge.

### Inferred Hidden Rules

- A navigation request is not complete until the target `DisplayEvent` is both the current navigation state and intersects the pane viewport after layout/render.
- The retry loop must inspect final viewport geometry before deciding it is done. "Container found" is weaker evidence than "container visible in the scroll viewer's extent range."
- If the target is not visible on a verification pass, re-issue `ScrollIntoView(index)` for that same target before attempting precise top alignment.

### Action Guidance

- Refactor the alignment retry so it stops only when the target container is visible and the scroll offset is at the desired top-align position.
- Use the same per-pane request versioning so older queued verifications cannot scroll to earlier targets after a newer key/button navigation.
- Keep the guarantee local to Conversation/Execution ListBox scrolling; do not alter pane layout or keyboard shortcut routing.

### Confidence

High: the user described the observable invariant that matters, and it maps directly to a missing post-layout visibility check in the current implementation.
