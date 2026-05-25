# MVVM & State Management

The frontend follows strict MVVM with CommunityToolkit.Mvvm.

## ViewModel Style

Use source generators to keep ViewModels compact and explicit.

```csharp
public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string? currentTitle;

    [RelayCommand]
    private async Task RefreshAsync()
    {
        // Load data
    }
}
```

## State Ownership

- **View**: layout, resources, visual states, and view-only interaction
- **ViewModel**: UI state, commands, validation, and navigation selection
- **Service**: persistence, I/O, domain operations, and cross-screen coordination

## Local vs Shared State

- keep page-local flags such as `IsBusy`, `IsExpanded`, and current selection inside the page ViewModel
- keep shell state such as active page, current workspace, or theme preference in shell-level ViewModels or desktop services
- keep persisted business state in application or infrastructure services, not in Views

## Navigation State

When using `SukiSideMenu`, the shell ViewModel owns:

- the menu item collection
- the current page selection
- any search or filter state for navigation

Page ViewModels should not know about the visual structure of the side menu.

## Dependency Injection

Use constructor injection for ViewModels and UI services.

- register page ViewModels as transient unless reuse is required
- register shared desktop coordinators as singleton only when their lifecycle is intentionally app-wide
- keep service dependencies explicit

## Async Rules

- prefer `[RelayCommand]` returning `Task`
- avoid `async void` except for framework event handlers
- surface failure to the user through dialog, toast, or inline status

## Boundary Rule

ViewModels must not hold references to concrete Avalonia controls, windows, or the visual tree.

Shell-level desktop ViewModels may expose `ISukiDialogManager` and `ISukiToastManager` as presentation-boundary objects when they are used only to bind `SukiWindow.Hosts`.

If a non-UI layer needs user feedback, route that request through a UI-facing abstraction or a shell-owned coordinator instead of calling Suki APIs directly.
