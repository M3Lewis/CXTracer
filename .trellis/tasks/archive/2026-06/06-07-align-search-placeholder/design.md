# Design: Search Redesign and UX Separation

We will separate session filtering and event content filtering by introducing `SessionSearchText` for the left sidebar and `EventSearchText` for the right transcript panel.

## Proposed Changes

### ViewModels

#### [MODIFY] [MainWindowViewModel.cs](file:///k:/Code/ACTIVE/CXTracer/src/CXTracer/ViewModels/MainWindowViewModel.cs)
- Introduce a master list `private readonly List<SessionInfo> _allSessions = [];` to hold all loaded session files.
- Rename `_searchText` and `SearchText` to `_sessionSearchText` and `SessionSearchText`.
- Introduce `[ObservableProperty] private string _eventSearchText = string.Empty;`.
- Modify `OnSessionSearchTextChanged` callback to dynamically filter the session list using a debounced cancellation token source.
- Update `OnEventSearchTextChanged` to invoke `ApplyFilter()`.
- Update `PassesFilter` to filter using `EventSearchText`.
- Update `ClearSearchCommand` to clear `EventSearchText`.
- Refactor `UpsertSession(string path)` to update `_allSessions` and then incrementally update the visible `Sessions` collection.

### Views

#### [MODIFY] [MainWindow.axaml](file:///k:/Code/ACTIVE/CXTracer/src/CXTracer/Views/MainWindow.axaml)
- Update left sidebar search box to bind to `SessionSearchText` with placeholder `"搜索会话名称或路径..."`.
- Update main panel's top toolbar columns to `ColumnDefinitions="*,Auto,Auto,160,Auto,Auto"`.
- Insert a new `TextBox` at `Grid.Column="3"` bound to `EventSearchText` with placeholder `"过滤日志内容..."`.
- Shift ComboBox to `Grid.Column="4"` and "Clear search" to `Grid.Column="5"`.
