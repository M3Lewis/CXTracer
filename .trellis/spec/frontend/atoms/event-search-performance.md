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

1. **Pre-computed Search Targets**: Do not perform string concatenation or case-insensitive operations on every event during the filter loop. Cache a combined, lowercase representation (e.g. `SearchableText`) during event initialization/lazy-loading.
2. **Debounce User Typing**: Debounce text input handlers (e.g. 200ms) before triggering query evaluation to prevent restarting heavy filtering operations on every single keystroke.
3. **Cancellation Token Recycling**: Maintain a class-scoped `CancellationTokenSource` for filtering. Cancel and dispose of any active filtering tasks immediately before starting a new search, and also during ViewModel reset/disposal.
4. **Asynchronous Batch Yielding**: Do not block the UI thread while filtering thousands of events. Implement the filter loop asynchronously and yield to the UI thread (via `Task.Yield()` or `await Task.Delay(1)`) in batches (e.g., every 40 events) to maintain UI responsiveness and support incremental UI rendering.
5. **Lazy/Conditional Heavy Matching**: Do not scan heavy fields (like serialized Raw JSON blocks) by default. Restrict Raw JSON matching to cases where the user explicitly requests it (e.g., check `ShowRawEvents` or filter is set to `"Raw"`).

# Why

- Performing `String.Contains` with `StringComparison.OrdinalIgnoreCase` or combining `Title` and `Text` on every iteration allocates significant short-lived strings, triggering frequent Garbage Collection (GC) pauses and UI stutter.
- Without debouncing, rapid typing triggers multiple parallel filtering loops, which waste CPU cycles and pile up layout requests.
- Yielding control back to the UI thread in batches allows the UI layout and rendering passes to execute concurrently with the filter loop, maintaining a high frame rate and avoiding "Application Not Responding" (ANR) lockups.
- Raw JSON strings can be very large. Scanning them for matches is highly CPU intensive and degrades search performance by orders of magnitude if run unconditionally.

# Do

- Lazy-initialize and cache the searchable text inside the model:
  ```csharp
  private string? _searchableText;
  public string SearchableText => _searchableText ??= $"{Title} {Text}".ToLowerInvariant();
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

# Do Not

- Do not perform inline string concatenation or ordinal case-insensitive matching in the inner loop of a search filter.
- Do not scan the `RawJson` property unless the search filter context explicitly demands it.
- Do not run the filter loop synchronously on the main thread for collections exceeding 100 elements.
