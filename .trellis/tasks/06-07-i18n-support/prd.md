# PRD: Support i18n and Chinese/English Language Toggle

## Goal and User Value
Currently, CXTracer only displays the UI in English (with a few hardcoded Chinese strings like `只读`, `搜索会话名称或路径...`). We want to support internationalization (i18n), specifically allowing the user to toggle between English and Simplified Chinese (zh-CN). The language selection should be persistent across application restarts and configured in the settings window.

## Confirmed Facts
- **UI Architecture**: Built using Avalonia + SukiUI. Key windows are `MainWindow.axaml` and `SettingsWindow.axaml`.
- **Application Startup**: `App.axaml` defines global styles and resources. Merged dictionaries currently load `avares://CXTracer/Icons/Icons.axaml`. SukiTheme has `Locale="zh-CN"` hardcoded.
- **Settings Store**: `AppSettings` has properties serializable by source-generated `AppJsonContext` (supporting Native AOT compilation). Settings are saved and loaded by `AppSettingsService` under local app data.
- **ViewModels**: `SettingsWindowViewModel` holds a reference to `MainWindowViewModel` to synchronize shortcuts and navigation status. It is instantiated inside `MainWindow.axaml.cs` when settings button is clicked.
- **Hardcoded XAML strings** (need `{DynamicResource}`):
  - MainWindow: `Sessions root`, `Default`, `Refresh`, `Pin selected`, `Codex sessions`, `只读`, `搜索会话名称或路径...`, `Raw`, `Settings`, `过滤日志内容...`, `Clear search`, `Conversation`, `Execution`, `Raw events`, `Raw JSON`, `Close`, `上一条消息`, `下一条消息`
  - SettingsWindow: `Settings` (title), `Navigation`, `Synchronized navigation`, `Shortcut`, `OK`, `Author`, `Close`
- **Hardcoded ViewModel strings** (need runtime resource lookup):
  - Status messages: `Ready`, `Scanning sessions...`, `No .jsonl sessions found.`, `Loaded {n} session entries.`, `Directory not found: {path}`, `Directory exists. Click Refresh to scan.`, `Scan failed: {msg}`, `Reading {file}...`, `Viewing {file}`, `Live update: {n} events`, `Read failed: {msg}`, `Another session changed: {file}`, `Live read failed: {msg}`, `Settings load failed: {msg}`, `Settings save failed: {msg}`
  - Sync navigation: `Synchronized navigation enabled.`, `Synchronized navigation disabled.`, `Press Ctrl/Shift/Alt + a key for sync navigation.`, `Captured {shortcut}. Click OK to save.`, `Sync navigation shortcut set to {shortcut}.`, `Choose a Ctrl/Shift/Alt + key shortcut first.`, `Press shortcut...`, `Unset`, `No session selected`
  - Count formats: `{n} sessions`, `{visible}/{total} events`
  - Filter options display names: `All`, `Conversation`, `Commands`, `Errors`, `Diffs`, `Final`, `Tools`, `Raw`
  - Settings VM: `Settings ready.`, `Failed to open link: {msg}`

## Requirements

### REQ-1: Localization Resource Dictionaries
- **AC-1.1**: Create `src/CXTracer/Localization/en-US.axaml` containing all UI string resources in English, keyed by consistent identifiers.
- **AC-1.2**: Create `src/CXTracer/Localization/zh-CN.axaml` containing the same keys with Simplified Chinese translations.
- **AC-1.3**: Include format-string templates for parameterized messages (e.g. `StatusLoadedSessions` = `"Loaded {0} session entries."`).

### REQ-2: Dynamic XAML String Bindings
- **AC-2.1**: Update `MainWindow.axaml` — replace all hardcoded label strings with `{DynamicResource [Key]}`.
- **AC-2.2**: Update `SettingsWindow.axaml` — replace all hardcoded label strings with `{DynamicResource [Key]}`.

### REQ-3: ViewModel Runtime Localization
- **AC-3.1**: Implement a helper method (e.g. `GetLocalizedString(key, fallback)`) that calls `Application.Current.TryFindResource(key)` to fetch localized strings at runtime.
- **AC-3.2**: Replace all hardcoded status/format strings in `MainWindowViewModel.cs` and `SettingsWindowViewModel.cs` with calls to this helper.
- **AC-3.3**: Refactor `FilterOptions` from `ObservableCollection<string>` to `ObservableCollection<FilterOptionItem>` (with `Key` + `DisplayName`), so filter logic uses internal keys while display names update on language switch.

### REQ-4: Settings Integration
- **AC-4.1**: Add `public string Language { get; set; } = "en";` to `AppSettings`.
- **AC-4.2**: Expose `CurrentLanguage` property in `MainWindowViewModel` and proxy it in `SettingsWindowViewModel`.
- **AC-4.3**: Persist selected language in `settings.json` when updated.
- **AC-4.4**: Add a `Language` ComboBox in `SettingsWindow.axaml` displaying `English` / `简体中文`, bound to the ViewModel.

### REQ-5: Runtime Language Switching
- **AC-5.1**: Implement `ApplyLanguage(string lang)` method that dynamically replaces the localization `ResourceDictionary` in `Application.Current.Resources.MergedDictionaries`.
- **AC-5.2**: After swapping the dictionary, refresh all computed ViewModel properties (`SessionCountText`, `EventCountText`, `StatusMessage`, filter display names, etc.).

### REQ-6: First-Launch OS Locale Detection
- **AC-6.1**: When no `settings.json` exists on first launch, check `CultureInfo.CurrentUICulture.Name`. If it contains `"zh"`, default to `"zh"` (Chinese). Otherwise default to `"en"` (English).

## Out of Scope
- Supporting other locales beyond English and Simplified Chinese.
- Dynamically switching SukiUI's `Locale` property at runtime (CXTracer doesn't use SukiUI's built-in localized controls like DatePicker, so the impact is negligible).
- Localizing data-driven content (session titles, file paths, event content from JSONL files).
