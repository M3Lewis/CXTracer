# PRD: Optimize Event Search Performance

## Goal and User Value

Currently, the event search box causes significant UI lag when typing. This is because every keystroke synchronously clears and rebuilds the non-virtualized `ListBox`es for all visible events, scanning through multiple large text fields (including potentially very long `RawJson`) without any debounce.

This task will:
- Implement debounce (150-250ms) on `OnEventSearchTextChanged` to prevent UI reconstruction on every keystroke.
- Make the filter application asynchronous and batch-based using `YieldToUiAsync()`, avoiding blocking the UI thread.
- Optimize search efficiency by pre-computing a searchable text field and restricting `RawJson` searches to specific filters or conditions.

## Confirmed Facts

- **Keystroke Handler**: `OnEventSearchTextChanged(string value)` currently calls `ApplyFilter()` synchronously on every character change.
- **Filtering Logic**: `ApplyFilter()` iterates over all events and adds matching ones. `PassesFilter()` scans `Title`, `Text`, and `RawJson` on every check.
- **Yielding mechanism**: `MainWindowViewModel.cs` already contains `YieldToUiAsync()` using `DispatcherPriority.Background`.
- **Batching size**: `MainWindowViewModel.cs` already defines `EventBatchSize` (currently 40).

## Requirements

### REQ-1: Keystroke Debouncing
- **AC-1.1**: Add a `CancellationTokenSource? _eventFilterCts` to track and cancel ongoing filter operations.
- **AC-1.2**: In `OnEventSearchTextChanged(string value)`, cancel any existing CTS and start a new one to delay the filter application (or use a task-based delay / cancellation pattern).

### REQ-2: Asynchronous Event Batching
- **AC-2.1**: Modify `ApplyFilter()` to be an async method `ApplyFilterAsync(CancellationToken ct)`.
- **AC-2.2**: Clear visible lists and run through all events in `_allEvents`.
- **AC-2.3**: Yield to the UI thread using `YieldToUiAsync()` every `EventBatchSize` (40) events to allow UI interaction during rebuilds.
- **AC-2.4**: Gracefully handle cancellation (throw/catch or return early on cancellation requested).

### REQ-3: Conditional RawJson Search
- **AC-3.1**: Modify `PassesFilter()` to only search `evt.RawJson` when `ShowRawEvents` is true or the selected filter is "Raw". Otherwise, exclude `RawJson` from search matching.

### REQ-4: Pre-computed Searchable Text
- **AC-4.1**: In `DisplayEvent`, expose a pre-computed lowercase `SearchableText` property that combines `Title` and `Text`.
- **AC-4.2**: Use this `SearchableText` with a pre-lowercased query in `PassesFilter()` instead of doing multiple `Contains(..., StringComparison.OrdinalIgnoreCase)` calls.

### REQ-5: Disposal / Clean-up
- **AC-5.1**: Ensure `_eventFilterCts` is canceled and disposed of when the ViewModel is disposed.
