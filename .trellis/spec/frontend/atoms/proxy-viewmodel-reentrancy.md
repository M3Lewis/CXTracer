---
id: frontend.settings.proxy-viewmodel-reentrancy
type: pitfall
priority: must
applies_when:
  - creating a ViewModel that exposes settings from another ViewModel
  - binding Avalonia controls to proxy properties that write through to another ViewModel
  - syncing SettingsWindowViewModel state with MainWindowViewModel state
code_anchors:
  - src/CodexLens/ViewModels/SettingsWindowViewModel.cs
  - src/CodexLens/ViewModels/MainWindowViewModel.cs
verify:
  - changing the SettingsWindow control updates the underlying MainWindowViewModel setting
  - the bound control does not visually revert during the same click or key interaction
  - external updates from MainWindowViewModel sync back only when the local value differs
source:
  kind: bug_analysis
  ref: .trellis/tasks/05-26-fix-navigation-settings-interactions/break-loop.md
last_checked: 2026-06-04
---

# Rule

Proxy ViewModels must not directly delegate get/set to another ViewModel property when the setter can trigger synchronous `PropertyChanged` back into the proxy during Avalonia's binding write cycle.

# Why

Avalonia compiled bindings can discard the read-back when a proxy setter mutates the underlying ViewModel and receives the resulting `PropertyChanged` cascade before the binding write finishes. The visible control can revert even though the underlying setting changed.

# Do

Own a local backing field in the proxy ViewModel, update that field before mutating the underlying ViewModel, and sync external changes back only when values differ.

# Do Not

Do not implement a settings proxy as `get => _main.SomeSetting; set => _main.SomeSetting = value; OnPropertyChanged();`.
