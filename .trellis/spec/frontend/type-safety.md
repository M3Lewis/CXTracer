# Type Safety & Binding Rules

These rules keep Avalonia bindings explicit and reduce runtime failures, especially in release and AOT-sensitive builds.

## Compiled Bindings

Every view should declare its ViewModel type explicitly.

```xml
<UserControl
    xmlns="https://github.com/avaloniaui"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:vm="using:MyApp.ViewModels.Pages"
    x:Class="MyApp.Views.Pages.DashboardView"
    x:DataType="vm:DashboardViewModel"
    x:CompileBindings="True">

    <TextBlock Text="{Binding CurrentTitle}" />
</UserControl>
```

## Required Rules

- set `x:DataType` on every view that binds to a ViewModel
- enable `x:CompileBindings="True"` on reusable views and pages
- prefer strongly typed properties and commands over object bags or magic strings

## Commands

- use `[RelayCommand]` or equivalent strongly typed command generation
- prefer `Task`-returning commands for async work
- do not hide exceptions inside fire-and-forget UI code

## Converters

Converters should stay small and presentation-only.

- format, map, or normalize values
- do not trigger I/O
- do not mutate application state

## View Resolution

If the application uses a `ViewLocator`, keep the mapping strategy centralized and predictable. Do not spread reflection-based type guessing across feature code.

## Version Compatibility

SukiUI and Avalonia version mismatches can break type resolution and window setup.

- keep Avalonia and SukiUI versions aligned intentionally
- prefer the released NuGet package unless a required fix exists only in CI builds
- when upgrading, verify `SukiTheme` and `SukiWindow` still resolve and render correctly
