# Technical Design

## Architecture and Boundaries
- This feature is implemented entirely within the View layer using Avalonia Attached Properties. The Model (`DisplayEvent`) and ViewModel (`MainWindowViewModel`) remain untouched, ensuring perfect separation of concerns and preserving the memory optimization boundaries established previously.

## Component: `SearchHighlight` Attached Properties
We will create a static class `SearchHighlight` containing two Avalonia attached properties:
1. `TextProperty` (`string`): Binds to the original text (e.g., `Title` or `Text`).
2. `QueryProperty` (`string`): Binds to the search query.
3. `HighlightBrushProperty` (`IBrush`): Optional, defines the highlight color (defaults to a theme resource like `SystemAccentColorLight2` or yellow).

## Data Flow and Rendering Logic
1. **Change Detection**: When either `Text` or `Query` changes, a property changed callback is triggered on the attached `TextBlock`.
2. **Fast Path (No Highlighting)**: If `Query` is empty or null, we clear the `TextBlock.Inlines` collection and set `TextBlock.Text` directly. This avoids allocating `Run` elements completely when no search is active, making it exactly as fast as standard binding.
3. **Highlight Path (Parsing and Rendering)**:
   - If a `Query` exists, we use `String.IndexOf(..., StringComparison.OrdinalIgnoreCase)` to find the first occurrence.
   - If no match is found, we fall back to the Fast Path.
   - If matches are found, we allocate new `Run` elements. We iterate through the string, creating standard `Run` blocks for non-matched segments and `Run` blocks with `Background` sets for matched segments.
   - The final collection of `Run` blocks is added to the existing `TextBlock.Inlines` collection using `Inlines.Clear()` followed by appending each `Run` (avoiding resetting the `InlineCollection` instance itself to prevent layout/parent tree issues).

## Memory and Performance Considerations
- **Debouncing Integration**: The `QueryProperty` will be bound to the ViewModel's `EventSearchText` which already has `Delay=250`. This ensures that the heavy recalculation of `Inlines` across 2000 blocks only fires after the user pauses typing.
- **Substring Allocation**: While `string.Substring()` will allocate new strings for the `Run` blocks, this only occurs on visible/matched items during an active search. Since we aren't maintaining these strings permanently (they are cleared when the search is cleared), the temporary GC pressure is an acceptable trade-off for the visual UX.
- **Dynamic Brush Resolution**: To support theme changes cleanly without creating a new `SolidColorBrush` on every render, the brush is resolved from the app's resources or via a shared static brush (e.g. using a theme accent color resource), avoiding inline instantiations during text parsing.

## Critical Gotchas (Avalonia Specific)
- **Text & Inlines Mutual Exclusivity**: In Avalonia, setting the built-in `TextBlock.Text` clears its `Inlines` collection, and conversely, modifying `Inlines` clears `TextBlock.Text` (setting it to `null`).
  - **The Risk**: If we bind `TextBlock.Text` directly in XAML (e.g., `Text="{Binding Title}"`) and simultaneously modify `Inlines` from the attached property, Avalonia will set `TextBlock.Text` to `null`. If the XAML binding happens to be `TwoWay`, this will write `null` back to the ViewModel, destroying the model's data. Even with `OneWay` bindings, this causes layout invalidation loops.
  - **The Solution**: We **must not** bind `TextBlock.Text` in XAML. Instead, we must define and bind our own attached property `SearchHighlight.Text="{Binding Title}"`. The attached property logic will handle setting the built-in `TextBlock.Text` during the Fast Path (when no search is active), and will modify `Inlines` during the Highlight Path, leaving the bound source completely safe from `null` updates.
  - **Logical Tree Safety**: `Inlines.Clear()` correctly detaches logical children, meaning rebuilding inlines periodically does not cause visual/logical memory leaks.

