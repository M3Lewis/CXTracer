# Product Requirements Document

## Goal and User Value
Reduce the overall memory footprint of CXTracer (currently 300-400MB) and troubleshoot/fix memory leaks. This ensures the app remains stable and performant during extended use.

## Confirmed Facts
- CXTracer uses Avalonia UI and CommunityToolkit.Mvvm.
- Users load session files which are parsed into `DisplayEvent` objects.
- `MaxDegreeOfParallelism = 4` only restricts the background summary scanning, not the actual loaded event count.
- UI Virtualization is intentionally disabled to preserve scroll positioning accuracy. The memory footprint peak is largely unavoidable without breaking UX, so we must optimize data models instead.
- `DisplayEvent` aggressively caches large strings (`FormattedRawJson`, `SearchableText`) upon initialization, multiplying the memory overhead per event.

## Requirements
1. **Data Diet (Model Optimization)**:
   - Remove eager formatting and caching of `FormattedRawJson`. Parse and format the JSON only when requested by the UI (e.g., when the detail popup opens).
   - Eliminate `SearchableText` cache completely to prevent thousands of duplicate string allocations.
2. **Search Optimization**:
   - Implement zero-allocation string searching directly against original event fields.
   - Introduce UI debounce for search input to prevent rapid, repetitive O(N) filtering across thousands of non-virtualized elements.
3. **Memory Leak Prevention**:
   - Ensure `_allEvents`, `ConversationEvents`, `ExecutionEvents`, and `RawEvents` release objects cleanly upon session switch.

## Acceptance Criteria
- Loading a session with ~1500 lines consumes significantly less baseline memory.
- Searching logs causes zero new string allocations and doesn't freeze the UI while typing.
- Switching between sessions multiple times returns memory usage to a stable baseline without indefinite growth.

## Out of Scope
- Re-enabling UI virtualization for ListBoxes.
- Data pagination or "load more" infinite scroll mechanics.
- Background log parsing limitations (we maintain the load-all approach for seamless cross-pane UX).

## Open Questions
- None.
