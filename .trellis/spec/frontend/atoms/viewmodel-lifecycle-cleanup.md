---
id: frontend.state.viewmodel-lifecycle-cleanup
type: pitfall
priority: must
applies_when:
  - instantiating transient ViewModels (e.g. Settings, dialogs)
  - subscribing transient ViewModels to long-lived or parent ViewModel PropertyChanged events
code_anchors:
  - src/CXTracer/Views/MainWindow.axaml.cs
  - src/CXTracer/ViewModels/SettingsWindowViewModel.cs
verify:
  - transient ViewModels implement IDisposable to unsubscribe from all parent/singleton events
  - the visual host (Window/UserControl) invokes Dispose() on the transient ViewModel when closed or unloaded
source:
  kind: bug_analysis
  ref: task-2026-06-07-settings-vm-leak
last_checked: 2026-06-07
---

# Rule

Transient ViewModels (like `SettingsWindowViewModel`) that subscribe to events on long-lived ViewModels (like `MainWindowViewModel`) must implement `IDisposable` to unsubscribe. Visual hosts creating these transient ViewModels must invoke `Dispose()` when closed or unloaded.

# Why

A strong event subscription (`+=`) creates a strong reference from the publisher to the subscriber. If the transient ViewModel does not unsubscribe, the long-lived ViewModel will keep it alive in memory forever, causing a memory leak that grows every time the transient window or control is opened and closed.

# Do

- Implement `IDisposable` in transient ViewModels:
  ```csharp
  public void Dispose()
  {
      _main.PropertyChanged -= OnMainPropertyChanged;
  }
  ```
- Invoke `Dispose()` in the Window's `Closed` event or Control's `Unloaded` event:
  ```csharp
  var settingsVm = new SettingsWindowViewModel(viewModel);
  var window = new SettingsWindow { DataContext = settingsVm };
  window.Closed += (_, _) => settingsVm.Dispose();
  ```

# Do Not

- Do not instantiate a transient ViewModel and bind it as a `DataContext` without hooking into the visual host's close/unload event to call `Dispose()`.
