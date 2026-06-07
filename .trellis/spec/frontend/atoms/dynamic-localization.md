---
id: frontend.dynamic-localization
type: invariant
priority: must
applies_when:
  - adding new UI strings in XAML views
  - localizing dynamic status, format, or toast messages in ViewModels
  - creating localized ComboBox/ListBox dropdown items
code_anchors:
  - src/CXTracer/Localization/en-US.axaml
  - src/CXTracer/Localization/zh-CN.axaml
  - src/CXTracer/ViewModels/MainWindowViewModel.cs
verify:
  - Static XAML text must use `{DynamicResource}` references.
  - ViewModel-defined selections (e.g., event filters) must use a Key/DisplayName model (e.g., `FilterOptionItem`) so text translations don't break logic.
  - Previous localization dictionaries must be removed from `MergedDictionaries` before the new one is added to prevent resource leaks and duplicate keys.
source:
  kind: user_confirmed
  ref: task-06-07-i18n-support
last_checked: 2026-06-07
---

# Rule

To support runtime language toggling in Avalonia + SukiUI:
1. **Static UI Text**: All text labels in XAML must be bound using `{DynamicResource [Key]}` instead of hardcoded strings or `{StaticResource}`.
2. **ViewModel Strings**: Dynamic strings (such as status messages, format templates, and toast titles) must be resolved at runtime using `Application.Current.Resources.TryGetResource` rather than hardcoded text.
3. **Decoupled Selection Logic**: Bound dropdown selections (e.g. event filters) must wrap options in a helper model containing a stable logical `Key` and an observable/updatable `DisplayName`. Filter and comparison logic must match against the `Key`, while the UI binds to `DisplayName` (with a `DataTemplate` if necessary). Display names must be updated programmatically when the language changes.
4. **Dynamic Dictionary Swapping**: Changing the language must remove the existing dictionary containing `/Localization/` from `Application.Current.Resources.MergedDictionaries` before adding the new dictionary (e.g., `avares://CXTracer/Localization/zh-CN.axaml`), preventing memory bloat and resource conflicts.

# Why

Without these rules:
- Hardcoded strings or `{StaticResource}` bindings will not update when the user toggles the language dropdown.
- Matching logic in ViewModels (such as event filters) will break if matched against translated display strings (e.g., filtering for "对话" fails if the logic expects "Conversation").
- Swapping resource dictionaries without cleaning up the previous files will lead to multiple conflicting dictionaries being loaded simultaneously, causing redundant memory usage and undefined key-lookup precedence.
