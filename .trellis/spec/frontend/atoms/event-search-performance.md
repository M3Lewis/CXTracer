---
id: frontend.event-search-performance
type: performance
priority: must
applies_when:
  - implementing or modifying search, filtering, or query text matching on event streams
code_anchors:
  - src/CXTracer/ViewModels/MainWindowViewModel.cs
  - src/CXTracer/Models/DisplayEvent.cs
verify:
  - typing in the search box does not block the UI thread or cause rendering stutter
  - active search task cancels immediately when the query changes or session changes
  - filtering yields to the UI thread in batches to keep the application responsive
source:
  kind: bug_history
  ref: task-2026-06-08-optimize-event-search
last_checked: 2026-06-08
---

# Rule

When implementing text search, query filtering, or dynamic display filters on large lists (such as transcript event streams), follow these guidelines:

1. **Zero-Allocation Search Matching**: Do not use concatenated search helper strings (e.g., `SearchableText`) stored permanently in model classes if the list is non-virtualized. Caching strings on thousands of items multiplies memory consumption. Instead, perform direct, field-by-field search matching using zero-allocation case-insensitive comparisons like `StringComparison.OrdinalIgnoreCase`.
2. **Debounce User Typing**: Debounce text input handlers (e.g., `Delay=250` on XAML TextBox bindings or code-behind timers) before triggering query evaluation to prevent restarting heavy filtering operations on every single keystroke.
3. **Cancellation Token Recycling**: Maintain a class-scoped `CancellationTokenSource` for filtering. Cancel and dispose of any active filtering tasks immediately before starting a new search, and also during ViewModel reset/disposal.
4. **Asynchronous Batch Yielding**: Do not block the UI thread while filtering thousands of events. Implement the filter loop asynchronously and yield to the UI thread (via `Task.Yield()` or `await Task.Delay(1)`) in batches (e.g., every 40 events) to maintain UI responsiveness and support incremental UI rendering.
5. **Lazy/Conditional Heavy Matching**: Do not scan heavy fields (like serialized Raw JSON blocks) by default. Restrict Raw JSON matching to cases where the user explicitly requests it (e.g., check `ShowRawEvents` or filter is set to `"Raw"`).
6. **Selection and Scroll Position Restoration**: Rebuilding list collections causes selection loss and resets the scrollbar position to the top of the viewport. The ViewModel must capture and restore the selection (if still visible) after the filter completes. To align the restored selection in the View, the ViewModel must raise a dedicated event (e.g. `FilterAppliedScrollRequest`) upon filter completion. The View must subscribe to this event and call `ScrollEventIntoView` using a low-priority background dispatcher call (e.g. `DispatcherPriority.Background`) to ensure it executes after layout passes.

# Why

- Creating pre-computed lowercase helper strings (like `.ToLowerInvariant()`) for every loaded event consumes significant persistent heap memory, which is especially problematic when UI virtualization is disabled.
- Performing `String.Contains` with `StringComparison.OrdinalIgnoreCase` directly on original string properties is highly optimized in modern .NET and performs case-insensitive comparisons with zero heap allocations.
- Without debouncing, rapid typing triggers multiple parallel filtering loops, which waste CPU cycles and pile up layout requests.
- Yielding control back to the UI thread in batches allows the UI layout and rendering passes to execute concurrently with the filter loop, maintaining a high frame rate and avoiding "Application Not Responding" (ANR) lockups.
- Raw JSON strings can be very large. Scanning them for matches is highly CPU intensive and degrades search performance by orders of magnitude if run unconditionally.
- Rebuilding collections triggers list clearing, resetting the scroll position. Since filter execution is debounced and asynchronous, triggering scroll restoration on search text property changes executes before the new items are rendered. Broadcasting a completion event from the ViewModel guarantees correct timing for scroll positioning.

# Do

- Perform zero-allocation case-insensitive string matching directly on separate properties instead of pre-concatenated buffers:
  ```csharp
  var matchText = evt.Title.Contains(qTrimmed, StringComparison.OrdinalIgnoreCase) || 
                  evt.Text.Contains(qTrimmed, StringComparison.OrdinalIgnoreCase);
  ```
- Use native XAML binding Delay or code-behind timers to debounce typing inputs:
  ```xml
  Text="{Binding EventSearchText, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged, Delay=250}"
  ```
- Cancel previous search tasks and yield periodically:
  ```csharp
  _filterCts?.Cancel();
  _filterCts?.Dispose();
  var cts = new CancellationTokenSource();
  _filterCts = cts;

  int count = 0;
  foreach (var item in allItems)
  {
      cts.Token.ThrowIfCancellationRequested();
      // filter logic here...
      
      if (++count % 40 == 0)
      {
          await Task.Yield(); // yield to UI thread
      }
  }
  ```
- Raise a custom event from the ViewModel when asynchronous/debounced filtering completes, and handle it in the View to scroll the selection back into view:
  ```csharp
  // In ViewModel:
  public event Action<DisplayEvent>? FilterAppliedScrollRequest;
  
  // Inside ApplyFilterAsync:
  if (previousSelected is not null && isStillVisible)
  {
      SetCurrentTranscriptEvent(previousSelected);
      FilterAppliedScrollRequest?.Invoke(previousSelected);
  }

  // In View:
  viewModel.FilterAppliedScrollRequest += ViewModel_FilterAppliedScrollRequest;
  
  private void ViewModel_FilterAppliedScrollRequest(DisplayEvent selected)
  {
      Dispatcher.UIThread.Post(() => ScrollEventIntoView(selected), DispatcherPriority.Background);
  }
  ```

# Do Not

- Do not perform inline string concatenation in the inner loop of a search filter.
- Do not pre-allocate and cache large search-helper strings (e.g. `.ToLowerInvariant()`) permanently on every model item.
- Do not scan the `RawJson` property unless the search filter context explicitly demands it.
- Do not run the filter loop synchronously on the main thread for collections exceeding 100 elements.
- Do not trigger scroll restoration on property changed events of the search query text itself, as it fires before the debounced/asynchronous repopulation finishes.
