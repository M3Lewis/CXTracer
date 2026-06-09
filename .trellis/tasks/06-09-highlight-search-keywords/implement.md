# Implementation Plan

1. **Create `SearchHighlight.cs` Attached Properties**:
   - [x] Create `src/CXTracer/Behaviors/SearchHighlight.cs` (or similar UI helper namespace).
   - [x] Implement `Text` (string) and `Query` (string) attached properties.
   - [x] Implement a static event handler for property changes that updates the `TextBlock.Inlines`.

2. **Implement Parsing Logic**:
   - [x] Write the logic inside the property changed handler.
   - [x] Fast path: If `Query` is empty, clear `Inlines` and set `Text`.
   - [x] Match path: Split text using `String.IndexOf(..., StringComparison.OrdinalIgnoreCase)`. Add `Run` objects to `TextBlock.Inlines`. Set a distinct `Background` brush for matched runs.

3. **Update `MainWindow.axaml` Bindings**:
   - [x] Locate the `TextBlock` bindings for `Title` and `Text` (around lines 439, 443, 598, 603).
   - [x] Replace standard `Text="{Binding Title}"` with:
     ```xml
     <TextBlock local:SearchHighlight.Text="{Binding Title}"
                local:SearchHighlight.Query="{Binding $parent[Window].DataContext.EventSearchText}"
                TextWrapping="Wrap" />
     ```
   - [x] Ensure the XML namespace `xmlns:local="clr-namespace:CXTracer.Behaviors"` (or wherever it's placed) is available.

4. **Validation**:
   - [x] Verify the app still compiles.
   - [x] Verify search still works identically (case-insensitive search of Title and Text).
   - [x] Verify the detail popup still shows formatted JSON correctly.
   - [x] Verify memory footprint is visibly reduced.
