# Implementation Plan

## Checklist

### Phase 1: Brush Caching (Low-risk, independent)
- [ ] Add static `SolidColorBrush` fields to `DisplayEvent.cs` for all color hex values
- [ ] Replace `new SolidColorBrush(Color.Parse(hex))` calls in computed properties with static references
- [ ] Build and verify no regressions

### Phase 2: AXAML — Replace ItemsControl with ListBox
- [ ] Add a custom `ListBoxItem` style in `MainWindow.axaml` that strips all selection chrome (reuse the existing transparent template pattern from `sessionList`)
- [ ] Replace Conversation pane `ScrollViewer` + `ItemsControl` with `ListBox` (keep `x:Name="ConversationItemsControl"` or rename to `ConversationListBox`)
- [ ] Replace Execution pane `ScrollViewer` + `ItemsControl` with `ListBox`
- [ ] Disable keyboard navigation on both ListBoxes (`KeyboardNavigation.DirectionalNavigation="None"`) so arrow keys don't get captured
- [ ] Apply `accentScrollArea` scrollbar styling to the ListBox's internal ScrollViewer via a style selector
- [ ] Build and verify card appearance is identical

### Phase 3: Code-behind — Adapt Navigation Helpers
- [ ] Change `ControlsForPane()` return type to `(ListBox, ScrollViewer)` — resolve ScrollViewer from ListBox visual tree (cache reference)
- [ ] Update `GetItemContainers()` to use index-based container lookup instead of `GetVisualDescendants()`
- [ ] Update `ScrollContainerToTop` to work with ListBox containers
- [ ] Update `ScrollEventIntoView` — use `ListBox.ScrollIntoView()` for initial bring-into-view, then fine-tune offset
- [ ] Verify Up/Down navigation works in both panes
- [ ] Verify synchronized navigation companion scrolling works

### Phase 4: Verification
- [ ] `dotnet build` succeeds with no warnings
- [ ] Left/Right arrow switching feels instant on large sessions
- [ ] Up/Down navigation scrolls correctly
- [ ] Synchronized navigation works
- [ ] Search/filter works
- [ ] Card styling is preserved
- [ ] Live session updates append correctly
