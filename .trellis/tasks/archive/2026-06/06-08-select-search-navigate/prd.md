# PRD: Select Search Item and Resume Navigation

Allow users to select an event card within search results, navigate with arrow keys within the filtered list, and retain selection/resume global navigation after clearing the search query.

## Goal and User Value

When debugging long traces, users search for keywords to find relevant logs. Currently:
1. They cannot easily select a card from the search results to start navigating from it.
2. Clicking a card opens the detail popup immediately, which blocks keyboard navigation.
3. Clearing the search box resets the selection to null, forcing the user to find their place again.

This task enables clicking to select a card, navigating within search results, and clearing the search while keeping the selected card highlighted and scrolled into view, allowing the user to seamlessly resume global navigation from that point.

## Confirmed Facts

1. **Click Behavior**: Left-clicking a card currently only triggers `viewModel.ShowDetailPopup(evt)` and does not set the active selection (`SetCurrentTranscriptEvent(evt)`).
2. **Keyboard Interception**: The search text box retains focus while typing, which causes `IsTextInputFocused(e.Source)` to return true, blocking global Up/Down arrow navigation.
3. **Selection Clearance**: `ApplyFilterAsync` in `MainWindowViewModel.cs` calls `SetCurrentTranscriptEvent(null)` unconditionally at the start of the filtering pass, wiping out any selection when the search query is changed or cleared.
4. **Scroll Alignment**: When search is cleared, the full lists are rebuilt, but the previously selected item is not scrolled back into view.

## Requirements

*   **REQ-01: Card Selection on Click**
    *   Left-clicking an event card must set it as the active `CurrentTranscriptEvent` and highlight it.
    *   To prevent the detail popup from blocking navigation, the detail popup should only open on double-click (or a specific action), while single-click is reserved for selection.
*   **REQ-02: Keyboard Navigation in Filtered Results**
    *   Up/Down arrow keys must navigate selection within the active pane's filtered search results.
    *   To support keyboard-only workflow, pressing Up/Down arrow keys while the search TextBox is focused must still trigger active pane list navigation (since Up/Down arrows do not edit text).
    *   Left-clicking a card to select it must automatically unfocus the search TextBox (e.g. by setting focus to the main SukiWindow or ListBox).
*   **REQ-03: Retain Selection on Clear Search**
    *   When the search query is cleared, `ApplyFilterAsync` must NOT clear the active selection if that selected item is still present in the target list (which is always true when resetting to the global list).
*   **REQ-04: Scroll-into-view on Reset**
    *   When search is cleared, the selected event must be scrolled into view at the top of its pane so the user can resume global navigation from that point.

## Acceptance Criteria

1. **Card Click Selection**: Single-clicking a card highlights it as the active navigation target. Double-clicking the card opens the detail popup.
2. **Search Navigation**: When a search query is active and a card is selected, pressing Up/Down arrow keys moves the selection only within the visible filtered cards of that pane, even if the search text box is currently focused.
3. **Resume Global Navigation**: Clicking the "Clear Search" button:
   - Resets the search text box.
   - Restores the unfiltered global list.
   - Retains the highlight on the selected card.
   - Scrolls the selected card to the top of the viewport.
   - Allows subsequent Up/Down arrow key presses to navigate adjacent global events starting from that card.

## Out of Scope

- Storing search history or history of selected cards across session loads.
- Selecting multiple cards simultaneously.

## Open Questions

None. All planning and focus management requirements are resolved.
