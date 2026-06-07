# Design: Pane Switching Lag Fix

## Architecture Decision

### ItemsControl → ListBox Migration

**Why ListBox, not a custom VirtualizingStackPanel?**

Avalonia's `ListBox` inherits from `ListControl` and uses `VirtualizingStackPanel` by default as its `ItemsPanel`. This gives us recycling for free without introducing custom panel code. The key is to suppress the selection behavior (since these panes are read-only transcript displays, not interactive selection lists).

**Alternative considered**: Keeping `ItemsControl` and adding `<ItemsControl.ItemsPanel><ItemsPanelTemplate><VirtualizingStackPanel/></ItemsPanelTemplate></ItemsControl.ItemsPanel>`. However, in Avalonia, `ItemsControl` doesn't implement `IVirtualizingPanel` host logic — virtualization only works with `ListBox` or `ListView` which handle container recycling through `ItemContainerGenerator`.

### Scroll Navigation Adaptation

Current code uses `GetVisualDescendants().OfType<ContentPresenter>()` — this walks the full visual tree. With virtualization, only realized (on-screen) containers exist. Navigation must change to be **index-based**:

1. Track the "current visible top index" by using `ScrollViewer.Offset` and estimated item height.
2. For Up/Down navigation: compute target index, then call `listBox.ScrollIntoView(index)`.
3. For anchor detection: use `listBox.ContainerFromIndex()` on indices near the estimated scroll position.

**Fallback**: If precise top-alignment (current `ScrollContainerToTop`) is critical, keep the manual `scrollViewer.Offset` approach but only for the small set of realized containers.

## File Changes

### [MODIFY] MainWindow.axaml

1. Replace the Conversation `ItemsControl` + outer `ScrollViewer` with a `ListBox`:
   - Move the `ItemTemplate` to `ListBox.ItemTemplate`.
   - Remove the outer `ScrollViewer` (ListBox has its own).
   - Add a no-selection `ListBoxItem` template that suppresses highlight/hover chrome.
   - Apply `Classes="accentScrollArea"` to the ListBox's internal ScrollViewer via a style.

2. Same for the Execution pane.

3. Keep `x:Name` references: rename `ConversationItemsControl` → keep as ListBox name, `ConversationScrollViewer` → access via ListBox's internal `ScrollViewer`.

### [MODIFY] MainWindow.axaml.cs

1. Update `ControlsForPane()` to return `(ListBox, ScrollViewer)` — get the ScrollViewer from the ListBox's visual tree on first use (cached).
2. Update `GetItemContainers()` to use `listBox.ContainerFromIndex(i)` in a loop over `listBox.Items.Count`.
3. For virtualized containers that return null (off-screen), skip them in navigation — only navigate to realized items, or use `ScrollIntoView` first.
4. `ScrollContainerToTop`: keep the manual offset approach for top-alignment precision.

### [MODIFY] DisplayEvent.cs

1. Add static `SolidColorBrush` fields for each color hex.
2. Change `CardBackground`, `CardBorder`, `RoleBadgeBackground`, `RoleBadgeForeground` switch expressions to return the static instances.

## Risk Assessment

| Risk | Mitigation |
|------|-----------|
| ListBox selection chrome leaks through | Custom ListBoxItem template with transparent background, no selection indicator |
| Virtualized containers break `TranslatePoint` calls | Only call on realized containers; null-check `ContainerFromIndex` results |
| Keyboard focus changes break arrow-key navigation | Keep `KeyDown` handler on the Window level; don't let ListBox capture arrow keys (`KeyboardNavigation.DirectionalNavigation="None"`) |
| Scroll position jumps on collection updates (live events) | Keep current append-only pattern; ListBox handles appends to virtualized panels well |

## Verification

1. Build: `dotnet build` must succeed with no warnings.
2. Manual: Open a large session (300+ events), press Left/Right arrows rapidly — no stutter.
3. Manual: Up/Down navigation scrolls to next card in active pane.
4. Manual: Enable synchronized navigation, verify companion pane follows.
5. Manual: Search filter applies correctly, results appear in both panes.
6. Manual: Cards look identical to current design (colors, borders, spacing, role badges).
