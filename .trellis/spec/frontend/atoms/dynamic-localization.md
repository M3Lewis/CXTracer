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
  - Localization dictionaries must be statically declared in `App.axaml` and dynamically cached/swapped via `IResourceDictionary` typecasting and the `LocalizationLanguage` identifier key to be fully Native AOT-safe.
source:
  kind: user_confirmed
  ref: task-06-07-i18n-support, task-06-07-06-07-fix-aot-i18n
last_checked: 2026-06-07
---

# Rule

To support runtime language toggling in Avalonia + SukiUI with full Native AOT compatibility:
1. **Static UI Text**: All text labels in XAML must be bound using `{DynamicResource [Key]}` instead of hardcoded strings or `{StaticResource}`.
2. **ViewModel Strings**: Dynamic strings (such as status messages, format templates, and toast titles) must be resolved at runtime using `Application.Current.Resources.TryGetResource` rather than hardcoded text.
3. **Decoupled Selection Logic**: Bound dropdown selections (e.g. event filters) must wrap options in a helper model containing a stable logical `Key` and an observable/updatable `DisplayName`. Filter and comparison logic must match against the `Key`, while the UI binds to `DisplayName` (with a `DataTemplate` if necessary). Display names must be updated programmatically when the language changes.
4. **Native AOT Resource Swapping (Static Merging & Caching)**:
   - Do **NOT** instantiate new `ResourceInclude` objects or load `.axaml` files by URI in C# at runtime (dynamic XAML parsing is incompatible with Native AOT compilation).
   - Statically declare all localization dictionaries (`en-US.axaml`, `zh-CN.axaml`) inside the `Application.Resources.MergedDictionaries` block in `App.axaml`.
   - Add a `<sys:String x:Key="LocalizationLanguage">...</sys:String>` tag to each dictionary (e.g., `en` or `zh`) to serve as a runtime identifier.
   - On startup, iterate through `MergedDictionaries`, cast elements (`IResourceProvider`) to `IResourceDictionary`, and check for the `LocalizationLanguage` key. Cache the pre-instantiated dictionaries and remove the inactive one.
   - Switch languages by adding and removing the cached dictionary instances.

# Why

Without these rules:
- Hardcoded strings or `{StaticResource}` bindings will not update when the user toggles the language dropdown.
- Matching logic in ViewModels (such as event filters) will break if matched against translated display strings (e.g., filtering for "对话" fails if the logic expects "Conversation").
- Dynamic instantiation of `ResourceInclude(Uri)` in C# triggers `IL2026` trimming warnings and crashes the application on startup in Native AOT because XAML is processed ahead-of-time and cannot be dynamically loaded by path.
- At runtime under Native AOT, XamlX compiles `<ResourceInclude>` declarations into standard `ResourceDictionary` objects. Querying them using `OfType<ResourceInclude>()` will return an empty list, which prevents the language toggle from finding the dictionaries to switch. Matching on the custom string key via `TryGetResource` is AOT-safe.
