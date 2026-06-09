# Technical Design

## Architecture and Boundaries
- The memory optimization strictly occurs within the Model layer (`DisplayEvent.cs`), ViewModel layer (`MainWindowViewModel.cs`), and minimal XAML adjustments (`MainWindow.axaml`), explicitly avoiding changes to the fundamental UI control structures (no virtualization re-enabling).

## Data Flow and Contracts
1. **Lazy Property Generation (Memory Footprint Reduction)**:
   - `FormattedRawJson`: Converted from an eagerly cached string to an on-the-fly computed property that uses `JsonSerializer.Serialize` returning an indented string. Memory overhead only occurs when viewing the detail popup, then gets GC'd.
   - `SearchableText`: Removed entirely. String concatenation and `ToLowerInvariant()` buffering previously generated massive temporary heap allocations during search operations.

2. **Search Optimization (Zero-Allocation & CPU Saving)**:
   - **Zero-Allocation Search**: Filtering logic in `PassesFilterInternal` now uses `StringComparison.OrdinalIgnoreCase` against `Title` and `Text` directly. This evaluates matches directly at the character level, bypassing string cloning or lowercase conversions.
   - **Debouncing**: Because 2000 non-virtualized UI elements create a slow O(N) evaluation chain, searching per-keystroke causes UI thread stalling. Adding a native Avalonia `Delay=250` binding modifier to `EventSearchText` prevents filter logic execution until the user pauses typing for 250ms.

3. **Garbage Collection Optimization**:
   - Clearing `ConversationEvents`, `ExecutionEvents`, and `RawEvents` effectively purges the UI tree, allowing Avalonia and .NET GC to reclaim memory seamlessly during session swaps.

## Compatibility and Migration Notes
- Dropping `SearchableText` does not drop functionality; the system still correctly checks both `Title` and `Text` fields for matches.
- Replacing eager JSON formatting requires CPU time on-click, but `System.Text.Json` parses typical small logs (<1MB) in negligible milliseconds, rendering the UX impact virtually zero.
