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

### Proxy ViewModel Pattern (anti-pattern → corrected)

**Do NOT** use proxy properties that delegate get/set to another ViewModel's property. When a compiled binding writes to a proxy setter, and the setter mutates the underlying ViewModel, the underlying PropertyChanged cascade fires synchronously back into the bound ViewModel **during the binding's write cycle**. Avalonia's binding re-entrancy guard can discard the read-back, causing the control to visually revert.

**Wrong** (causes checkbox toggle failure):
```csharp
// Proxy property — re-entrant PropertyChanged during binding write
public bool? IsSynchronizedNavigationEnabled
{
    get => _main.IsSynchronizedNavigationEnabled;
    set
    {
        if (value is not bool enabled || _main.IsSynchronizedNavigationEnabled == enabled)
            return;
        _main.IsSynchronizedNavigationEnabled = enabled; // triggers cascade back to self
        OnPropertyChanged();
    }
}
```

**Correct** — expose a nullable binding property for `CheckBox.IsChecked`, own a local non-null field, and sync both directions explicitly:
```csharp
private bool _isSynchronizedNavigationEnabled;

public bool? IsSynchronizedNavigationEnabled
{
    get => _isSynchronizedNavigationEnabled;
    set
    {
        if (value is not bool enabled || _isSynchronizedNavigationEnabled == enabled)
            return;
        _isSynchronizedNavigationEnabled = enabled;
        _main.IsSynchronizedNavigationEnabled = enabled;
        OnPropertyChanged();
    }
}

// In constructor:
_isSynchronizedNavigationEnabled = main.IsSynchronizedNavigationEnabled;

// In PropertyChanged handler — only sync from underlying VM when value actually differs:
case nameof(MainWindowViewModel.IsSynchronizedNavigationEnabled):
    if (_isSynchronizedNavigationEnabled != _main.IsSynchronizedNavigationEnabled)
    {
        _isSynchronizedNavigationEnabled = _main.IsSynchronizedNavigationEnabled;
        OnPropertyChanged(nameof(IsSynchronizedNavigationEnabled));
    }
    break;
```

**Why this works**: The local field is updated BEFORE `_main` is touched, so when the `_main` PropertyChanged cascade fires back, the equality check `_isSynchronizedNavigationEnabled != _main.IsSynchronizedNavigationEnabled` is `false` — the handler is a no-op. The only effective `OnPropertyChanged` call is the one from the local setter, after all mutations are complete and outside any re-entrant binding cycle.

**Also**: Always set `IsThreeState="False"` explicitly on Avalonia `CheckBox` controls. SukiUI themes may override the default.

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
- active transcript pane and current navigated transcript event

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
- Add large `ObservableCollection` updates in batches and yield to `DispatcherPriority.Background` between batches.
- Startup may auto-load the newest session, but older sessions should remain metadata-only until explicit selection.
- Do not use `async void` except framework event handlers.

## Watcher Event Debounce

`FileSystemWatcher` can raise multiple events for one append. `MainWindowViewModel`
debounces changes per path before entering the UI thread:

- `_changeDebouncers` maps file path to the latest `CancellationTokenSource`.
- a new change for the same path cancels and disposes the previous token source.
- the active task waits briefly before calling `HandleSessionFileChangedOnUiThreadAsync`.
- only after the debounce delay should code marshal through `Dispatcher.UIThread`.

This keeps duplicate `Changed` events from causing overlapping appended reads or
visible event duplication.

The debounce state is ViewModel-owned because it coordinates UI updates and
selection/pinning behavior. `SessionWatcher` should remain a thin event source.

## Boundary Rules

- ViewModels may depend on services and model types.
- ViewModels should not reference `SukiWindow`, `ScrollViewer`, `ItemsControl`, or visual tree types.
- Code-behind should not own app state.

## Avoid

- Adding global state stores for the current single-window app.
- Using static mutable state for selected session or root path.
- Updating observable collections from background threads.
- Letting old debounce token sources survive after they have been superseded or after the ViewModel is disposed.
