---
id: frontend.state.observable-collection-mutation
type: performance
priority: must
applies_when:
  - updating or re-sorting active collections bound to virtualizing UI (e.g. ListBox)
  - handling background live-reload file appends
code_anchors:
  - src/CXTracer/ViewModels/MainWindowViewModel.cs
verify:
  - collections are modified incrementally (e.g., in-place moves or item insertion/removal) rather than fully cleared and rebuilt
source:
  kind: bug_analysis
  ref: task-2026-06-07-session-list-live-thrash
last_checked: 2026-06-07
---

# Rule

Do not rebuild an `ObservableCollection` (using `.Clear()` and `.Add()`) when sorting or updating existing items. Perform incremental mutations (`RemoveAt`, `Insert`, `Move`) instead.

# Why

Calling `Clear()` triggers a `Reset` notification in `INotifyCollectionChanged`. For virtualizing lists (like Avalonia's `ListBox`), this forces the UI framework to discard all materialized item containers (`ListBoxItem`s), recalculate layout, and allocate new containers. During high-frequency background log updates, this causes severe UI lag, garbage collection overhead, and list flickering.

# Do

- Determine the correct sorted position of an updated item and move it using incremental operations:
  ```csharp
  int oldIndex = Sessions.IndexOf(existing);
  int targetIndex = 0; // index of the updated position
  if (oldIndex != targetIndex)
  {
      Sessions.RemoveAt(oldIndex);
      Sessions.Insert(targetIndex, existing);
  }
  ```

# Do Not

- Do not call `Sessions.Clear()` and then loop through a sorted list to re-add items:
  ```csharp
  // BAD: Triggers a full UI reconstruction loop
  Sessions.Clear();
  foreach (var item in sorted) Sessions.Add(item);
  ```
