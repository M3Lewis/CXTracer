# Implementation Plan

1. **Optimize `DisplayEvent.cs` Strings**:
   - [x] Remove the `_formattedRawJson` backing field. Update the `FormattedRawJson` property to parse and return the formatted JSON on-the-fly. Catch parsing errors and return `RawJson`.
   - [x] Remove the `_searchableText` backing field and `SearchableText` property entirely.
2. **Optimize `MainWindowViewModel.cs` Search Logic**:
   - [x] Update `PassesFilterInternal` to use `StringComparison.OrdinalIgnoreCase` checks directly against `evt.Title` and `evt.Text` instead of `evt.SearchableText`. This eliminates the memory overhead of the lowercased searchable text cache while retaining full-text search capability.
   - [x] Remove `qLower` usages to avoid duplicate string allocations.
3. **UI Debounce Implementation**:
   - [x] Update `MainWindow.axaml` search box binding to include `Delay=250` for `EventSearchText`. This limits rapid filter execution and UI stalling during fast typing.
4. **Validation**:
   - [ ] Verify the app still compiles.
   - [ ] Verify search still works identically (case-insensitive search of Title and Text).
   - [ ] Verify the detail popup still shows formatted JSON correctly.
   - [ ] Verify memory footprint is visibly reduced.
