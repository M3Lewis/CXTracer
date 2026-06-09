# Product Requirements Document

## Goal and User Value
When the user types a keyword into the search box, the event list is filtered. To improve scannability, we will add visual highlighting to the matched keywords within the `Title` and `Text` fields of the `DisplayEvent` cards.

## Confirmed Facts
- The UI uses Avalonia UI.
- The `Title` and `Text` fields are currently bound to `TextBlock.Text` properties in `MainWindow.axaml`.
- Filtering is based on `StringComparison.OrdinalIgnoreCase`.
- Memory and UI thread performance are critical because UI virtualization is disabled.
- The search query input (`EventSearchText`) already has a 250ms debounce (`Delay=250`) which will naturally delay highlighting updates until the user pauses typing.

## Requirements
1. **Search Highlight Behavior**: Implement an Attached Property (e.g., `SearchHighlight.Query` and `SearchHighlight.Text`) for `TextBlock` to handle the rendering of highlighted text.
2. **Zero/Low Allocation Parsing**: When applying highlights, parse the text dynamically and split it into Avalonia `Run` elements. If the search query is empty or not found, simply set the standard `Text` property to avoid `Run` allocations entirely.
3. **Model Purity**: The Model (`DisplayEvent`) and ViewModel layers must remain completely untouched. All highlighting logic is strictly confined to the View layer.

## Out of Scope
- Highlighting matched text in the `RawJson` detail popup.
- Implementing object pooling for `Run` elements (not worth the complexity for this specific non-virtualized 2000-item constraint; rebuilding Inlines per query change is sufficient).

## Acceptance Criteria
- Typing a search query highlights the matching substring in `Title` and `Text` with a distinct background color (e.g., Yellow or the theme's highlight color).
- The highlighting is case-insensitive.
- The UI does not stutter while typing (debounced execution).
- Clearing the search query removes the `Run` blocks and reverts the `TextBlock` back to a single text rendering.
