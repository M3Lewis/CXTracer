# MVVM and State Management

Codex Lens uses CommunityToolkit.Mvvm and a single shell ViewModel.

## ViewModel Pattern

`MainWindowViewModel` is a `sealed partial` class deriving from `ObservableObject`.

Use source-generated observable properties:

```csharp
[ObservableProperty]
private string _searchText = string.Empty;
```

Use source-generated commands:

```csharp
[RelayCommand]
private async Task RefreshAsync()
{
    // UI operation
}
```

## State Ownership

`MainWindowViewModel` owns UI state:

- `RootPath`
- `SearchText`
- `SelectedFilter`
- `SelectedSession`
- `StatusMessage`
- `IsBusy`
- `ShowRawEvents`
- `PinSelectedSession`
- event counts
- observable event/session collections

Services own IO and parsing. Views own visuals and view-only scroll mechanics.

## Collections

Use `ObservableCollection<T>` for collections bound to the UI:

- `Sessions`
- `ConversationEvents`
- `ExecutionEvents`
- `RawEvents`
- `FilterOptions`

Keep the full event backing list private as `_allEvents`, then project visible collections in `ApplyFilter()`.

## Async and UI Thread

- Commands that do IO should return `Task`.
- Use cancellation when replacing a selected-session load.
- Use `Dispatcher.UIThread` before mutating observable collections from watcher events.
- Do not use `async void` except framework event handlers.

## Boundary Rules

- ViewModels may depend on services and model types.
- ViewModels should not reference `SukiWindow`, `ScrollViewer`, `ItemsControl`, or visual tree types.
- Code-behind should not own app state.

## Avoid

- Adding global state stores for the current single-window app.
- Using static mutable state for selected session or root path.
- Updating observable collections from background threads.
